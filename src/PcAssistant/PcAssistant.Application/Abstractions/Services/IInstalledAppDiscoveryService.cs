using PcAssistant.Application.Models;

namespace PcAssistant.Application.Abstractions.Services;

public interface IInstalledAppDiscoveryService
{
    Task<IReadOnlyList<InstalledAppOption>> ListInstalledAppsAsync(CancellationToken cancellationToken = default);

    Task<string?> GetAppIconDataUrlAsync(
        string appPath,
        string? iconSourcePath = null,
        CancellationToken cancellationToken = default);
}
