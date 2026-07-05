using PcAssistant.Domain.Enums;

namespace PcAssistant.Application.Models;

public sealed record CreateWebAutomationProjectRequest(string Name, string? Description, string StartUrl);

public sealed record UpdateWebAutomationProjectRequest(Guid ProjectId, string Name, string? Description, string StartUrl);

public sealed record CreateWebAutomationFlowRequest(
    Guid ProjectId,
    string Name,
    string StartUrl,
    bool Headless,
    string BrowserChannel,
    bool UsePersistentSession,
    int DefaultTimeoutMs);

public sealed record UpdateWebAutomationFlowRequest(
    Guid FlowId,
    string Name,
    string StartUrl,
    bool Headless,
    string BrowserChannel,
    bool UsePersistentSession,
    int DefaultTimeoutMs);

public sealed record CreateWebAutomationStepRequest(
    Guid FlowId,
    WebAutomationStepType StepType,
    WebSelectorType SelectorType,
    string? Selector,
    string? Value,
    string? Url,
    string? Description,
    int TimeoutMs,
    int DelayAfterMs,
    bool IsOptional,
    bool TakeScreenshotAfterStep,
    int RetryCount);

public sealed record UpdateWebAutomationStepRequest(
    Guid StepId,
    WebAutomationStepType StepType,
    WebSelectorType SelectorType,
    string? Selector,
    string? Value,
    string? Url,
    string? Description,
    int TimeoutMs,
    int DelayAfterMs,
    bool IsOptional,
    bool TakeScreenshotAfterStep,
    int RetryCount);

public sealed record ReorderWebAutomationStepsRequest(Guid FlowId, IReadOnlyList<Guid> StepIds);

public sealed record RunWebAutomationFlowRequest(Guid FlowId, bool RunHeaded, bool RequireConfirmation);

public sealed record WebAutomationProjectDto(
    Guid Id,
    string Name,
    string? Description,
    string StartUrl,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    IReadOnlyList<WebAutomationFlowDto> Flows);

public sealed record WebAutomationFlowDto(
    Guid Id,
    Guid ProjectId,
    string Name,
    string StartUrl,
    bool Headless,
    string BrowserChannel,
    bool UsePersistentSession,
    int DefaultTimeoutMs,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    IReadOnlyList<WebAutomationStepDto> Steps);

public sealed record WebAutomationStepDto(
    Guid Id,
    Guid FlowId,
    int OrderIndex,
    WebAutomationStepType StepType,
    WebSelectorType SelectorType,
    string? Selector,
    string? Value,
    string? Url,
    string? Description,
    int TimeoutMs,
    int DelayAfterMs,
    bool IsOptional,
    bool TakeScreenshotAfterStep,
    int RetryCount,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record WebAutomationRunDto(
    Guid Id,
    Guid FlowId,
    WebAutomationRunStatus Status,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    string? ErrorMessage,
    string? BrowserChannel,
    bool Headless,
    IReadOnlyList<WebAutomationRunStepLogDto> StepLogs);

public sealed record WebAutomationRunStepLogDto(
    Guid Id,
    Guid RunId,
    Guid StepId,
    int OrderIndex,
    WebAutomationStepRunStatus Status,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    string? Message,
    string? ErrorMessage,
    string? ScreenshotPath,
    string? ExtractedValue);

public sealed record WebPreviewDto(
    string Url,
    bool CanUseInlinePreview,
    string? InlinePreviewUrl,
    string? ScreenshotPath,
    string? Message);

public sealed record WebAutomationOperationResult<T>(bool Succeeded, T? Value, string? ErrorMessage)
{
    public static WebAutomationOperationResult<T> Success(T value)
    {
        return new WebAutomationOperationResult<T>(true, value, null);
    }

    public static WebAutomationOperationResult<T> Failure(string errorMessage)
    {
        return new WebAutomationOperationResult<T>(false, default, errorMessage);
    }
}
