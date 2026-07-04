using PcAssistant.Application.Abstractions;
using PcAssistant.Persistence.Repositories;

namespace PcAssistant.Persistence;

internal sealed class CommandHistoryUnitOfWork(IPcAssistantDbContext dbContext) : ICommandHistoryUnitOfWork
{
    private IChatSessionRepository? _chatSessions;
    private ICommandLogRepository? _commandLogs;

    public IChatSessionRepository ChatSessions => _chatSessions ??= new EfChatSessionRepository(dbContext);

    public ICommandLogRepository CommandLogs => _commandLogs ??= new EfCommandLogRepository(dbContext);

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
        return dbContext.DisposeAsync();
    }
}
