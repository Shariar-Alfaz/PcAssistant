using System.Net.Http.Json;
using PcAssistant.Application.Abstractions;
using PcAssistant.Application.Models;

namespace PcAssistant.Infrastructure.Api;

public sealed class CommandAiClient(HttpClient httpClient) : ICommandAiClient
{
    public async Task<CommandResponse> PredictAsync(string text, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "predict",
            new CommandRequest(text),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Prediction service returned {(int)response.StatusCode}: {error}");
        }

        var commandResponse = await response.Content.ReadFromJsonAsync<CommandResponse>(cancellationToken);
        return commandResponse ?? throw new InvalidOperationException("Prediction service returned an empty response.");
    }
}
