namespace PcAssistant.Domain.Entity;

public sealed class ScheduledTask
{
    public const string RestartTaskType = "system.restart";
    public const string MessageAutomationTaskType = "automation.send_message";
    public const string NoRepeatMode = "none";
    public const string WeeklyRepeatMode = "weekly";
    public const string QueuedStatus = "queued";
    public const string CompletedStatus = "completed";
    public const string CancelledStatus = "cancelled";
    public const string FailedStatus = "failed";

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
        string? lastMessage,
        string? appPath,
        string? appDisplayName,
        string? recipientNames,
        string? messageText,
        string repeatMode,
        int repeatDaysOfWeek)
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
        AppPath = NormalizeOptional(appPath);
        AppDisplayName = NormalizeOptional(appDisplayName);
        RecipientNames = NormalizeOptional(recipientNames);
        MessageText = NormalizeOptional(messageText);
        RepeatMode = NormalizeRepeatMode(repeatMode);
        RepeatDaysOfWeek = repeatDaysOfWeek;
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

    public string? AppPath { get; private set; }

    public string? AppDisplayName { get; private set; }

    public string? RecipientNames { get; private set; }

    public string? MessageText { get; private set; }

    public string RepeatMode { get; private set; } = NoRepeatMode;

    public int RepeatDaysOfWeek { get; private set; }

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
            message,
            appPath: null,
            appDisplayName: null,
            recipientNames: null,
            messageText: null,
            NoRepeatMode,
            repeatDaysOfWeek: 0);
    }

    public static ScheduledTask QueueMessageAutomation(
        string title,
        string appPath,
        string appDisplayName,
        IReadOnlyList<string> recipientNames,
        string messageText,
        DateTimeOffset scheduledForUtc,
        DateTimeOffset createdAtUtc,
        string repeatMode,
        int repeatDaysOfWeek)
    {
        if (recipientNames.Count == 0)
        {
            throw new ArgumentException("At least one recipient is required.", nameof(recipientNames));
        }

        return new ScheduledTask(
            Guid.NewGuid(),
            chatSessionId: null,
            commandLogId: null,
            MessageAutomationTaskType,
            title,
            priority: 50,
            scheduledForUtc,
            createdAtUtc,
            lastMessage: "Message automation scheduled.",
            NormalizeRequired(appPath, nameof(appPath)),
            NormalizeRequired(appDisplayName, nameof(appDisplayName)),
            string.Join('\n', recipientNames.Select(recipient => NormalizeRequired(recipient, nameof(recipientNames)))),
            NormalizeRequired(messageText, nameof(messageText)),
            repeatMode,
            repeatDaysOfWeek);
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

    public void MarkFailed(string message, DateTimeOffset failedAtUtc)
    {
        QueueStatus = FailedStatus;
        CompletedAtUtc = failedAtUtc.ToUniversalTime();
        LastMessage = string.IsNullOrWhiteSpace(message) ? "Task failed." : message.Trim();
    }

    public void Reschedule(DateTimeOffset scheduledForUtc, string message)
    {
        ScheduledForUtc = scheduledForUtc.ToUniversalTime();
        LastMessage = string.IsNullOrWhiteSpace(message) ? "Task rescheduled." : message.Trim();
    }

    public void UpdateMessageAutomation(
        string title,
        string appPath,
        string appDisplayName,
        IReadOnlyList<string> recipientNames,
        string messageText,
        DateTimeOffset scheduledForUtc,
        string repeatMode,
        int repeatDaysOfWeek,
        DateTimeOffset updatedAtUtc)
    {
        if (TaskType != MessageAutomationTaskType)
        {
            throw new InvalidOperationException("Only message automations can be updated with message automation data.");
        }

        if (recipientNames.Count == 0)
        {
            throw new ArgumentException("At least one recipient is required.", nameof(recipientNames));
        }

        DisplayName = NormalizeRequired(title, nameof(title));
        AppPath = NormalizeRequired(appPath, nameof(appPath));
        AppDisplayName = NormalizeRequired(appDisplayName, nameof(appDisplayName));
        RecipientNames = string.Join('\n', recipientNames.Select(recipient => NormalizeRequired(recipient, nameof(recipientNames))));
        MessageText = NormalizeRequired(messageText, nameof(messageText));
        ScheduledForUtc = scheduledForUtc.ToUniversalTime();
        RepeatMode = NormalizeRepeatMode(repeatMode);
        RepeatDaysOfWeek = repeatDaysOfWeek;
        QueueStatus = QueuedStatus;
        CompletedAtUtc = null;
        LastMessage = $"Message automation rescheduled for {ScheduledForUtc.ToLocalTime():MMM d, yyyy h:mm tt}.";
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string NormalizeRepeatMode(string? repeatMode)
    {
        if (string.IsNullOrWhiteSpace(repeatMode))
        {
            return NoRepeatMode;
        }

        var normalized = repeatMode.Trim().ToLowerInvariant();
        return normalized == WeeklyRepeatMode ? WeeklyRepeatMode : NoRepeatMode;
    }
}
