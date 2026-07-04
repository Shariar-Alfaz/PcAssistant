namespace PcAssistant.Application.Models;

public sealed record ChatSessionDetails(
    ChatSessionSummary Summary,
    IReadOnlyList<CommandHistoryItem> Messages);
