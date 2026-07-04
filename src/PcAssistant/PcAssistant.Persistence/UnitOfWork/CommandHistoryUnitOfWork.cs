using PcAssistant.Application.Abstractions.Repositories;
using PcAssistant.Application.Abstractions.Services;
using PcAssistant.Application.Abstractions.UnitOfWorks;
using PcAssistant.Persistence.Database;

namespace PcAssistant.Persistence.UnitOfWork;

internal sealed class CommandHistoryUnitOfWork(
    IPcAssistantDbContext dbContext,
    IChatSessionRepository chatSessions,
    ICommandLogRepository commandLogs,
    IScheduledTaskRepository scheduledTasks) : ICommandHistoryUnitOfWork
{
    public IChatSessionRepository ChatSessions { get; } = chatSessions;

    public ICommandLogRepository CommandLogs { get; } = commandLogs;

    public IScheduledTaskRepository ScheduledTasks { get; } = scheduledTasks;

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        dbContext.ClearTrackedChanges();
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
