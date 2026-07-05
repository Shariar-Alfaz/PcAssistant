using Microsoft.Playwright;
using PcAssistant.Application.Abstractions.Services;
using PcAssistant.Application.Models;

namespace PcAssistant.Infrastructure.WebAutomation;

internal sealed class PlaywrightWebPreviewService : IWebPreviewService
{
    public Task<WebPreviewDto> CreatePreviewAsync(string url, CancellationToken cancellationToken = default)
    {
        if (!IsValidHttpUrl(url))
        {
            return Task.FromResult(new WebPreviewDto(url, false, null, null, "Enter an absolute http:// or https:// URL."));
        }

        return Task.FromResult(new WebPreviewDto(url, true, url, null, "Inline preview is available when the website allows embedding."));
    }

    public async Task<WebPreviewDto> CaptureScreenshotPreviewAsync(string url, CancellationToken cancellationToken = default)
    {
        if (!IsValidHttpUrl(url))
        {
            return new WebPreviewDto(url, false, null, null, "Enter an absolute http:// or https:// URL.");
        }

        var screenshotPath = BuildScreenshotPath("preview");
        Directory.CreateDirectory(Path.GetDirectoryName(screenshotPath)!);

        try
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Channel = "msedge",
                Headless = true,
            });

            var page = await browser.NewPageAsync();
            await page.GotoAsync(url, new PageGotoOptions { Timeout = 30000, WaitUntil = WaitUntilState.DOMContentLoaded });
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath, FullPage = true });
            return new WebPreviewDto(url, false, null, screenshotPath, "Screenshot preview captured with Playwright.");
        }
        catch (Exception ex)
        {
            return new WebPreviewDto(url, false, null, null, $"Preview failed: {ex.Message}");
        }
    }

    public async Task OpenHeadedPreviewAsync(string url, CancellationToken cancellationToken = default)
    {
        if (!IsValidHttpUrl(url))
        {
            throw new InvalidOperationException("Enter an absolute http:// or https:// URL.");
        }

        using var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Channel = "msedge",
            Headless = false,
        });

        var page = await browser.NewPageAsync();
        await page.GotoAsync(url, new PageGotoOptions { Timeout = 30000, WaitUntil = WaitUntilState.DOMContentLoaded });
    }

    private static bool IsValidHttpUrl(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https";
    }

    private static string BuildScreenshotPath(string prefix)
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PcAssistant",
            "WebAutomationScreenshots");
        return Path.Combine(root, $"{prefix}-{DateTime.UtcNow:yyyyMMdd-HHmmssfff}.png");
    }
}
