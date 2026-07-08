namespace PcAssistant.Domain.Entity;

public sealed class BrowserProfile
{
    private BrowserProfile()
    {
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string UserDataDirectory { get; private set; } = string.Empty;
    public bool IsDefault { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
}
