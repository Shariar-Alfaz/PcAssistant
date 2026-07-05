namespace PcAssistant.Domain.Entity;

public sealed class WebSelectorSnapshot
{
    private WebSelectorSnapshot()
    {
    }

    public Guid Id { get; private set; }
    public Guid StepId { get; private set; }
    public string? CssSelector { get; private set; }
    public string? XPath { get; private set; }
    public string? Role { get; private set; }
    public string? AccessibleName { get; private set; }
    public string? InnerText { get; private set; }
    public string? HtmlSnippet { get; private set; }
    public double? X { get; private set; }
    public double? Y { get; private set; }
    public double? Width { get; private set; }
    public double? Height { get; private set; }
    public string? ScreenshotPath { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
}
