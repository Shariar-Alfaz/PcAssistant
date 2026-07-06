using Microsoft.Playwright;
using PcAssistant.Application.Abstractions.Services;
using PcAssistant.Application.Models;
using PcAssistant.Domain.Enums;

namespace PcAssistant.Infrastructure.WebAutomation;

internal sealed class PlaywrightWebAutomationService : IWebAutomationService
{
    public async Task<WebAutomationRunDto> RunFlowAsync(
        WebAutomationFlowDto flow,
        bool runHeaded,
        CancellationToken cancellationToken = default)
    {
        var runId = Guid.NewGuid();
        var started = DateTime.UtcNow;
        var logs = new List<WebAutomationRunStepLogDto>();
        var status = WebAutomationRunStatus.Completed;
        string? errorMessage = null;

        await using var session = await BrowserSession.OpenAsync(flow, runHeaded, cancellationToken);
        try
        {
            await session.Page.GotoAsync(flow.StartUrl, new PageGotoOptions { Timeout = flow.DefaultTimeoutMs, WaitUntil = WaitUntilState.DOMContentLoaded });
            foreach (var step in flow.Steps.OrderBy(step => step.OrderIndex))
            {
                var log = await ExecuteStepWithRetryAsync(session.Page, runId, step, cancellationToken);
                logs.Add(log);

                if (log.Status == WebAutomationStepRunStatus.Failed && !step.IsOptional)
                {
                    status = WebAutomationRunStatus.Failed;
                    errorMessage = log.ErrorMessage;
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            status = WebAutomationRunStatus.Cancelled;
            errorMessage = "Run was cancelled.";
        }
        catch (Exception ex)
        {
            status = WebAutomationRunStatus.Failed;
            errorMessage = ex.Message;
        }

        return new WebAutomationRunDto(
            runId,
            flow.Id,
            status,
            started,
            DateTime.UtcNow,
            errorMessage,
            flow.BrowserChannel,
            runHeaded ? false : flow.Headless,
            logs);
    }

    public async Task<WebAutomationRunStepLogDto> RunSingleStepAsync(
        WebAutomationFlowDto flow,
        WebAutomationStepDto step,
        bool runHeaded,
        CancellationToken cancellationToken = default)
    {
        var runId = Guid.NewGuid();
        await using var session = await BrowserSession.OpenAsync(flow, runHeaded, cancellationToken);
        await session.Page.GotoAsync(flow.StartUrl, new PageGotoOptions { Timeout = flow.DefaultTimeoutMs, WaitUntil = WaitUntilState.DOMContentLoaded });
        return await ExecuteStepWithRetryAsync(session.Page, runId, step, cancellationToken);
    }

    private static async Task<WebAutomationRunStepLogDto> ExecuteStepWithRetryAsync(
        IPage page,
        Guid runId,
        WebAutomationStepDto step,
        CancellationToken cancellationToken)
    {
        WebAutomationRunStepLogDto? lastLog = null;
        for (var attempt = 0; attempt <= step.RetryCount; attempt++)
        {
            lastLog = await ExecuteStepAsync(page, runId, step, cancellationToken);
            if (lastLog.Status == WebAutomationStepRunStatus.Completed || step.IsOptional)
            {
                return lastLog;
            }
        }

        return lastLog!;
    }

    private static async Task<WebAutomationRunStepLogDto> ExecuteStepAsync(
        IPage page,
        Guid runId,
        WebAutomationStepDto step,
        CancellationToken cancellationToken)
    {
        var started = DateTime.UtcNow;
        string? screenshotPath = null;
        string? extractedValue = null;
        string? message = null;
        string? errorMessage = null;
        var status = WebAutomationStepRunStatus.Completed;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var locator = RequiresLocator(step.StepType) ? BuildLocator(page, step) : null;

            switch (step.StepType)
            {
                case WebAutomationStepType.GotoUrl:
                    await page.GotoAsync(step.Url ?? throw new InvalidOperationException("Step URL is required."), new PageGotoOptions { Timeout = step.TimeoutMs, WaitUntil = WaitUntilState.DOMContentLoaded });
                    message = $"Opened {step.Url}.";
                    break;
                case WebAutomationStepType.Click:
                    await locator!.ClickAsync(new LocatorClickOptions { Timeout = step.TimeoutMs });
                    message = "Clicked element.";
                    break;
                case WebAutomationStepType.Fill:
                    await locator!.FillAsync(step.Value ?? string.Empty, new LocatorFillOptions { Timeout = step.TimeoutMs });
                    message = "Filled input.";
                    break;
                case WebAutomationStepType.Type:
                    await locator!.PressSequentiallyAsync(step.Value ?? string.Empty, new LocatorPressSequentiallyOptions { Timeout = step.TimeoutMs });
                    message = "Typed text.";
                    break;
                case WebAutomationStepType.Press:
                    await page.Keyboard.PressAsync(step.Value ?? "Enter");
                    message = $"Pressed {step.Value}.";
                    break;
                case WebAutomationStepType.WaitForSelector:
                    await locator!.WaitForAsync(new LocatorWaitForOptions { Timeout = step.TimeoutMs, State = WaitForSelectorState.Visible });
                    message = "Element is visible.";
                    break;
                case WebAutomationStepType.Wait:
                    await page.WaitForTimeoutAsync(Math.Max(1, step.DelayAfterMs));
                    message = "Wait completed.";
                    break;
                case WebAutomationStepType.SelectOption:
                    await locator!.SelectOptionAsync(step.Value ?? string.Empty, new LocatorSelectOptionOptions { Timeout = step.TimeoutMs });
                    message = "Selected option.";
                    break;
                case WebAutomationStepType.UploadFile:
                    var filePaths = NormalizeUploadPaths(step.Value);
                    await locator!.SetInputFilesAsync(filePaths, new LocatorSetInputFilesOptions { Timeout = step.TimeoutMs });
                    message = filePaths.Length == 1
                        ? $"Uploaded {Path.GetFileName(filePaths[0])}."
                        : $"Uploaded {filePaths.Length} files.";
                    break;
                case WebAutomationStepType.Check:
                    await locator!.CheckAsync(new LocatorCheckOptions { Timeout = step.TimeoutMs });
                    message = "Checked element.";
                    break;
                case WebAutomationStepType.Uncheck:
                    await locator!.UncheckAsync(new LocatorUncheckOptions { Timeout = step.TimeoutMs });
                    message = "Unchecked element.";
                    break;
                case WebAutomationStepType.Hover:
                    await locator!.HoverAsync(new LocatorHoverOptions { Timeout = step.TimeoutMs });
                    message = "Hovered element.";
                    break;
                case WebAutomationStepType.Screenshot:
                    screenshotPath = await CaptureScreenshotAsync(page, step.Id, cancellationToken);
                    message = "Screenshot captured.";
                    break;
                case WebAutomationStepType.ExtractText:
                    extractedValue = await locator!.InnerTextAsync(new LocatorInnerTextOptions { Timeout = step.TimeoutMs });
                    message = "Text extracted.";
                    break;
                case WebAutomationStepType.AssertText:
                    var text = await locator!.InnerTextAsync(new LocatorInnerTextOptions { Timeout = step.TimeoutMs });
                    if (!text.Contains(step.Value ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Expected text was not found.");
                    }

                    message = "Text assertion passed.";
                    break;
                case WebAutomationStepType.AssertVisible:
                    await locator!.WaitForAsync(new LocatorWaitForOptions { Timeout = step.TimeoutMs, State = WaitForSelectorState.Visible });
                    if (!await locator.IsVisibleAsync())
                    {
                        throw new InvalidOperationException("Element is not visible.");
                    }

                    message = "Visibility assertion passed.";
                    break;
            }

            if (step.DelayAfterMs > 0 && step.StepType != WebAutomationStepType.Wait)
            {
                await page.WaitForTimeoutAsync(step.DelayAfterMs);
            }

            if (step.TakeScreenshotAfterStep && screenshotPath is null)
            {
                screenshotPath = await CaptureScreenshotAsync(page, step.Id, cancellationToken);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            status = step.IsOptional ? WebAutomationStepRunStatus.Skipped : WebAutomationStepRunStatus.Failed;
            errorMessage = ex.Message;
            try
            {
                screenshotPath = await CaptureScreenshotAsync(page, step.Id, cancellationToken);
            }
            catch
            {
                screenshotPath = null;
            }
        }

        return new WebAutomationRunStepLogDto(
            Guid.NewGuid(),
            runId,
            step.Id,
            step.OrderIndex,
            status,
            started,
            DateTime.UtcNow,
            message,
            errorMessage,
            screenshotPath,
            extractedValue);
    }

    private static ILocator BuildLocator(IPage page, WebAutomationStepDto step)
    {
        var selector = step.Selector ?? string.Empty;
        var value = step.Value ?? selector;
        var locator = step.SelectorType switch
        {
            WebSelectorType.XPath => page.Locator($"xpath={selector}"),
            WebSelectorType.Text => page.GetByText(value),
            WebSelectorType.Role => page.GetByRole(ParseRole(selector), new PageGetByRoleOptions { Name = value }),
            WebSelectorType.Label => page.GetByLabel(value),
            WebSelectorType.Placeholder => page.GetByPlaceholder(value),
            WebSelectorType.TestId => page.GetByTestId(value),
            _ => page.Locator(selector),
        };

        return locator.Nth(0);
    }

    private static AriaRole ParseRole(string? role)
    {
        return Enum.TryParse<AriaRole>(role, ignoreCase: true, out var parsed) ? parsed : AriaRole.Button;
    }

    private static string[] NormalizeUploadPaths(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException("Upload file path is required.");
        }

        var paths = value
            .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(path => Path.GetFullPath(Environment.ExpandEnvironmentVariables(path)))
            .ToArray();

        if (paths.Length == 0)
        {
            throw new InvalidOperationException("Upload file path is required.");
        }

        foreach (var path in paths)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Upload file was not found.", path);
            }
        }

        return paths;
    }

    private static bool RequiresLocator(WebAutomationStepType stepType)
    {
        return stepType is not WebAutomationStepType.GotoUrl and not WebAutomationStepType.Wait and not WebAutomationStepType.Screenshot and not WebAutomationStepType.Press;
    }

    private static async Task<string> CaptureScreenshotAsync(IPage page, Guid stepId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PcAssistant",
            "WebAutomationScreenshots");
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, $"{DateTime.UtcNow:yyyyMMdd-HHmmssfff}-{stepId:N}.png");
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = path, FullPage = true });
        return path;
    }

    private sealed class BrowserSession : IAsyncDisposable
    {
        private BrowserSession(IPlaywright playwright, Microsoft.Playwright.IBrowser? browser, IBrowserContext context, IPage page)
        {
            Playwright = playwright;
            Browser = browser;
            Context = context;
            Page = page;
        }

        private IPlaywright Playwright { get; }
        private Microsoft.Playwright.IBrowser? Browser { get; }
        private IBrowserContext Context { get; }
        public IPage Page { get; }

        public static async Task<BrowserSession> OpenAsync(WebAutomationFlowDto flow, bool runHeaded, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            var headless = runHeaded ? false : flow.Headless;
            var channel = string.IsNullOrWhiteSpace(flow.BrowserChannel) ? "msedge" : flow.BrowserChannel;

            if (flow.UsePersistentSession)
            {
                var userDataDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "PcAssistant",
                    "WebAutomationProfiles",
                    flow.Id.ToString("N"));
                Directory.CreateDirectory(userDataDir);
                var context = await playwright.Chromium.LaunchPersistentContextAsync(
                    userDataDir,
                    new BrowserTypeLaunchPersistentContextOptions
                    {
                        Channel = channel,
                        Headless = headless,
                    });
                var page = context.Pages.FirstOrDefault() ?? await context.NewPageAsync();
                return new BrowserSession(playwright, null, context, page);
            }

            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Channel = channel,
                Headless = headless,
            });
            var browserContext = await browser.NewContextAsync();
            var newPage = await browserContext.NewPageAsync();
            return new BrowserSession(playwright, browser, browserContext, newPage);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.CloseAsync();
            if (Browser is not null)
            {
                await Browser.CloseAsync();
            }

            Playwright.Dispose();
        }
    }
}
