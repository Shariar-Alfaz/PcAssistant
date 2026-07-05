namespace PcAssistant.Application.Models;

public sealed record ChatSessionSummary(
    Guid Id,
    string Title,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    int MessageCount);
