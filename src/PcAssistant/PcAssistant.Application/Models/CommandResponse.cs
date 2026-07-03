using System.Text.Json.Serialization;

namespace PcAssistant.Application.Models;

public sealed class CommandResponse
{
    [JsonPropertyName("input")]
    public string Input { get; set; } = string.Empty;

    [JsonPropertyName("commandLabel")]
    public string CommandLabel { get; set; } = "unknown";

    [JsonPropertyName("rawPredictedLabel")]
    public string RawPredictedLabel { get; set; } = "unknown";

    [JsonPropertyName("source")]
    public string Source { get; set; } = "model";

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    [JsonPropertyName("requiresConfirmation")]
    public bool RequiresConfirmation { get; set; }

    [JsonPropertyName("probabilities")]
    public Dictionary<string, double> Probabilities { get; set; } = new();
}
