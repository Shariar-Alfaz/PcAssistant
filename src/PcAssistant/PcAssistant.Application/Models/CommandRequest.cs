using System.Text.Json.Serialization;

namespace PcAssistant.Application.Models;

public sealed class CommandRequest
{
    public CommandRequest(string text)
    {
        Text = text;
    }

    [JsonPropertyName("text")]
    public string Text { get; }
}
