namespace PcAssistant.Application.Models;

public sealed record ScheduledTaskItem(
    Guid Id,
    Guid? ChatSessionId,
    Guid? CommandLogId,
    string TaskType,
    string DisplayName,
    string QueueStatus,
    int Priority,
    int QueuePosition,
    DateTimeOffset ScheduledForUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string? LastMessage,
    string? AppPath,
    string? AppDisplayName,
    IReadOnlyList<string> RecipientNames,
    string? MessageText,
    string RepeatMode,
    int RepeatDaysOfWeek);
