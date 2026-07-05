using PcAssistant.Application.Abstractions.Services;
using PcAssistant.Application.Abstractions.UnitOfWorks;
using PcAssistant.Application.Models;
using PcAssistant.Domain.Entity;

namespace PcAssistant.Application.UseCases;

public sealed class ScheduledTaskService(
    ICommandHistoryUnitOfWorkFactory unitOfWorkFactory,
    IMessageAutomationService messageAutomationService) : IScheduledTaskService
{
    private static readonly TimeSpan MinimumDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MaximumDelay = TimeSpan.FromHours(24);
    private static readonly SemaphoreSlim DueTaskCompletionGate = new(1, 1);

    public async Task<IReadOnlyList<ScheduledTaskItem>> ListQueuedAsync(CancellationToken cancellationToken = default)
    {
        await using var unitOfWork = unitOfWorkFactory.Create();
        var queuedTasks = await unitOfWork.ScheduledTasks.ListQueuedAsync(cancellationToken);
        return SortQueue(queuedTasks).Select(ToScheduledTaskItem).ToArray();
    }

    public async Task<ScheduledTaskPage> ListByTaskTypeAsync(
        string taskType,
        int pageNumber,
        int pageSize,
        string? searchText = null,
        CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var skip = (pageNumber - 1) * pageSize;

        await using var unitOfWork = unitOfWorkFactory.Create();
        var totalCount = await unitOfWork.ScheduledTasks.CountByTaskTypeAsync(taskType, searchText, cancellationToken);
        var tasks = await unitOfWork.ScheduledTasks.ListByTaskTypeAsync(taskType, searchText, skip, pageSize, cancellationToken);
        return new ScheduledTaskPage(
            tasks.Select(ToScheduledTaskItem).ToArray(),
            pageNumber,
            pageSize,
            totalCount);
    }

    public async Task<ScheduledTaskItem> QueueRestartAsync(
        Guid? chatSessionId,
        Guid? commandLogId,
        TimeSpan delay,
        string? message,
        CancellationToken cancellationToken = default)
    {
        var normalizedDelay = NormalizeDelay(delay);
        var now = DateTimeOffset.UtcNow;
        var task = ScheduledTask.QueueRestart(
            chatSessionId,
            commandLogId,
            now.Add(normalizedDelay),
            now,
            message);

        await using var unitOfWork = unitOfWorkFactory.Create();
        try
        {
            await unitOfWork.ScheduledTasks.AddAsync(task, cancellationToken);
            await ReorderQueueAsync(unitOfWork, task, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
            return ToScheduledTaskItem(task);
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ScheduledTaskItem> ScheduleMessageAutomationAsync(
        ScheduleMessageAutomationRequest request,
        CancellationToken cancellationToken = default)
    {
        var recipients = NormalizeRecipients(request.RecipientNames);
        var now = DateTimeOffset.UtcNow;
        var repeatMode = NormalizeRepeatMode(request.RepeatMode);
        var repeatDaysOfWeek = repeatMode == ScheduledTask.WeeklyRepeatMode
            ? NormalizeRepeatDays(request.RepeatDaysOfWeek, request.ScheduledForLocal.DayOfWeek)
            : 0;
        var scheduledForUtc = repeatMode == ScheduledTask.WeeklyRepeatMode
            ? NextWeeklyOccurrenceUtc(request.ScheduledForLocal, repeatDaysOfWeek, now)
            : EnsureFutureUtc(request.ScheduledForLocal.ToUniversalTime(), now);

        var task = ScheduledTask.QueueMessageAutomation(
            request.Title,
            request.AppPath,
            request.AppDisplayName,
            recipients,
            request.Message,
            scheduledForUtc,
            now,
            repeatMode,
            repeatDaysOfWeek);

        await using var unitOfWork = unitOfWorkFactory.Create();
        try
        {
            await unitOfWork.ScheduledTasks.AddAsync(task, cancellationToken);
            await ReorderQueueAsync(unitOfWork, task, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
            return ToScheduledTaskItem(task);
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ScheduledTaskItem?> RescheduleMessageAutomationAsync(
        Guid scheduledTaskId,
        ScheduleMessageAutomationRequest request,
        CancellationToken cancellationToken = default)
    {
        var recipients = NormalizeRecipients(request.RecipientNames);
        var now = DateTimeOffset.UtcNow;
        var repeatMode = NormalizeRepeatMode(request.RepeatMode);
        var repeatDaysOfWeek = repeatMode == ScheduledTask.WeeklyRepeatMode
            ? NormalizeRepeatDays(request.RepeatDaysOfWeek, request.ScheduledForLocal.DayOfWeek)
            : 0;
        var scheduledForUtc = repeatMode == ScheduledTask.WeeklyRepeatMode
            ? NextWeeklyOccurrenceUtc(request.ScheduledForLocal, repeatDaysOfWeek, now)
            : EnsureFutureUtc(request.ScheduledForLocal.ToUniversalTime(), now);

        await using var unitOfWork = unitOfWorkFactory.Create();
        var task = await unitOfWork.ScheduledTasks.GetByIdAsync(scheduledTaskId, cancellationToken);
        if (task is null || task.TaskType != ScheduledTask.MessageAutomationTaskType)
        {
            return null;
        }

        try
        {
            task.UpdateMessageAutomation(
                request.Title,
                request.AppPath,
                request.AppDisplayName,
                recipients,
                request.Message,
                scheduledForUtc,
                repeatMode,
                repeatDaysOfWeek,
                now);
            await ReorderQueueAsync(unitOfWork, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
            return ToScheduledTaskItem(task);
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<ScheduledTaskItem>> CompleteDueTasksAsync(CancellationToken cancellationToken = default)
    {
        await DueTaskCompletionGate.WaitAsync(cancellationToken);
        try
        {
            await using var unitOfWork = unitOfWorkFactory.Create();
            var now = DateTimeOffset.UtcNow;
            var queuedTasks = await unitOfWork.ScheduledTasks.ListQueuedAsync(cancellationToken);
            var dueTasks = queuedTasks
                .Where(task => task.ScheduledForUtc <= now)
                .ToArray();

            if (dueTasks.Length == 0)
            {
                return Array.Empty<ScheduledTaskItem>();
            }

            try
            {
                foreach (var task in dueTasks)
                {
                    if (task.TaskType == ScheduledTask.MessageAutomationTaskType)
                    {
                        await CompleteMessageAutomationTaskAsync(task, now, cancellationToken);
                        continue;
                    }

                    task.MarkCompleted("Scheduled window elapsed.", now);
                }

                await ReorderQueueAsync(unitOfWork, cancellationToken);
                await unitOfWork.CommitAsync(cancellationToken);
                return dueTasks.Select(ToScheduledTaskItem).ToArray();
            }
            catch
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }
        }
        finally
        {
            DueTaskCompletionGate.Release();
        }
    }

    public async Task<ScheduledTaskItem?> CancelQueuedTaskAsync(
        Guid scheduledTaskId,
        string message,
        CancellationToken cancellationToken = default)
    {
        await using var unitOfWork = unitOfWorkFactory.Create();
        var now = DateTimeOffset.UtcNow;
        var task = await unitOfWork.ScheduledTasks.GetQueuedByIdAsync(scheduledTaskId, cancellationToken);
        if (task is null)
        {
            return null;
        }

        try
        {
            task.MarkCancelled(message, now);
            await ReorderQueueAsync(unitOfWork, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
            return ToScheduledTaskItem(task);
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<ScheduledTaskItem>> CancelQueuedRestartTasksAsync(
        string message,
        CancellationToken cancellationToken = default)
    {
        await using var unitOfWork = unitOfWorkFactory.Create();
        var now = DateTimeOffset.UtcNow;
        var queuedTasks = await unitOfWork.ScheduledTasks.ListQueuedAsync(cancellationToken);
        var restartTasks = queuedTasks
            .Where(task => task.TaskType == ScheduledTask.RestartTaskType)
            .ToArray();

        if (restartTasks.Length == 0)
        {
            return Array.Empty<ScheduledTaskItem>();
        }

        try
        {
            foreach (var task in restartTasks)
            {
                task.MarkCancelled(message, now);
            }

            await ReorderQueueAsync(unitOfWork, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
            return restartTasks.Select(ToScheduledTaskItem).ToArray();
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> DeleteTaskAsync(Guid scheduledTaskId, CancellationToken cancellationToken = default)
    {
        await using var unitOfWork = unitOfWorkFactory.Create();
        var task = await unitOfWork.ScheduledTasks.GetByIdAsync(scheduledTaskId, cancellationToken);
        if (task is null)
        {
            return false;
        }

        try
        {
            unitOfWork.ScheduledTasks.Delete(task);
            await ReorderQueueAsync(unitOfWork, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static TimeSpan NormalizeDelay(TimeSpan delay)
    {
        if (delay < MinimumDelay)
        {
            return MinimumDelay;
        }

        return delay > MaximumDelay ? MaximumDelay : delay;
    }

    private async Task CompleteMessageAutomationTaskAsync(
        ScheduledTask task,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        try
        {
            await messageAutomationService.SendMessageToSpecificPersonAsync(
                new SendMessageRequest
                {
                    AppPath = task.AppPath ?? string.Empty,
                    AppDisplayName = task.AppDisplayName ?? string.Empty,
                    RecipientNames = ParseRecipients(task.RecipientNames).ToArray(),
                    Message = task.MessageText ?? string.Empty,
                },
                cancellationToken);

            if (task.RepeatMode == ScheduledTask.WeeklyRepeatMode && task.RepeatDaysOfWeek > 0)
            {
                var next = NextWeeklyOccurrenceUtc(task.ScheduledForUtc.ToLocalTime(), task.RepeatDaysOfWeek, now);
                task.Reschedule(next, $"Message sent. Next run {next.ToLocalTime():MMM d, yyyy h:mm tt}.");
                return;
            }

            task.MarkCompleted("Message sent.", now);
        }
        catch (Exception ex)
        {
            task.MarkFailed($"Message automation failed: {ex.Message}", now);
        }
    }

    private static IReadOnlyList<string> NormalizeRecipients(IReadOnlyList<string> recipients)
    {
        var normalized = recipients
            .Select(recipient => recipient.Trim())
            .Where(recipient => !string.IsNullOrWhiteSpace(recipient))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalized.Length == 0)
        {
            throw new ArgumentException("At least one recipient is required.", nameof(recipients));
        }

        return normalized;
    }

    private static IReadOnlyList<string> ParseRecipients(string? recipientNames)
    {
        return string.IsNullOrWhiteSpace(recipientNames)
            ? Array.Empty<string>()
            : recipientNames.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string NormalizeRepeatMode(string repeatMode)
    {
        return repeatMode.Equals(ScheduledTask.WeeklyRepeatMode, StringComparison.OrdinalIgnoreCase)
            ? ScheduledTask.WeeklyRepeatMode
            : ScheduledTask.NoRepeatMode;
    }

    private static int NormalizeRepeatDays(int repeatDaysOfWeek, DayOfWeek fallbackDay)
    {
        var normalized = repeatDaysOfWeek & 0b0111_1111;
        return normalized == 0 ? DayMask(fallbackDay) : normalized;
    }

    private static DateTimeOffset EnsureFutureUtc(DateTimeOffset scheduledForUtc, DateTimeOffset now)
    {
        if (scheduledForUtc <= now)
        {
            throw new ArgumentOutOfRangeException(nameof(scheduledForUtc), "Schedule time must be in the future.");
        }

        return scheduledForUtc;
    }

    private static DateTimeOffset NextWeeklyOccurrenceUtc(
        DateTimeOffset scheduledForLocal,
        int repeatDaysOfWeek,
        DateTimeOffset nowUtc)
    {
        var localNow = nowUtc.ToLocalTime();
        var localTime = scheduledForLocal.TimeOfDay;

        for (var offset = 0; offset <= 7; offset++)
        {
            var candidateDate = localNow.Date.AddDays(offset);
            if ((repeatDaysOfWeek & DayMask(candidateDate.DayOfWeek)) == 0)
            {
                continue;
            }

            var candidateLocal = new DateTimeOffset(candidateDate.Add(localTime), localNow.Offset);
            if (candidateLocal.ToUniversalTime() > nowUtc)
            {
                return candidateLocal.ToUniversalTime();
            }
        }

        return new DateTimeOffset(localNow.Date.AddDays(7).Add(localTime), localNow.Offset).ToUniversalTime();
    }

    private static int DayMask(DayOfWeek dayOfWeek)
    {
        return 1 << (int)dayOfWeek;
    }

    private static async Task ReorderQueueAsync(
        ICommandHistoryUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var queuedTasks = await unitOfWork.ScheduledTasks.ListQueuedAsync(cancellationToken);
        ReorderQueue(queuedTasks);
    }

    private static async Task ReorderQueueAsync(
        ICommandHistoryUnitOfWork unitOfWork,
        ScheduledTask newTask,
        CancellationToken cancellationToken)
    {
        var queuedTasks = await unitOfWork.ScheduledTasks.ListQueuedAsync(cancellationToken);
        ReorderQueue(queuedTasks.Concat([newTask]));
    }

    private static void ReorderQueue(IEnumerable<ScheduledTask> queuedTasks)
    {
        var queuePosition = 1;
        foreach (var task in SortQueue(queuedTasks))
        {
            task.SetQueuePosition(queuePosition++);
        }
    }

    private static IEnumerable<ScheduledTask> SortQueue(IEnumerable<ScheduledTask> tasks)
    {
        return tasks
            .DistinctBy(task => task.Id)
            .OrderBy(task => task.ScheduledForUtc)
            .ThenByDescending(task => task.Priority)
            .ThenBy(task => task.CreatedAtUtc)
            .ThenBy(task => task.Id);
    }

    private static ScheduledTaskItem ToScheduledTaskItem(ScheduledTask task)
    {
        return new ScheduledTaskItem(
            task.Id,
            task.ChatSessionId,
            task.CommandLogId,
            task.TaskType,
            task.DisplayName,
            task.QueueStatus,
            task.Priority,
            task.QueuePosition,
            task.ScheduledForUtc,
            task.CreatedAtUtc,
            task.CompletedAtUtc,
            task.LastMessage,
            task.AppPath,
            task.AppDisplayName,
            ParseRecipients(task.RecipientNames),
            task.MessageText,
            task.RepeatMode,
            task.RepeatDaysOfWeek);
    }
}
