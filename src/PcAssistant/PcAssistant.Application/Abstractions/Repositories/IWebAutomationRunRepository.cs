using PcAssistant.Domain.Entity;

namespace PcAssistant.Application.Abstractions.Repositories;

public interface IWebAutomationRunRepository
{
    Task AddRunAsync(WebAutomationRun run, CancellationToken cancellationToken = default);

    Task AddStepLogAsync(WebAutomationRunStepLog stepLog, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WebAutomationRun>> ListRunsAsync(Guid flowId, CancellationToken cancellationToken = default);
}
