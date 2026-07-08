namespace PcAssistant.Domain.Entity;

public sealed class WebAutomationVariable
{
    private WebAutomationVariable()
    {
    }

    public Guid Id { get; private set; }
    public Guid FlowId { get; private set; }
    public string Key { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;
    public bool IsSecret { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
}
