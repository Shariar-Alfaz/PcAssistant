using PcAssistant.Application.Abstractions.Repositories;

namespace PcAssistant.Application.Abstractions.UnitOfWorks;

public interface IWebAutomationUnitOfWork : IAsyncDisposable
{
    IWebAutomationRepository Automations { get; }

    IWebAutomationRunRepository Runs { get; }

    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);
}
