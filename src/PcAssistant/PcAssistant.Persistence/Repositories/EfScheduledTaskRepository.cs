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

    public Task<ScheduledTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.ScheduledTasks
            .FirstOrDefaultAsync(task => task.Id == id, cancellationToken);
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

    public async Task<IReadOnlyList<ScheduledTask>> ListByTaskTypeAsync(
        string taskType,
        string? searchText,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        return await ApplyTaskTypeSearch(dbContext.ScheduledTasks, taskType, searchText)
            .OrderBy(task => task.QueueStatus == ScheduledTask.QueuedStatus ? 0 : 1)
            .ThenBy(task => task.ScheduledForUtc)
            .ThenByDescending(task => task.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToArrayAsync(cancellationToken);
    }

    public Task<int> CountByTaskTypeAsync(
        string taskType,
        string? searchText,
        CancellationToken cancellationToken = default)
    {
        return ApplyTaskTypeSearch(dbContext.ScheduledTasks, taskType, searchText)
            .CountAsync(cancellationToken);
    }

    public void Delete(ScheduledTask task)
    {
        dbContext.ScheduledTasks.Remove(task);
    }

    private static IQueryable<ScheduledTask> ApplyTaskTypeSearch(
        IQueryable<ScheduledTask> query,
        string taskType,
        string? searchText)
    {
        query = query.Where(task => task.TaskType == taskType);
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return query;
        }

        var normalized = searchText.Trim();
        return query.Where(task =>
            task.DisplayName.Contains(normalized)
            || task.QueueStatus.Contains(normalized)
            || (task.LastMessage != null && task.LastMessage.Contains(normalized))
            || (task.AppPath != null && task.AppPath.Contains(normalized))
            || (task.AppDisplayName != null && task.AppDisplayName.Contains(normalized))
            || (task.RecipientNames != null && task.RecipientNames.Contains(normalized))
            || (task.MessageText != null && task.MessageText.Contains(normalized)));
    }
}
