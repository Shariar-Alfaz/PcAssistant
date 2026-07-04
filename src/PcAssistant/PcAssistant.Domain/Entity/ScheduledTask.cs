namespace PcAssistant.Domain.Entity;

public sealed class ScheduledTask
{
    public const string RestartTaskType = "system.restart";
    public const string QueuedStatus = "queued";
    public const string CompletedStatus = "completed";
    public const string CancelledStatus = "cancelled";

    private ScheduledTask()
    {
    }

    private ScheduledTask(
        Guid id,
        Guid? chatSessionId,
        Guid? commandLogId,
        string taskType,
        string displayName,
        int priority,
        DateTimeOffset scheduledForUtc,
        DateTimeOffset createdAtUtc,
        string? lastMessage)
    {
        Id = id;
        ChatSessionId = chatSessionId;
        CommandLogId = commandLogId;
        TaskType = NormalizeRequired(taskType, nameof(taskType));
        DisplayName = NormalizeRequired(displayName, nameof(displayName));
        Priority = priority;
        ScheduledForUtc = scheduledForUtc.ToUniversalTime();
        CreatedAtUtc = createdAtUtc.ToUniversalTime();
        QueueStatus = QueuedStatus;
        LastMessage = string.IsNullOrWhiteSpace(lastMessage) ? null : lastMessage.Trim();
    }

    public Guid Id { get; private set; }

    public Guid? ChatSessionId { get; private set; }

    public Guid? CommandLogId { get; private set; }

    public string TaskType { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string QueueStatus { get; private set; } = QueuedStatus;

    public int Priority { get; private set; }

    public int QueuePosition { get; private set; }

    public DateTimeOffset ScheduledForUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public string? LastMessage { get; private set; }

    public static ScheduledTask QueueRestart(
        Guid? chatSessionId,
        Guid? commandLogId,
        DateTimeOffset scheduledForUtc,
        DateTimeOffset createdAtUtc,
        string? message)
    {
        return new ScheduledTask(
            Guid.NewGuid(),
            chatSessionId,
            commandLogId,
            RestartTaskType,
            "Windows restart",
            priority: 100,
            scheduledForUtc,
            createdAtUtc,
            message);
    }

    public void SetQueuePosition(int queuePosition)
    {
        if (queuePosition < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(queuePosition), "Queue position must be positive.");
        }

        QueuePosition = queuePosition;
    }

    public void MarkCompleted(string message, DateTimeOffset completedAtUtc)
    {
        QueueStatus = CompletedStatus;
        CompletedAtUtc = completedAtUtc.ToUniversalTime();
        LastMessage = string.IsNullOrWhiteSpace(message) ? "Task completed." : message.Trim();
    }

    public void MarkCancelled(string message, DateTimeOffset cancelledAtUtc)
    {
        QueueStatus = CancelledStatus;
        CompletedAtUtc = cancelledAtUtc.ToUniversalTime();
        LastMessage = string.IsNullOrWhiteSpace(message) ? "Task cancelled." : message.Trim();
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return value.Trim();
    }
}
