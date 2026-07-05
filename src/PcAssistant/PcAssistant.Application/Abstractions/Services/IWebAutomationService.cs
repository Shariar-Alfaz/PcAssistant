using PcAssistant.Application.Models;

namespace PcAssistant.Application.Abstractions.Services;

public interface IWebAutomationService
{
    Task<WebAutomationRunDto> RunFlowAsync(
        WebAutomationFlowDto flow,
        bool runHeaded,
        CancellationToken cancellationToken = default);

    Task<WebAutomationRunStepLogDto> RunSingleStepAsync(
        WebAutomationFlowDto flow,
        WebAutomationStepDto step,
        bool runHeaded,
        CancellationToken cancellationToken = default);
}
