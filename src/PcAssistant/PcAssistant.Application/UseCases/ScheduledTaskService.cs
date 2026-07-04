using PcAssistant.Application.Abstractions.Services;
using PcAssistant.Application.Abstractions.UnitOfWorks;
using PcAssistant.Application.Models;
using PcAssistant.Domain.Entity;

namespace PcAssistant.Application.UseCases;

public sealed class ScheduledTaskService(ICommandHistoryUnitOfWorkFactory unitOfWorkFactory) : IScheduledTaskService
{
    private static readonly TimeSpan MinimumDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MaximumDelay = TimeSpan.FromHours(24);

    public async Task<IReadOnlyList<ScheduledTaskItem>> ListQueuedAsync(CancellationToken cancellationToken = default)
    {
        await using var unitOfWork = unitOfWorkFactory.Create();
        var queuedTasks = await unitOfWork.ScheduledTasks.ListQueuedAsync(cancellationToken);
        return SortQueue(queuedTasks).Select(ToScheduledTaskItem).ToArray();
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

    public async Task<IReadOnlyList<ScheduledTaskItem>> CompleteDueTasksAsync(CancellationToken cancellationToken = default)
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

    private static TimeSpan NormalizeDelay(TimeSpan delay)
    {
        if (delay < MinimumDelay)
        {
            return MinimumDelay;
        }

        return delay > MaximumDelay ? MaximumDelay : delay;
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
            task.LastMessage);
    }
}
