namespace PcAssistant.Domain.Entity;

public sealed class WebAutomationProject
{
    private readonly List<WebAutomationFlow> _flows = new();

    private WebAutomationProject()
    {
    }

    private WebAutomationProject(Guid id, string name, string? description, string startUrl, DateTime createdAtUtc)
    {
        Id = id;
        Name = NormalizeRequired(name, nameof(name));
        Description = NormalizeOptional(description);
        StartUrl = NormalizeRequired(startUrl, nameof(startUrl));
        CreatedAtUtc = ToUtc(createdAtUtc);
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string StartUrl { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public bool IsDeleted { get; private set; }
    public IReadOnlyCollection<WebAutomationFlow> Flows => _flows;

    public static WebAutomationProject Create(string name, string? description, string startUrl, DateTime createdAtUtc)
    {
        return new WebAutomationProject(Guid.NewGuid(), name, description, startUrl, createdAtUtc);
    }

    public void Update(string name, string? description, string startUrl, DateTime updatedAtUtc)
    {
        Name = NormalizeRequired(name, nameof(name));
        Description = NormalizeOptional(description);
        StartUrl = NormalizeRequired(startUrl, nameof(startUrl));
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

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static DateTime ToUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
    }
}
