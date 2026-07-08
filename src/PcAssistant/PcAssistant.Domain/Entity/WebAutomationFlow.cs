namespace PcAssistant.Domain.Entity;

public sealed class WebAutomationFlow
{
    private readonly List<WebAutomationStep> _steps = new();
    private readonly List<WebAutomationVariable> _variables = new();
    private readonly List<WebAutomationRun> _runs = new();

    private WebAutomationFlow()
    {
    }

    private WebAutomationFlow(
        Guid id,
        Guid projectId,
        string name,
        string startUrl,
        bool headless,
        string browserChannel,
        bool usePersistentSession,
        int defaultTimeoutMs,
        DateTime createdAtUtc)
    {
        Id = id;
        ProjectId = projectId;
        Name = NormalizeRequired(name, nameof(name));
        StartUrl = NormalizeRequired(startUrl, nameof(startUrl));
        Headless = headless;
        BrowserChannel = string.IsNullOrWhiteSpace(browserChannel) ? "msedge" : browserChannel.Trim();
        UsePersistentSession = usePersistentSession;
        DefaultTimeoutMs = Math.Clamp(defaultTimeoutMs, 1000, 120000);
        CreatedAtUtc = ToUtc(createdAtUtc);
    }

    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string StartUrl { get; private set; } = string.Empty;
    public bool Headless { get; private set; }
    public string BrowserChannel { get; private set; } = "msedge";
    public bool UsePersistentSession { get; private set; }
    public int DefaultTimeoutMs { get; private set; } = 30000;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public bool IsDeleted { get; private set; }
    public IReadOnlyCollection<WebAutomationStep> Steps => _steps;
    public IReadOnlyCollection<WebAutomationVariable> Variables => _variables;
    public IReadOnlyCollection<WebAutomationRun> Runs => _runs;

    public static WebAutomationFlow Create(
        Guid projectId,
        string name,
        string startUrl,
        bool headless,
        string browserChannel,
        bool usePersistentSession,
        int defaultTimeoutMs,
        DateTime createdAtUtc)
    {
        return new WebAutomationFlow(Guid.NewGuid(), projectId, name, startUrl, headless, browserChannel, usePersistentSession, defaultTimeoutMs, createdAtUtc);
    }

    public void Update(
        string name,
        string startUrl,
        bool headless,
        string browserChannel,
        bool usePersistentSession,
        int defaultTimeoutMs,
        DateTime updatedAtUtc)
    {
        Name = NormalizeRequired(name, nameof(name));
        StartUrl = NormalizeRequired(startUrl, nameof(startUrl));
        Headless = headless;
        BrowserChannel = string.IsNullOrWhiteSpace(browserChannel) ? "msedge" : browserChannel.Trim();
        UsePersistentSession = usePersistentSession;
        DefaultTimeoutMs = Math.Clamp(defaultTimeoutMs, 1000, 120000);
        UpdatedAtUtc = ToUtc(updatedAtUtc);
    }

    public void Archive(DateTime updatedAtUtc)
    {
        IsDeleted = true;
        UpdatedAtUtc = ToUtc(updatedAtUtc);
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return value.Trim();
    }

    private static DateTime ToUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
    }
}
