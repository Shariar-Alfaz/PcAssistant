using PcAssistant.Application.Models;

namespace PcAssistant.Application.Abstractions.Services;

public interface IWebPreviewService
{
    Task<WebPreviewDto> CreatePreviewAsync(string url, CancellationToken cancellationToken = default);

    Task<WebPreviewDto> CaptureScreenshotPreviewAsync(string url, CancellationToken cancellationToken = default);

    Task OpenHeadedPreviewAsync(string url, CancellationToken cancellationToken = default);
}
