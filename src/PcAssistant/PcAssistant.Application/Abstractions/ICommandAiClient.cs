using PcAssistant.Application.Models;

namespace PcAssistant.Application.Abstractions;

public interface ICommandAiClient
{
    Task<CommandResponse> PredictAsync(string text, CancellationToken cancellationToken = default);
}
