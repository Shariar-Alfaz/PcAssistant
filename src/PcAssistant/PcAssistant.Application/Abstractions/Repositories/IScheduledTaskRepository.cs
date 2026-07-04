using PcAssistant.Domain.Entity;

namespace PcAssistant.Application.Abstractions.Repositories;

public interface IScheduledTaskRepository
{
    Task AddAsync(ScheduledTask task, CancellationToken cancellationToken = default);

    Task<ScheduledTask?> GetQueuedByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScheduledTask>> ListQueuedAsync(CancellationToken cancellationToken = default);
}
