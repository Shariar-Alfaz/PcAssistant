using PcAssistant.Domain.Enums;

namespace PcAssistant.Domain.Entity;

public sealed class WebAutomationRun
{
    private readonly List<WebAutomationRunStepLog> _stepLogs = new();

    private WebAutomationRun()
    {
    }

    private WebAutomationRun(Guid id, Guid flowId, WebAutomationRunStatus status, DateTime startedAtUtc, string? browserChannel, bool headless)
    {
        Id = id;
        FlowId = flowId;
        Status = status;
        StartedAtUtc = ToUtc(startedAtUtc);
        BrowserChannel = string.IsNullOrWhiteSpace(browserChannel) ? "msedge" : browserChannel.Trim();
        Headless = headless;
    }

    public Guid Id { get; private set; }
    public Guid FlowId { get; private set; }
    public WebAutomationRunStatus Status { get; private set; }
    public DateTime StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? BrowserChannel { get; private set; }
    public bool Headless { get; private set; }
    public IReadOnlyCollection<WebAutomationRunStepLog> StepLogs => _stepLogs;

    public static WebAutomationRun Start(Guid flowId, string? browserChannel, bool headless, DateTime startedAtUtc)
    {
        return new WebAutomationRun(Guid.NewGuid(), flowId, WebAutomationRunStatus.Running, startedAtUtc, browserChannel, headless);
    }

    public static WebAutomationRun Start(Guid id, Guid flowId, string? browserChannel, bool headless, DateTime startedAtUtc)
    {
        return new WebAutomationRun(id, flowId, WebAutomationRunStatus.Running, startedAtUtc, browserChannel, headless);
    }

    public void Complete(WebAutomationRunStatus status, string? errorMessage, DateTime completedAtUtc)
    {
        Status = status;
        ErrorMessage = string.IsNullOrWhiteSpace(errorMessage) ? null : errorMessage.Trim();
        CompletedAtUtc = ToUtc(completedAtUtc);
    }

    private static DateTime ToUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
    }
}
