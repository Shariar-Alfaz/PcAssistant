namespace PcAssistant.Domain.Entity;

public sealed class CommandLogEntry
{
    private CommandLogEntry()
    {
    }

    private CommandLogEntry(
        Guid id,
        Guid chatSessionId,
        string userText,
        string commandLabel,
        string rawPredictedLabel,
        string source,
        double confidence,
        bool requiresConfirmation,
        string assistantMessage,
        string? preview,
        string? resolvedPath,
        bool isDangerous,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        ChatSessionId = chatSessionId;
        UserText = userText;
        CommandLabel = commandLabel;
        RawPredictedLabel = rawPredictedLabel;
        Source = source;
        Confidence = confidence;
        RequiresConfirmation = requiresConfirmation;
        AssistantMessage = assistantMessage;
        Preview = preview;
        ResolvedPath = resolvedPath;
        IsDangerous = isDangerous;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid ChatSessionId { get; private set; }

    public string UserText { get; private set; } = string.Empty;

    public string CommandLabel { get; private set; } = "unknown";

    public string RawPredictedLabel { get; private set; } = "unknown";

    public string Source { get; private set; } = "client";

    public double Confidence { get; private set; }

    public bool RequiresConfirmation { get; private set; }

    public string AssistantMessage { get; private set; } = string.Empty;

    public string? Preview { get; private set; }

    public string? ResolvedPath { get; private set; }

    public bool IsDangerous { get; private set; }

    public string ExecutionStatus { get; private set; } = "pending";

    public string? ExecutionMessage { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? ExecutedAtUtc { get; private set; }

    public static CommandLogEntry CreatePrediction(
        Guid chatSessionId,
        string userText,
        string commandLabel,
        string rawPredictedLabel,
        string source,
        double confidence,
        bool requiresConfirmation,
        string assistantMessage,
        string? preview,
        string? resolvedPath,
        bool isDangerous,
        DateTimeOffset createdAtUtc)
    {
        if (chatSessionId == Guid.Empty)
        {
            throw new ArgumentException("Chat session is required.", nameof(chatSessionId));
        }

        if (string.IsNullOrWhiteSpace(userText))
        {
            throw new ArgumentException("Command text is required.", nameof(userText));
        }

        return new CommandLogEntry(
            Guid.NewGuid(),
            chatSessionId,
            userText.Trim(),
            NormalizeLabel(commandLabel),
            NormalizeLabel(rawPredictedLabel),
            string.IsNullOrWhiteSpace(source) ? "client" : source.Trim(),
            confidence,
            requiresConfirmation,
            string.IsNullOrWhiteSpace(assistantMessage) ? "No response was recorded." : assistantMessage.Trim(),
            string.IsNullOrWhiteSpace(preview) ? null : preview.Trim(),
            string.IsNullOrWhiteSpace(resolvedPath) ? null : resolvedPath.Trim(),
            isDangerous,
            createdAtUtc);
    }

    public void MarkExecuted(bool succeeded, string message, DateTimeOffset executedAtUtc)
    {
        ExecutionStatus = succeeded ? "succeeded" : "failed";
        ExecutionMessage = string.IsNullOrWhiteSpace(message) ? null : message.Trim();
        ExecutedAtUtc = executedAtUtc;
    }

    public void UpdatePlannedTarget(string? preview, string? resolvedPath, string assistantMessage)
    {
        Preview = string.IsNullOrWhiteSpace(preview) ? null : preview.Trim();
        ResolvedPath = string.IsNullOrWhiteSpace(resolvedPath) ? null : resolvedPath.Trim();
        AssistantMessage = string.IsNullOrWhiteSpace(assistantMessage) ? AssistantMessage : assistantMessage.Trim();
    }

    public void MarkCancelled(string message, DateTimeOffset cancelledAtUtc)
    {
        ExecutionStatus = "cancelled";
        ExecutionMessage = string.IsNullOrWhiteSpace(message) ? "Command cancelled." : message.Trim();
        ExecutedAtUtc = cancelledAtUtc;
    }

    private static string NormalizeLabel(string? label)
    {
        return string.IsNullOrWhiteSpace(label) ? "unknown" : label.Trim();
    }
}
