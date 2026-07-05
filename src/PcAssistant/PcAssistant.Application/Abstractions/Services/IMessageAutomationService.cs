using PcAssistant.Application.Models;

namespace PcAssistant.Application.Abstractions.Services;

public interface IMessageAutomationService
{
    Task SendMessageToSpecificPersonAsync(SendMessageRequest request, CancellationToken cancellationToken = default);
}