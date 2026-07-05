using PcAssistant.Domain.Enums;

namespace PcAssistant.Domain.Entity;

public sealed class WebAutomationStep
{
    private readonly List<WebAutomationRunStepLog> _runStepLogs = new();

    private WebAutomationStep()
    {
    }

    private WebAutomationStep(
        Guid id,
        Guid flowId,
        int orderIndex,
        WebAutomationStepType stepType,
        WebSelectorType selectorType,
        string? selector,
        string? value,
        string? url,
        string? description,
        int timeoutMs,
        int delayAfterMs,
        bool isOptional,
        bool takeScreenshotAfterStep,
        int retryCount,
        DateTime createdAtUtc)
    {
        Id = id;
        FlowId = flowId;
        OrderIndex = orderIndex;
        StepType = stepType;
        SelectorType = selectorType;
        Selector = NormalizeOptional(selector);
        Value = NormalizeOptional(value);
        Url = NormalizeOptional(url);
        Description = NormalizeOptional(description);
        TimeoutMs = Math.Clamp(timeoutMs, 1000, 120000);
        DelayAfterMs = Math.Max(0, delayAfterMs);
        IsOptional = isOptional;
        TakeScreenshotAfterStep = takeScreenshotAfterStep;
        RetryCount = Math.Clamp(retryCount, 0, 5);
        CreatedAtUtc = ToUtc(createdAtUtc);
    }

    public Guid Id { get; private set; }
    public Guid FlowId { get; private set; }
    public int OrderIndex { get; private set; }
    public WebAutomationStepType StepType { get; private set; }
    public WebSelectorType SelectorType { get; private set; }
    public string? Selector { get; private set; }
    public string? Value { get; private set; }
    public string? Url { get; private set; }
    public string? Description { get; private set; }
    public int TimeoutMs { get; private set; }
    public int DelayAfterMs { get; private set; }
    public bool IsOptional { get; private set; }
    public bool TakeScreenshotAfterStep { get; private set; }
    public int RetryCount { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public WebSelectorSnapshot? SelectorSnapshot { get; private set; }
    public IReadOnlyCollection<WebAutomationRunStepLog> RunStepLogs => _runStepLogs;

    public static WebAutomationStep Create(
        Guid flowId,
        int orderIndex,
        WebAutomationStepType stepType,
        WebSelectorType selectorType,
        string? selector,
        string? value,
        string? url,
        string? description,
        int timeoutMs,
        int delayAfterMs,
        bool isOptional,
        bool takeScreenshotAfterStep,
        int retryCount,
        DateTime createdAtUtc)
    {
        return new WebAutomationStep(Guid.NewGuid(), flowId, orderIndex, stepType, selectorType, selector, value, url, description, timeoutMs, delayAfterMs, isOptional, takeScreenshotAfterStep, retryCount, createdAtUtc);
    }

    public void Update(
        WebAutomationStepType stepType,
        WebSelectorType selectorType,
        string? selector,
        string? value,
        string? url,
        string? description,
        int timeoutMs,
        int delayAfterMs,
        bool isOptional,
        bool takeScreenshotAfterStep,
        int retryCount,
        DateTime updatedAtUtc)
    {
        StepType = stepType;
        SelectorType = selectorType;
        Selector = NormalizeOptional(selector);
        Value = NormalizeOptional(value);
        Url = NormalizeOptional(url);
        Description = NormalizeOptional(description);
        TimeoutMs = Math.Clamp(timeoutMs, 1000, 120000);
        DelayAfterMs = Math.Max(0, delayAfterMs);
        IsOptional = isOptional;
        TakeScreenshotAfterStep = takeScreenshotAfterStep;
        RetryCount = Math.Clamp(retryCount, 0, 5);
        UpdatedAtUtc = ToUtc(updatedAtUtc);
    }

    public void SetOrderIndex(int orderIndex, DateTime updatedAtUtc)
    {
        if (orderIndex < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(orderIndex), "Order index must start at one.");
        }

        OrderIndex = orderIndex;
        UpdatedAtUtc = ToUtc(updatedAtUtc);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static DateTime ToUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
    }
}
