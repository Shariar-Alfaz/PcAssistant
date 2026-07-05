using PcAssistant.Domain.Entity;

namespace PcAssistant.Application.Abstractions.Repositories;

public interface IWebAutomationRepository
{
    Task AddProjectAsync(WebAutomationProject project, CancellationToken cancellationToken = default);

    Task<WebAutomationProject?> GetProjectAsync(Guid projectId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WebAutomationProject>> ListProjectsAsync(CancellationToken cancellationToken = default);

    Task<WebAutomationFlow?> GetFlowAsync(Guid flowId, CancellationToken cancellationToken = default);

    Task AddFlowAsync(WebAutomationFlow flow, CancellationToken cancellationToken = default);

    Task AddStepAsync(WebAutomationStep step, CancellationToken cancellationToken = default);

    Task<WebAutomationStep?> GetStepAsync(Guid stepId, CancellationToken cancellationToken = default);

    void DeleteStep(WebAutomationStep step);

    Task<int> CountStepsAsync(Guid flowId, CancellationToken cancellationToken = default);
}
