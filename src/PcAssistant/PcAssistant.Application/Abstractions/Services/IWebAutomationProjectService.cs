using PcAssistant.Application.Models;

namespace PcAssistant.Application.Abstractions.Services;

public interface IWebAutomationProjectService
{
    Task<WebAutomationOperationResult<WebAutomationProjectDto>> CreateProjectAsync(CreateWebAutomationProjectRequest request, CancellationToken cancellationToken = default);

    Task<WebAutomationOperationResult<WebAutomationProjectDto>> UpdateProjectAsync(UpdateWebAutomationProjectRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WebAutomationProjectDto>> GetProjectsAsync(CancellationToken cancellationToken = default);

    Task<WebAutomationProjectDto?> GetProjectDetailsAsync(Guid projectId, CancellationToken cancellationToken = default);

    Task<WebAutomationOperationResult<WebAutomationFlowDto>> CreateFlowAsync(CreateWebAutomationFlowRequest request, CancellationToken cancellationToken = default);

    Task<WebAutomationOperationResult<WebAutomationFlowDto>> UpdateFlowAsync(UpdateWebAutomationFlowRequest request, CancellationToken cancellationToken = default);

    Task<WebAutomationOperationResult<WebAutomationStepDto>> AddStepAsync(CreateWebAutomationStepRequest request, CancellationToken cancellationToken = default);

    Task<WebAutomationOperationResult<WebAutomationStepDto>> UpdateStepAsync(UpdateWebAutomationStepRequest request, CancellationToken cancellationToken = default);

    Task<WebAutomationOperationResult<bool>> DeleteStepAsync(Guid stepId, CancellationToken cancellationToken = default);

    Task<WebAutomationOperationResult<WebAutomationStepDto>> DuplicateStepAsync(Guid stepId, CancellationToken cancellationToken = default);

    Task<WebAutomationOperationResult<IReadOnlyList<WebAutomationStepDto>>> ReorderStepsAsync(ReorderWebAutomationStepsRequest request, CancellationToken cancellationToken = default);

    Task<WebAutomationOperationResult<WebAutomationRunDto>> RunFlowAsync(RunWebAutomationFlowRequest request, CancellationToken cancellationToken = default);

    Task<WebAutomationOperationResult<WebAutomationRunStepLogDto>> RunSingleStepAsync(Guid flowId, Guid stepId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WebAutomationRunDto>> GetRunLogsAsync(Guid flowId, CancellationToken cancellationToken = default);
}
