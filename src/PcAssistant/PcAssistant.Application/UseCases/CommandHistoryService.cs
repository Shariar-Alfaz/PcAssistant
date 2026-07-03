using PcAssistant.Application.Abstractions;
using PcAssistant.Application.Models;
using PcAssistant.Domain;

namespace PcAssistant.Application.UseCases;

public sealed class CommandHistoryService(ICommandHistoryUnitOfWorkFactory unitOfWorkFactory) : ICommandHistoryService
{
    public async Task<IReadOnlyList<CommandHistoryItem>> ListRecentAsync(
        int count,
        CancellationToken cancellationToken = default)
    {
        await using var unitOfWork = unitOfWorkFactory.Create();
        var entries = await unitOfWork.CommandLogs.ListRecentAsync(count, cancellationToken);
        return entries.Select(ToHistoryItem).ToArray();
    }

    public async Task<CommandHistoryItem> RecordPredictionAsync(
        string userText,
        CommandResponse response,
        ParsedCommand parsedCommand,
        string effectiveLabel,
        string assistantMessage,
        bool lowConfidenceRequiresConfirmation,
        CancellationToken cancellationToken = default)
    {
        await using var unitOfWork = unitOfWorkFactory.Create();
        var entry = CommandLogEntry.CreatePrediction(
            userText,
            effectiveLabel,
            response.RawPredictedLabel,
            response.Source,
            response.Confidence,
            response.RequiresConfirmation || parsedCommand.RequiresConfirmation || lowConfidenceRequiresConfirmation,
            assistantMessage,
            parsedCommand.Preview,
            parsedCommand.ResolvedPath,
            parsedCommand.IsDangerous,
            DateTimeOffset.UtcNow);

        try
        {
            await unitOfWork.CommandLogs.AddAsync(entry, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
            return ToHistoryItem(entry);
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<CommandHistoryItem> RecordExecutionAsync(
        Guid commandLogId,
        CommandExecutionResult result,
        CancellationToken cancellationToken = default)
    {
        await using var unitOfWork = unitOfWorkFactory.Create();
        var entry = await GetRequiredEntryAsync(unitOfWork, commandLogId, cancellationToken);
        entry.MarkExecuted(result.Succeeded, result.Message, DateTimeOffset.UtcNow);

        try
        {
            await unitOfWork.CommitAsync(cancellationToken);
            return ToHistoryItem(entry);
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<CommandHistoryItem> UpdatePlannedTargetAsync(
        Guid commandLogId,
        ParsedCommand parsedCommand,
        string assistantMessage,
        CancellationToken cancellationToken = default)
    {
        await using var unitOfWork = unitOfWorkFactory.Create();
        var entry = await GetRequiredEntryAsync(unitOfWork, commandLogId, cancellationToken);
        entry.UpdatePlannedTarget(parsedCommand.Preview, parsedCommand.ResolvedPath, assistantMessage);

        try
        {
            await unitOfWork.CommitAsync(cancellationToken);
            return ToHistoryItem(entry);
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<CommandHistoryItem> RecordCancellationAsync(
        Guid commandLogId,
        string message,
        CancellationToken cancellationToken = default)
    {
        await using var unitOfWork = unitOfWorkFactory.Create();
        var entry = await GetRequiredEntryAsync(unitOfWork, commandLogId, cancellationToken);
        entry.MarkCancelled(message, DateTimeOffset.UtcNow);

        try
        {
            await unitOfWork.CommitAsync(cancellationToken);
            return ToHistoryItem(entry);
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task<CommandLogEntry> GetRequiredEntryAsync(
        ICommandHistoryUnitOfWork unitOfWork,
        Guid id,
        CancellationToken cancellationToken)
    {
        var entry = await unitOfWork.CommandLogs.GetByIdAsync(id, cancellationToken);
        return entry ?? throw new InvalidOperationException("Command log entry was not found.");
    }

    private static CommandHistoryItem ToHistoryItem(CommandLogEntry entry)
    {
        return new CommandHistoryItem(
            entry.Id,
            entry.UserText,
            entry.CommandLabel,
            entry.RawPredictedLabel,
            entry.Source,
            entry.Confidence,
            entry.RequiresConfirmation,
            entry.AssistantMessage,
            entry.Preview,
            entry.ResolvedPath,
            entry.IsDangerous,
            entry.ExecutionStatus,
            entry.ExecutionMessage,
            entry.CreatedAtUtc,
            entry.ExecutedAtUtc);
    }
}
