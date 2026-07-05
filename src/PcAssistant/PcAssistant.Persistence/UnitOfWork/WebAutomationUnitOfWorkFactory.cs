using Autofac;
using PcAssistant.Application.Abstractions.Repositories;
using PcAssistant.Application.Abstractions.UnitOfWorks;

namespace PcAssistant.Persistence.UnitOfWork;

internal sealed class WebAutomationUnitOfWorkFactory(ILifetimeScope lifetimeScope) : IWebAutomationUnitOfWorkFactory
{
    public IWebAutomationUnitOfWork Create()
    {
        var scope = lifetimeScope.BeginLifetimeScope();
        return new ScopedWebAutomationUnitOfWork(scope.Resolve<WebAutomationUnitOfWork>(), scope);
    }

    private sealed class ScopedWebAutomationUnitOfWork(
        IWebAutomationUnitOfWork inner,
        ILifetimeScope scope) : IWebAutomationUnitOfWork
    {
        public IWebAutomationRepository Automations => inner.Automations;

        public IWebAutomationRunRepository Runs => inner.Runs;

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
