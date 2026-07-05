namespace PcAssistant.Application.Abstractions.Services;

public interface IAiApiProcessService
{
    Task StartAsync(CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);
}
