using Microsoft.Playwright;
using PcAssistant.Application.Abstractions.Services;
using PcAssistant.Application.Models;

namespace PcAssistant.Infrastructure.WebAutomation;

internal sealed class PlaywrightWebPreviewService : IWebPreviewService
{
    private const int PreviewTimeoutMs = 30000;

    public async Task<WebPreviewDto> CreatePreviewAsync(string url, CancellationToken cancellationToken = default)
    {
        if (!IsValidHttpUrl(url))
        {
            return new WebPreviewDto(url, false, null, null, null, "Enter an absolute http:// or https:// URL.");
        }

        try
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Channel = "msedge",
                Headless = true,
            });

            var page = await browser.NewPageAsync();
            await NavigateForPreviewAsync(page, url);
            if (await LooksLikeLoadingSkeletonAsync(page))
            {
                var screenshotPath = BuildScreenshotPath("preview");
                Directory.CreateDirectory(Path.GetDirectoryName(screenshotPath)!);
                await page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath, FullPage = true });
                return new WebPreviewDto(url, false, null, null, screenshotPath, "Inline preview is still loading, so a screenshot preview was captured.");
            }

            var previewHtml = await BuildPreviewSnapshotAsync(page, url);
            return new WebPreviewDto(url, true, null, previewHtml, null, "Inline preview captured for targeting.");
        }
        catch (Exception ex)
        {
            return new WebPreviewDto(url, false, null, null, null, $"Inline preview failed: {ex.Message}");
        }
    }

    public async Task<WebPreviewDto> CaptureScreenshotPreviewAsync(string url, CancellationToken cancellationToken = default)
    {
        if (!IsValidHttpUrl(url))
        {
            return new WebPreviewDto(url, false, null, null, null, "Enter an absolute http:// or https:// URL.");
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
            await NavigateForPreviewAsync(page, url);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath, FullPage = true });
            return new WebPreviewDto(url, false, null, null, screenshotPath, "Screenshot preview captured with Playwright.");
        }
        catch (Exception ex)
        {
            return new WebPreviewDto(url, false, null, null, null, $"Preview failed: {ex.Message}");
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
        await NavigateForPreviewAsync(page, url);
    }

    private static bool IsValidHttpUrl(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https";
    }

    private static async Task NavigateForPreviewAsync(IPage page, string url)
    {
        await page.GotoAsync(url, new PageGotoOptions { Timeout = PreviewTimeoutMs, WaitUntil = WaitUntilState.DOMContentLoaded });
        await WaitForLoadStateBestEffortAsync(page, LoadState.Load, 10000);
        await WaitForLoadStateBestEffortAsync(page, LoadState.NetworkIdle, 5000);
        await WaitForKnownDynamicContentAsync(page);
        await page.WaitForTimeoutAsync(1200);
    }

    private static async Task WaitForLoadStateBestEffortAsync(IPage page, LoadState loadState, float timeoutMs)
    {
        try
        {
            await page.WaitForLoadStateAsync(loadState, new PageWaitForLoadStateOptions { Timeout = timeoutMs });
        }
        catch
        {
            // Some SPA pages keep long-lived requests open; use the best DOM we have.
        }
    }

    private static async Task WaitForKnownDynamicContentAsync(IPage page)
    {
        var host = new Uri(page.Url).Host;
        if (host.Contains("youtube.com", StringComparison.OrdinalIgnoreCase))
        {
            await WaitForSelectorBestEffortAsync(
                page,
                "ytd-rich-item-renderer, ytd-video-renderer, ytd-watch-flexy, a#thumbnail[href], video",
                12000);
            return;
        }

        await WaitForSelectorBestEffortAsync(
            page,
            "main, article, [role='main'], a[href], button, input, textarea, select, img, video",
            5000);
    }

    private static async Task WaitForSelectorBestEffortAsync(IPage page, string selector, float timeoutMs)
    {
        try
        {
            await page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions { Timeout = timeoutMs, State = WaitForSelectorState.Attached });
        }
        catch
        {
            // Preview should still return whatever rendered successfully.
        }
    }

    private static async Task<bool> LooksLikeLoadingSkeletonAsync(IPage page)
    {
        try
        {
            return await page.EvaluateAsync<bool>(
                @"() => {
                    const visibleText = (document.body?.innerText || '').replace(/\s+/g, ' ').trim();
                    const interactiveCount = document.querySelectorAll('a[href], button, input, textarea, select, video').length;
                    const mediaCount = document.querySelectorAll('img[src], video, ytd-thumbnail img[src], yt-image img[src]').length;
                    const skeletonCount = document.querySelectorAll(
                        [
                            '[class*=""skeleton""]',
                            '[class*=""placeholder""]',
                            '[class*=""shimmer""]',
                            'yt-page-navigation-progress',
                            'ytd-rich-grid-skeleton',
                            'ytd-ghost-grid-renderer',
                            'tp-yt-paper-spinner'
                        ].join(',')
                    ).length;

                    return visibleText.length < 80 && interactiveCount < 3 && mediaCount < 2 && skeletonCount > 0;
                }");
        }
        catch
        {
            return false;
        }
    }

    private static async Task<string> BuildPreviewSnapshotAsync(IPage page, string url)
    {
        await page.EvaluateAsync(
            @"previewUrl => {
                document.querySelectorAll('script, noscript').forEach((element) => element.remove());
                let head = document.head;
                if (!head) {
                    head = document.createElement('head');
                    document.documentElement.prepend(head);
                }

                let base = head.querySelector('base[data-pc-assistant-preview-base]');
                if (!base) {
                    base = document.createElement('base');
                    base.setAttribute('data-pc-assistant-preview-base', 'true');
                    head.prepend(base);
                }

                base.href = previewUrl;
            }",
            url);

        return await page.ContentAsync();
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
