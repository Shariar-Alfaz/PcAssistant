using Autofac;
using PcAssistant.Application.Abstractions.Repositories;
using PcAssistant.Application.Abstractions.Services;
using PcAssistant.Application.Abstractions.UnitOfWorks;

namespace PcAssistant.Persistence.UnitOfWork;

internal sealed class CommandHistoryUnitOfWorkFactory(ILifetimeScope lifetimeScope) : ICommandHistoryUnitOfWorkFactory
{
    public ICommandHistoryUnitOfWork Create()
    {
        var scope = lifetimeScope.BeginLifetimeScope();
        return new ScopedCommandHistoryUnitOfWork(
            scope.Resolve<CommandHistoryUnitOfWork>(),
            scope);
    }

    private sealed class ScopedCommandHistoryUnitOfWork(
        ICommandHistoryUnitOfWork inner,
        ILifetimeScope scope) : ICommandHistoryUnitOfWork
    {
        public IChatSessionRepository ChatSessions => inner.ChatSessions;

        public ICommandLogRepository CommandLogs => inner.CommandLogs;

        public IScheduledTaskRepository ScheduledTasks => inner.ScheduledTasks;

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            return inner.CommitAsync(cancellationToken);
        }

        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            return inner.RollbackAsync(cancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            await inner.DisposeAsync();
            scope.Dispose();
        }
    }
}
