namespace PcAssistant.Domain.Entity;

public sealed class ChatSession
{
    public const string DefaultTitle = "New chat";

    private readonly List<CommandLogEntry> _commandLogs = new();

    private ChatSession()
    {
    }

    private ChatSession(Guid id, string title, DateTimeOffset createdAtUtc)
    {
        Id = id;
        Title = NormalizeTitle(title);
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public string Title { get; private set; } = DefaultTitle;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<CommandLogEntry> CommandLogs => _commandLogs;

    public static ChatSession Create(string? title, DateTimeOffset createdAtUtc)
    {
        return new ChatSession(Guid.NewGuid(), title ?? DefaultTitle, createdAtUtc);
    }

    public void Touch(DateTimeOffset updatedAtUtc)
    {
        UpdatedAtUtc = updatedAtUtc;
    }

    public void RenameFromFirstMessage(string userText)
    {
        if (!Title.Equals(DefaultTitle, StringComparison.Ordinal))
        {
            return;
        }

        Title = BuildTitle(userText);
    }

    private static string BuildTitle(string userText)
    {
        var normalized = NormalizeTitle(userText);
        return normalized.Length <= 48 ? normalized : $"{normalized[..45]}...";
    }

    private static string NormalizeTitle(string title)
    {
        return string.IsNullOrWhiteSpace(title) ? DefaultTitle : title.Trim();
    }
}
