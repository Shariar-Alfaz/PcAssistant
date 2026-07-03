namespace PcAssistant.Components.Pages.Models;

public readonly struct AssistantDirectoryTrigger
{
    public AssistantDirectoryTrigger(int index, string token, string query)
    {
        Index = index;
        Token = token;
        Query = query;
    }

    public int Index { get; }

    public string Token { get; }

    public string Query { get; }
}
