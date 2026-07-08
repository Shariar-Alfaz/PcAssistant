namespace PcAssistant.Application.Models;

public sealed record ScheduleMessageAutomationRequest(
    string Title,
    string AppPath,
    string AppDisplayName,
    IReadOnlyList<string> RecipientNames,
    string Message,
    DateTimeOffset ScheduledForLocal,
    string RepeatMode,
    int RepeatDaysOfWeek);

public sealed record ScheduleWebAutomationRequest(
    Guid FlowId,
    string Title,
    string StartUrl,
    DateTimeOffset ScheduledForLocal,
    string RepeatMode,
    int RepeatDaysOfWeek);
