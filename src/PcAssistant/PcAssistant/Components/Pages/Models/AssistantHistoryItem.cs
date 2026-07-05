using PcAssistant.Application.Models;

namespace PcAssistant.Components.Pages.Models;

public sealed class AssistantHistoryItem
{
    private AssistantHistoryItem(
        Guid id,
        string userText,
        string commandLabel,
        string source,
        double confidence,
        string assistantMessage,
        string? resolvedPath,
        string executionStatus,
        string? executionMessage,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        UserText = userText;
        CommandLabel = commandLabel;
        Source = source;
        Confidence = confidence;
        AssistantMessage = assistantMessage;
        ResolvedPath = resolvedPath;
        ExecutionStatus = executionStatus;
        ExecutionMessage = executionMessage;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }

    public string UserText { get; }

    public string CommandLabel { get; }

    public string Source { get; }

    public double Confidence { get; }

    public string AssistantMessage { get; }

    public string? ResolvedPath { get; }

    public string ExecutionStatus { get; }

    public string? ExecutionMessage { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    public static AssistantHistoryItem FromCommandHistory(CommandHistoryItem item)
    {
        return new AssistantHistoryItem(
            item.Id,
            item.UserText,
            item.CommandLabel,
            item.Source,
            item.Confidence,
            item.AssistantMessage,
            item.ResolvedPath,
            item.ExecutionStatus,
            item.ExecutionMessage,
            item.CreatedAtUtc);
    }
}
