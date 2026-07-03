using PcAssistant.Application.Abstractions;
using PcAssistant.Persistence.Repositories;

namespace PcAssistant.Persistence;

internal sealed class CommandHistoryUnitOfWork(PcAssistantDbContext dbContext) : ICommandHistoryUnitOfWork
{
    private ICommandLogRepository? _commandLogs;

    public ICommandLogRepository CommandLogs => _commandLogs ??= new EfCommandLogRepository(dbContext);

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        dbContext.ChangeTracker.Clear();
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        return dbContext.DisposeAsync();
    }
}
