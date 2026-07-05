namespace PcAssistant.Application.Models;

public sealed record CommandHistoryItem(
    Guid Id,
    Guid ChatSessionId,
    string UserText,
    string CommandLabel,
    string RawPredictedLabel,
    string Source,
    double Confidence,
    bool RequiresConfirmation,
    string AssistantMessage,
    string? Preview,
    string? ResolvedPath,
    bool IsDangerous,
    string ExecutionStatus,
    string? ExecutionMessage,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ExecutedAtUtc);
