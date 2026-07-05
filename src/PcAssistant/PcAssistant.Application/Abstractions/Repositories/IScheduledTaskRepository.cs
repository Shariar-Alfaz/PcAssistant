using PcAssistant.Domain.Entity;

namespace PcAssistant.Application.Abstractions.Repositories;

public interface IScheduledTaskRepository
{
    Task AddAsync(ScheduledTask task, CancellationToken cancellationToken = default);

    Task<ScheduledTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ScheduledTask?> GetQueuedByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScheduledTask>> ListQueuedAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScheduledTask>> ListByTaskTypeAsync(
        string taskType,
        string? searchText,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<int> CountByTaskTypeAsync(
        string taskType,
        string? searchText,
        CancellationToken cancellationToken = default);

    void Delete(ScheduledTask task);
}
