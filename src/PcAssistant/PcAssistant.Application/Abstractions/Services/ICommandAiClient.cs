using PcAssistant.Application.Models;

namespace PcAssistant.Application.Abstractions.Services;

public interface ICommandAiClient
{
    Task<CommandResponse> PredictAsync(string text, CancellationToken cancellationToken = default);
}
