using PcAssistant.Application.Models;

namespace PcAssistant.Application.Abstractions;

public interface ICommandHistoryService
{
    Task<IReadOnlyList<ChatSessionSummary>> ListChatsAsync(CancellationToken cancellationToken = default);

    Task<ChatSessionDetails> GetChatAsync(Guid chatSessionId, CancellationToken cancellationToken = default);

    Task<ChatSessionSummary> CreateChatAsync(CancellationToken cancellationToken = default);

    Task DeleteChatAsync(Guid chatSessionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CommandHistoryItem>> ListRecentAsync(int count, CancellationToken cancellationToken = default);

    Task<CommandHistoryItem> RecordPredictionAsync(
        Guid chatSessionId,
        string userText,
        CommandResponse response,
        ParsedCommand parsedCommand,
        string effectiveLabel,
        string assistantMessage,
        bool lowConfidenceRequiresConfirmation,
        CancellationToken cancellationToken = default);

    Task<CommandHistoryItem> RecordExecutionAsync(
        Guid commandLogId,
        CommandExecutionResult result,
        CancellationToken cancellationToken = default);

    Task<CommandHistoryItem> UpdatePlannedTargetAsync(
        Guid commandLogId,
        ParsedCommand parsedCommand,
        string assistantMessage,
        CancellationToken cancellationToken = default);

    Task<CommandHistoryItem> RecordCancellationAsync(
        Guid commandLogId,
        string message,
        CancellationToken cancellationToken = default);
}
