using PcAssistant.Application.Models;

namespace PcAssistant.Application.Abstractions;

public interface ICommandHistoryService
{
    Task<IReadOnlyList<CommandHistoryItem>> ListRecentAsync(int count, CancellationToken cancellationToken = default);

    Task<CommandHistoryItem> RecordPredictionAsync(
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
