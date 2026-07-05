namespace PcAssistant.Domain.Enums;

public enum WebAutomationStepType
{
    GotoUrl = 0,
    Click = 1,
    Fill = 2,
    Type = 3,
    Press = 4,
    WaitForSelector = 5,
    Wait = 6,
    SelectOption = 7,
    Check = 8,
    Uncheck = 9,
    Hover = 10,
    Screenshot = 11,
    ExtractText = 12,
    AssertText = 13,
    AssertVisible = 14,
}

public enum WebSelectorType
{
    Css = 0,
    XPath = 1,
    Text = 2,
    Role = 3,
    Label = 4,
    Placeholder = 5,
    TestId = 6,
}

public enum WebAutomationRunStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4,
}

public enum WebAutomationStepRunStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Skipped = 4,
}

public enum BrowserTypeOption
{
    Chromium = 0,
    Edge = 1,
}
