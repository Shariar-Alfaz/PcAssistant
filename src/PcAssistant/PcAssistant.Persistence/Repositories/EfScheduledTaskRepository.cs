using Microsoft.EntityFrameworkCore;
using PcAssistant.Application.Abstractions.Repositories;
using PcAssistant.Domain.Entity;
using PcAssistant.Persistence.Database;

namespace PcAssistant.Persistence.Repositories;

internal sealed class EfScheduledTaskRepository(IPcAssistantDbContext dbContext) : IScheduledTaskRepository
{
    public async Task AddAsync(ScheduledTask task, CancellationToken cancellationToken = default)
    {
        await dbContext.ScheduledTasks.AddAsync(task, cancellationToken);
    }

    public Task<ScheduledTask?> GetQueuedByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.ScheduledTasks
            .FirstOrDefaultAsync(
                task => task.Id == id && task.QueueStatus == ScheduledTask.QueuedStatus,
                cancellationToken);
    }

    public async Task<IReadOnlyList<ScheduledTask>> ListQueuedAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.ScheduledTasks
            .Where(task => task.QueueStatus == ScheduledTask.QueuedStatus)
            .OrderBy(task => task.ScheduledForUtc)
            .ThenByDescending(task => task.Priority)
            .ThenBy(task => task.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
    }
}
