using PcAssistant.Application.Models;

namespace PcAssistant.Application.Abstractions.Services;

public interface IScheduledTaskService
{
    Task<IReadOnlyList<ScheduledTaskItem>> ListQueuedAsync(CancellationToken cancellationToken = default);

    Task<ScheduledTaskPage> ListByTaskTypeAsync(
        string taskType,
        int pageNumber,
        int pageSize,
        string? searchText = null,
        CancellationToken cancellationToken = default);

    Task<ScheduledTaskItem> QueueRestartAsync(
        Guid? chatSessionId,
        Guid? commandLogId,
        TimeSpan delay,
        string? message,
        CancellationToken cancellationToken = default);

    Task<ScheduledTaskItem> ScheduleMessageAutomationAsync(
        ScheduleMessageAutomationRequest request,
        CancellationToken cancellationToken = default);

    Task<ScheduledTaskItem?> RescheduleMessageAutomationAsync(
        Guid scheduledTaskId,
        ScheduleMessageAutomationRequest request,
        CancellationToken cancellationToken = default);

    Task<ScheduledTaskItem> ScheduleWebAutomationAsync(
        ScheduleWebAutomationRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScheduledTaskItem>> CompleteDueTasksAsync(CancellationToken cancellationToken = default);

    Task<ScheduledTaskItem?> CancelQueuedTaskAsync(
        Guid scheduledTaskId,
        string message,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScheduledTaskItem>> CancelQueuedRestartTasksAsync(
        string message,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteTaskAsync(Guid scheduledTaskId, CancellationToken cancellationToken = default);
}
