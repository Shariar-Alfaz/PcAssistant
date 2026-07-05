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
