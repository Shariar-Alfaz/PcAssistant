using PcAssistant.Application.Abstractions;
using PcAssistant.Application.Models;
using PcAssistant.Domain;

namespace PcAssistant.Application.UseCases;

public sealed class CommandHistoryService(ICommandHistoryUnitOfWorkFactory unitOfWorkFactory) : ICommandHistoryService
{
    public async Task<IReadOnlyList<ChatSessionSummary>> ListChatsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var unitOfWork = unitOfWorkFactory.Create();
        var sessions = await unitOfWork.ChatSessions.ListAsync(cancellationToken);
        return sessions.Select(ToChatSummary).ToArray();
    }

    public async Task<ChatSessionDetails> GetChatAsync(
        Guid chatSessionId,
        CancellationToken cancellationToken = default)
    {
        await using var unitOfWork = unitOfWorkFactory.Create();
        var session = await GetRequiredSessionAsync(unitOfWork, chatSessionId, cancellationToken);
        var entries = await unitOfWork.CommandLogs.ListByChatSessionAsync(chatSessionId, cancellationToken);
        return new ChatSessionDetails(
            ToChatSummary(session),
            entries.Select(ToHistoryItem).ToArray());
    }

    public async Task<ChatSessionSummary> CreateChatAsync(CancellationToken cancellationToken = default)
    {
        await using var unitOfWork = unitOfWorkFactory.Create();
        var session = ChatSession.Create(ChatSession.DefaultTitle, DateTimeOffset.UtcNow);

        try
        {
            await unitOfWork.ChatSessions.AddAsync(session, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
            return ToChatSummary(session);
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task DeleteChatAsync(Guid chatSessionId, CancellationToken cancellationToken = default)
    {
        await using var unitOfWork = unitOfWorkFactory.Create();
        var session = await GetRequiredSessionAsync(unitOfWork, chatSessionId, cancellationToken);

        try
        {
            unitOfWork.ChatSessions.Remove(session);
            await unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<CommandHistoryItem>> ListRecentAsync(
        int count,
        CancellationToken cancellationToken = default)
    {
        await using var unitOfWork = unitOfWorkFactory.Create();
        var entries = await unitOfWork.CommandLogs.ListRecentAsync(count, cancellationToken);
        return entries.Select(ToHistoryItem).ToArray();
    }

    public async Task<CommandHistoryItem> RecordPredictionAsync(
        Guid chatSessionId,
        string userText,
        CommandResponse response,
        ParsedCommand parsedCommand,
        string effectiveLabel,
        string assistantMessage,
        bool lowConfidenceRequiresConfirmation,
        CancellationToken cancellationToken = default)
    {
        await using var unitOfWork = unitOfWorkFactory.Create();
        var session = await GetRequiredSessionAsync(unitOfWork, chatSessionId, cancellationToken);
        session.RenameFromFirstMessage(userText);
        session.Touch(DateTimeOffset.UtcNow);

        var entry = CommandLogEntry.CreatePrediction(
            chatSessionId,
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

    private static async Task<ChatSession> GetRequiredSessionAsync(
        ICommandHistoryUnitOfWork unitOfWork,
        Guid id,
        CancellationToken cancellationToken)
    {
        var session = await unitOfWork.ChatSessions.GetByIdAsync(id, cancellationToken);
        return session ?? throw new InvalidOperationException("Chat session was not found.");
    }

    private static CommandHistoryItem ToHistoryItem(CommandLogEntry entry)
    {
        return new CommandHistoryItem(
            entry.Id,
            entry.ChatSessionId,
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

    private static ChatSessionSummary ToChatSummary(ChatSession session)
    {
        return new ChatSessionSummary(
            session.Id,
            session.Title,
            session.CreatedAtUtc,
            session.UpdatedAtUtc,
            session.CommandLogs.Count);
    }
}
