using PcAssistant.Domain.Enums;

namespace PcAssistant.Domain.Entity;

public sealed class WebAutomationRunStepLog
{
    private WebAutomationRunStepLog()
    {
    }

    private WebAutomationRunStepLog(
        Guid id,
        Guid runId,
        Guid stepId,
        int orderIndex,
        WebAutomationStepRunStatus status,
        DateTime startedAtUtc,
        DateTime? completedAtUtc,
        string? message,
        string? errorMessage,
        string? screenshotPath,
        string? extractedValue)
    {
        Id = id;
        RunId = runId;
        StepId = stepId;
        OrderIndex = orderIndex;
        Status = status;
        StartedAtUtc = ToUtc(startedAtUtc);
        CompletedAtUtc = completedAtUtc.HasValue ? ToUtc(completedAtUtc.Value) : null;
        Message = NormalizeOptional(message);
        ErrorMessage = NormalizeOptional(errorMessage);
        ScreenshotPath = NormalizeOptional(screenshotPath);
        ExtractedValue = NormalizeOptional(extractedValue);
    }

    public Guid Id { get; private set; }
    public Guid RunId { get; private set; }
    public Guid StepId { get; private set; }
    public int OrderIndex { get; private set; }
    public WebAutomationStepRunStatus Status { get; private set; }
    public DateTime StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public string? Message { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? ScreenshotPath { get; private set; }
    public string? ExtractedValue { get; private set; }

    public static WebAutomationRunStepLog Create(
        Guid runId,
        Guid stepId,
        int orderIndex,
        WebAutomationStepRunStatus status,
        DateTime startedAtUtc,
        DateTime? completedAtUtc,
        string? message,
        string? errorMessage,
        string? screenshotPath,
        string? extractedValue)
    {
        return new WebAutomationRunStepLog(Guid.NewGuid(), runId, stepId, orderIndex, status, startedAtUtc, completedAtUtc, message, errorMessage, screenshotPath, extractedValue);
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
