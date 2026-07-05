using PcAssistant.Application.Abstractions.Services;
using PcAssistant.Application.Models;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

#if WINDOWS
using System.Xml.Linq;
using Windows.Management.Deployment;
#endif

namespace PcAssistant.Infrastructure.Platform
{
    public class MessageAutomationService : IMessageAutomationService
    {
#if WINDOWS
        private const int SW_RESTORE = 9;

        private const uint INPUT_KEYBOARD = 1;
        private const uint INPUT_MOUSE = 0;

        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_UNICODE = 0x0004;

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;

        private const uint CF_UNICODETEXT = 13;
        private const uint GMEM_MOVEABLE = 0x0002;

        private const uint WM_CLOSE = 0x0010;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll")]
        private static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll")]
        private static extern bool EmptyClipboard();

        [DllImport("user32.dll")]
        private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

        [DllImport("user32.dll")]
        private static extern bool CloseClipboard();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalUnlock(IntPtr hMem);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
#endif

        public async Task SendMessageToSpecificPersonAsync(
            SendMessageRequest request,
            CancellationToken cancellationToken = default)
        {
#if WINDOWS
            ValidateRequest(request);

            var target = ResolveAutomationTarget(request);
            var profile = ResolveChatAppProfile(request, target);

            var wasVisibleBeforeAutomation = HasVisibleAppWindow(target);
            var shouldCloseAfterSend = !wasVisibleBeforeAutomation;

            Process? process = await GetOrStartProcessAsync(target, cancellationToken);

            IntPtr windowHandle = await GetMainWindowHandleAsync(
                target,
                process,
                timeoutMs: 30000,
                cancellationToken);

            if (windowHandle == IntPtr.Zero)
            {
                process = await ActivateProcessAsync(target, cancellationToken);

                windowHandle = await GetMainWindowHandleAsync(
                    target,
                    process,
                    timeoutMs: 15000,
                    cancellationToken);
            }

            if (windowHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to open or focus the application window.");
            }

            FocusWindow(windowHandle);

            await Task.Delay(1000, cancellationToken);

            foreach (string recipientName in request.RecipientNames)
            {
                await SendMessageToRecipientAsync(
                    windowHandle,
                    recipientName,
                    request.Message,
                    profile,
                    cancellationToken);

                await Task.Delay(1000, cancellationToken);
            }

            if (shouldCloseAfterSend)
            {
                CloseWindow(windowHandle);
            }
#else
            await Task.CompletedTask;
            throw new PlatformNotSupportedException("Message automation is only supported on Windows.");
#endif
        }

#if WINDOWS
        private static async Task<Process?> GetOrStartProcessAsync(
            AutomationTarget target,
            CancellationToken cancellationToken)
        {
            if (!target.IsShellLaunch)
            {
                Process? existingProcess = GetRunningProcessWithWindow(target.ProcessName);

                if (existingProcess != null)
                {
                    return existingProcess;
                }
            }

            Process? runningProcessWithoutWindow = null;

            if (!target.IsShellLaunch && !string.IsNullOrWhiteSpace(target.ProcessName))
            {
                runningProcessWithoutWindow = GetAnyRunningProcess(target.ProcessName);
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = target.LaunchFileName,
                Arguments = target.LaunchArguments,
                UseShellExecute = true
            };

            Process? startedProcess = null;

            try
            {
                startedProcess = Process.Start(startInfo);
            }
            catch
            {
                if (!target.IsShellLaunch)
                {
                    throw;
                }
            }

            if (!target.IsShellLaunch && startedProcess == null && runningProcessWithoutWindow == null)
            {
                throw new InvalidOperationException("Failed to start the application.");
            }

            await Task.Delay(target.IsShellLaunch ? 5000 : 3000, cancellationToken);

            return target.IsShellLaunch
                ? null
                : startedProcess ?? runningProcessWithoutWindow;
        }

        private static async Task<Process?> ActivateProcessAsync(
            AutomationTarget target,
            CancellationToken cancellationToken)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = target.LaunchFileName,
                Arguments = target.LaunchArguments,
                UseShellExecute = true
            };

            Process? startedProcess = null;

            try
            {
                startedProcess = Process.Start(startInfo);
            }
            catch
            {
                if (!target.IsShellLaunch)
                {
                    throw;
                }
            }

            await Task.Delay(target.IsShellLaunch ? 5000 : 3000, cancellationToken);

            if (target.IsShellLaunch)
            {
                return null;
            }

            return startedProcess
                ?? GetRunningProcessWithWindow(target.ProcessName)
                ?? GetAnyRunningProcess(target.ProcessName)
                ?? throw new InvalidOperationException("Failed to activate the application.");
        }

        private static AutomationTarget ResolveAutomationTarget(SendMessageRequest request)
        {
            var normalizedPath = request.AppPath.Trim();

            var shortcutTarget = TryResolveShortcutTarget(normalizedPath);
            if (shortcutTarget is not null)
            {
                normalizedPath = shortcutTarget;
            }

            if (TryNormalizeShellAppsFolderPath(normalizedPath, out var shellLaunchPath))
            {
                var appUserModelId = shellLaunchPath["shell:AppsFolder\\".Length..];

                var processName = GuessPackagedProcessName(
                    appUserModelId,
                    request.AppDisplayName);

                return new AutomationTarget(
                    LaunchFileName: "explorer.exe",
                    LaunchArguments: $"\"{shellLaunchPath}\"",
                    ProcessName: processName,
                    WindowTitleKeywords: BuildWindowKeywords(
                        normalizedPath,
                        processName,
                        request.AppDisplayName),
                    IsShellLaunch: true);
            }

            var processNameFromFile = SafeGetFileNameWithoutExtension(normalizedPath);

            if (string.IsNullOrWhiteSpace(processNameFromFile))
            {
                processNameFromFile = SafeGetFileNameWithoutExtension(request.AppPath);
            }

            var packagedLaunchId = TryResolvePackagedLaunchId(normalizedPath);
            if (!string.IsNullOrWhiteSpace(packagedLaunchId))
            {
                return new AutomationTarget(
                    LaunchFileName: "explorer.exe",
                    LaunchArguments: $"\"{packagedLaunchId}\"",
                    ProcessName: processNameFromFile,
                    WindowTitleKeywords: BuildWindowKeywords(
                        request.AppPath,
                        processNameFromFile,
                        request.AppDisplayName),
                    IsShellLaunch: true);
            }

            return new AutomationTarget(
                LaunchFileName: normalizedPath,
                LaunchArguments: string.Empty,
                ProcessName: processNameFromFile,
                WindowTitleKeywords: BuildWindowKeywords(
                    request.AppPath,
                    processNameFromFile,
                    request.AppDisplayName),
                IsShellLaunch: false);
        }

        private static bool TryNormalizeShellAppsFolderPath(
            string appPath,
            out string shellLaunchPath)
        {
            shellLaunchPath = string.Empty;

            if (string.IsNullOrWhiteSpace(appPath))
            {
                return false;
            }

            appPath = appPath.Trim().Trim('"');

            if (appPath.StartsWith("shell:AppsFolder\\", StringComparison.OrdinalIgnoreCase))
            {
                shellLaunchPath = appPath;
                return true;
            }

            if (appPath.Contains("!", StringComparison.OrdinalIgnoreCase)
                && !appPath.Contains("\\", StringComparison.OrdinalIgnoreCase)
                && !appPath.Contains("/", StringComparison.OrdinalIgnoreCase))
            {
                shellLaunchPath = $"shell:AppsFolder\\{appPath}";
                return true;
            }

            return false;
        }

        private static string GuessPackagedProcessName(
            string appUserModelId,
            string? appDisplayName)
        {
            var identity = $"{appUserModelId} {appDisplayName}";

            if (identity.Contains("whatsapp", StringComparison.OrdinalIgnoreCase))
            {
                return "WhatsApp";
            }

            if (identity.Contains("teams", StringComparison.OrdinalIgnoreCase))
            {
                return "ms-teams";
            }

            if (identity.Contains("telegram", StringComparison.OrdinalIgnoreCase))
            {
                return "Telegram";
            }

            if (identity.Contains("messenger", StringComparison.OrdinalIgnoreCase))
            {
                return "Messenger";
            }

            if (!string.IsNullOrWhiteSpace(appDisplayName))
            {
                return appDisplayName
                    .Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase)
                    .Trim();
            }

            var packageName = appUserModelId.Split('!')[0];
            var underscoreIndex = packageName.IndexOf('_');

            if (underscoreIndex > 0)
            {
                packageName = packageName[..underscoreIndex];
            }

            return packageName.Split('.').LastOrDefault() ?? packageName;
        }

        private static string? TryResolveShortcutTarget(string appPath)
        {
            if (!appPath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase)
                || !File.Exists(appPath))
            {
                return null;
            }

            try
            {
                var shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType is null)
                {
                    return null;
                }

                dynamic shell = Activator.CreateInstance(shellType)!;
                dynamic shortcut = shell.CreateShortcut(appPath);

                string targetPath = shortcut.TargetPath;

                return !string.IsNullOrWhiteSpace(targetPath)
                       && File.Exists(targetPath)
                    ? targetPath.Trim()
                    : null;
            }
            catch
            {
                return null;
            }
        }

        private static string? TryResolvePackagedLaunchId(string appPath)
        {
            if (string.IsNullOrWhiteSpace(appPath)
                || !appPath.Contains(@"\WindowsApps\", StringComparison.OrdinalIgnoreCase)
                || !File.Exists(appPath))
            {
                return null;
            }

            var installPath = Path.GetDirectoryName(appPath);
            if (string.IsNullOrWhiteSpace(installPath))
            {
                return null;
            }

            var packageFamilyName = FindPackageFamilyName(installPath);
            var appId = FindPackagedAppId(installPath, Path.GetFileName(appPath));

            return string.IsNullOrWhiteSpace(packageFamilyName)
                   || string.IsNullOrWhiteSpace(appId)
                ? null
                : $"shell:AppsFolder\\{packageFamilyName}!{appId}";
        }

        private static string? FindPackageFamilyName(string installPath)
        {
            try
            {
                var packageManager = new PackageManager();

                foreach (var package in packageManager.FindPackagesForUser(string.Empty))
                {
                    string? packagePath;

                    try
                    {
                        packagePath = package.InstalledLocation?.Path;
                    }
                    catch
                    {
                        continue;
                    }

                    if (packagePath?.Equals(installPath, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        return package.Id.FamilyName;
                    }
                }
            }
            catch
            {
                return null;
            }

            return null;
        }

        private static string? FindPackagedAppId(
            string installPath,
            string executableName)
        {
            var manifestPath = Path.Combine(installPath, "AppxManifest.xml");
            if (!File.Exists(manifestPath))
            {
                return null;
            }

            try
            {
                var manifest = XDocument.Load(manifestPath);

                return manifest
                    .Descendants()
                    .Where(element => element.Name.LocalName == "Application")
                    .Where(element =>
                    {
                        var executable = element.Attribute("Executable")?.Value;

                        if (string.IsNullOrWhiteSpace(executable))
                        {
                            return false;
                        }

                        var manifestExecutableName = Path.GetFileName(executable);

                        return executableName.Equals(
                            manifestExecutableName,
                            StringComparison.OrdinalIgnoreCase);
                    })
                    .OrderBy(element => IsBackgroundPackagedApp(element.Attribute("Id")?.Value) ? 1 : 0)
                    .Select(element => element.Attribute("Id")?.Value)
                    .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
            }
            catch
            {
                return null;
            }
        }

        private static bool IsBackgroundPackagedApp(string? value)
        {
            return !string.IsNullOrWhiteSpace(value)
                   && (value.Contains("update", StringComparison.OrdinalIgnoreCase)
                       || value.Contains("autostart", StringComparison.OrdinalIgnoreCase)
                       || value.Contains("remote", StringComparison.OrdinalIgnoreCase)
                       || value.Contains("background", StringComparison.OrdinalIgnoreCase));
        }

        private static ChatAppProfile ResolveChatAppProfile(
            SendMessageRequest request,
            AutomationTarget target)
        {
            if (request.SearchShortCutKey != VirtualKeys.K)
            {
                return new ChatAppProfile(request.SearchShortCutKey);
            }

            var identity = $"{request.AppPath} {request.AppDisplayName} {target.ProcessName}";

            if (identity.Contains("whatsapp", StringComparison.OrdinalIgnoreCase))
            {
                return new ChatAppProfile(VirtualKeys.F);
            }

            if (identity.Contains("teams", StringComparison.OrdinalIgnoreCase))
            {
                return new ChatAppProfile(VirtualKeys.N);
            }

            return new ChatAppProfile(request.SearchShortCutKey);
        }

        private static async Task<IntPtr> GetMainWindowHandleAsync(
            AutomationTarget target,
            Process? startedOrExistingProcess,
            int timeoutMs,
            CancellationToken cancellationToken)
        {
            int waitedMs = 0;

            while (waitedMs < timeoutMs)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!target.IsShellLaunch && startedOrExistingProcess != null)
                {
                    try
                    {
                        startedOrExistingProcess.Refresh();

                        if (!startedOrExistingProcess.HasExited
                            && startedOrExistingProcess.MainWindowHandle != IntPtr.Zero)
                        {
                            return startedOrExistingProcess.MainWindowHandle;
                        }
                    }
                    catch
                    {
                        // Ignore process refresh failure.
                    }
                }

                Process? processWithWindow = GetRunningProcessWithWindow(target.ProcessName);

                if (processWithWindow != null)
                {
                    return processWithWindow.MainWindowHandle;
                }

                var windowByTitle = FindVisibleWindowByTitle(target.WindowTitleKeywords);
                if (windowByTitle != IntPtr.Zero)
                {
                    return windowByTitle;
                }

                await Task.Delay(250, cancellationToken);
                waitedMs += 250;
            }

            return IntPtr.Zero;
        }

        private static bool HasVisibleAppWindow(AutomationTarget target)
        {
            return GetRunningProcessWithWindow(target.ProcessName) is not null
                   || FindVisibleWindowByTitle(target.WindowTitleKeywords) != IntPtr.Zero;
        }

        private static void CloseWindow(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
            {
                return;
            }

            SendMessage(windowHandle, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
        }

        private static string[] BuildWindowKeywords(
            string appPath,
            string processName,
            string? appDisplayName = null)
        {
            var fileName = SafeGetFileNameWithoutExtension(appPath);

            var keywords = new List<string?>
            {
                appDisplayName,
                processName,
                fileName,
                fileName?.Replace(".Root", string.Empty, StringComparison.OrdinalIgnoreCase),
            };

            var identity = $"{appPath} {processName} {appDisplayName}";

            if (identity.Contains("telegram", StringComparison.OrdinalIgnoreCase))
            {
                keywords.Add("Telegram");
            }

            if (identity.Contains("whatsapp", StringComparison.OrdinalIgnoreCase))
            {
                keywords.Add("WhatsApp");
            }

            if (identity.Contains("teams", StringComparison.OrdinalIgnoreCase))
            {
                keywords.Add("Teams");
                keywords.Add("Microsoft Teams");
            }

            if (identity.Contains("messenger", StringComparison.OrdinalIgnoreCase))
            {
                keywords.Add("Messenger");
            }

            return keywords
                .Where(keyword => !string.IsNullOrWhiteSpace(keyword))
                .Select(keyword => keyword!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static string SafeGetFileNameWithoutExtension(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            try
            {
                return Path.GetFileNameWithoutExtension(path) ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static IntPtr FindVisibleWindowByTitle(IReadOnlyList<string> keywords)
        {
            var result = IntPtr.Zero;

            if (keywords.Count == 0)
            {
                return result;
            }

            EnumWindows((hWnd, _) =>
            {
                if (!IsWindowVisible(hWnd))
                {
                    return true;
                }

                var titleBuilder = new StringBuilder(512);
                _ = GetWindowText(hWnd, titleBuilder, titleBuilder.Capacity);

                var title = titleBuilder.ToString();

                if (string.IsNullOrWhiteSpace(title))
                {
                    return true;
                }

                if (keywords.Any(keyword =>
                        !string.IsNullOrWhiteSpace(keyword)
                        && title.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                {
                    result = hWnd;
                    return false;
                }

                return true;
            }, IntPtr.Zero);

            return result;
        }

        private static Process? GetRunningProcessWithWindow(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
            {
                return null;
            }

            try
            {
                return Process
                    .GetProcessesByName(processName)
                    .Where(p => !p.HasExited)
                    .FirstOrDefault(p =>
                    {
                        try
                        {
                            p.Refresh();
                            return p.MainWindowHandle != IntPtr.Zero;
                        }
                        catch
                        {
                            return false;
                        }
                    });
            }
            catch
            {
                return null;
            }
        }

        private static Process? GetAnyRunningProcess(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
            {
                return null;
            }

            try
            {
                return Process
                    .GetProcessesByName(processName)
                    .FirstOrDefault(p => !p.HasExited);
            }
            catch
            {
                return null;
            }
        }

        private static void FocusWindow(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
            {
                return;
            }

            ShowWindow(windowHandle, SW_RESTORE);
            SendKey(VirtualKeys.Menu);

            var foregroundWindow = GetForegroundWindow();
            var currentThreadId = GetCurrentThreadId();

            var foregroundThreadId = foregroundWindow == IntPtr.Zero
                ? 0
                : GetWindowThreadProcessId(foregroundWindow, out _);

            var targetThreadId = GetWindowThreadProcessId(windowHandle, out _);

            if (foregroundThreadId != 0)
            {
                AttachThreadInput(currentThreadId, foregroundThreadId, true);
            }

            if (targetThreadId != 0)
            {
                AttachThreadInput(currentThreadId, targetThreadId, true);
            }

            BringWindowToTop(windowHandle);
            SetForegroundWindow(windowHandle);
            ClickInsideWindow(windowHandle);

            if (targetThreadId != 0)
            {
                AttachThreadInput(currentThreadId, targetThreadId, false);
            }

            if (foregroundThreadId != 0)
            {
                AttachThreadInput(currentThreadId, foregroundThreadId, false);
            }
        }

        private static void ClickInsideWindow(IntPtr windowHandle)
        {
            if (!GetWindowRect(windowHandle, out var rect))
            {
                return;
            }

            var x = rect.Left + Math.Max(80, (rect.Right - rect.Left) / 2);
            var y = rect.Top + Math.Max(120, (rect.Bottom - rect.Top) / 3);

            SetCursorPos(x, y);
            SendMouseClick();
        }

        private static async Task SendMessageToRecipientAsync(
            IntPtr windowHandle,
            string recipientName,
            string message,
            ChatAppProfile profile,
            CancellationToken cancellationToken)
        {
            FocusWindow(windowHandle);

            await Task.Delay(700, cancellationToken);

            SendHotKey(VirtualKeys.Control, profile.OpenChatShortcutKey);

            await Task.Delay(1500, cancellationToken);

            SendHotKey(VirtualKeys.Control, VirtualKeys.A);

            await Task.Delay(300, cancellationToken);

            PasteText(recipientName);

            await Task.Delay(1800, cancellationToken);

            SendKey(VirtualKeys.Enter);

            await Task.Delay(2200, cancellationToken);

            PasteText(message);

            await Task.Delay(800, cancellationToken);

            SendKey(VirtualKeys.Enter);
        }

        private static void ValidateRequest(SendMessageRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.AppPath))
            {
                throw new ArgumentException("AppPath is required.", nameof(request.AppPath));
            }

            if (request.RecipientNames == null || request.RecipientNames.Count == 0)
            {
                throw new ArgumentException("At least one recipient is required.", nameof(request.RecipientNames));
            }

            if (request.RecipientNames.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException("Recipient name cannot be empty.", nameof(request.RecipientNames));
            }

            if (string.IsNullOrWhiteSpace(request.Message))
            {
                throw new ArgumentException("Message is required.", nameof(request.Message));
            }
        }

        private static void SendText(string text)
        {
            foreach (char character in text)
            {
                SendUnicodeChar(character);
                Thread.Sleep(10);
            }
        }

        private static void PasteText(string text)
        {
            if (!TrySetClipboardText(text))
            {
                SendText(text);
                return;
            }

            SendHotKey(VirtualKeys.Control, VirtualKeys.V);
        }

        private static bool TrySetClipboardText(string text)
        {
            var bytes = Encoding.Unicode.GetBytes(text + '\0');

            var handle = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)bytes.Length);
            if (handle == IntPtr.Zero)
            {
                return false;
            }

            var pointer = GlobalLock(handle);
            if (pointer == IntPtr.Zero)
            {
                return false;
            }

            Marshal.Copy(bytes, 0, pointer, bytes.Length);
            GlobalUnlock(handle);

            var opened = false;

            for (var attempt = 0; attempt < 10; attempt++)
            {
                if (OpenClipboard(IntPtr.Zero))
                {
                    opened = true;
                    break;
                }

                Thread.Sleep(50);
            }

            if (!opened)
            {
                return false;
            }

            try
            {
                EmptyClipboard();
                return SetClipboardData(CF_UNICODETEXT, handle) != IntPtr.Zero;
            }
            finally
            {
                CloseClipboard();
            }
        }

        private static void SendUnicodeChar(char character)
        {
            ushort scanCode = character;

            INPUT[] inputs =
            [
                new INPUT
                {
                    type = INPUT_KEYBOARD,
                    U = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = 0,
                            wScan = scanCode,
                            dwFlags = KEYEVENTF_UNICODE,
                            time = 0,
                            dwExtraInfo = IntPtr.Zero
                        }
                    }
                },
                new INPUT
                {
                    type = INPUT_KEYBOARD,
                    U = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = 0,
                            wScan = scanCode,
                            dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP,
                            time = 0,
                            dwExtraInfo = IntPtr.Zero
                        }
                    }
                }
            ];

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        private static void SendHotKey(ushort modifierKey, ushort key)
        {
            KeyDown(modifierKey);
            Thread.Sleep(50);

            KeyDown(key);
            Thread.Sleep(50);

            KeyUp(key);
            Thread.Sleep(50);

            KeyUp(modifierKey);
        }

        private static void SendKey(ushort key)
        {
            KeyDown(key);
            Thread.Sleep(50);
            KeyUp(key);
        }

        private static void SendMouseClick()
        {
            INPUT[] inputs =
            [
                new INPUT
                {
                    type = INPUT_MOUSE,
                    U = new InputUnion
                    {
                        mi = new MOUSEINPUT
                        {
                            dwFlags = MOUSEEVENTF_LEFTDOWN,
                            dwExtraInfo = IntPtr.Zero
                        }
                    }
                },
                new INPUT
                {
                    type = INPUT_MOUSE,
                    U = new InputUnion
                    {
                        mi = new MOUSEINPUT
                        {
                            dwFlags = MOUSEEVENTF_LEFTUP,
                            dwExtraInfo = IntPtr.Zero
                        }
                    }
                }
            ];

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        private static void KeyDown(ushort key)
        {
            INPUT[] inputs =
            [
                new INPUT
                {
                    type = INPUT_KEYBOARD,
                    U = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = key,
                            wScan = 0,
                            dwFlags = 0,
                            time = 0,
                            dwExtraInfo = IntPtr.Zero
                        }
                    }
                }
            ];

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        private static void KeyUp(ushort key)
        {
            INPUT[] inputs =
            [
                new INPUT
                {
                    type = INPUT_KEYBOARD,
                    U = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = key,
                            wScan = 0,
                            dwFlags = KEYEVENTF_KEYUP,
                            time = 0,
                            dwExtraInfo = IntPtr.Zero
                        }
                    }
                }
            ];

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public InputUnion U;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)]
            public KEYBDINPUT ki;

            [FieldOffset(0)]
            public MOUSEINPUT mi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private sealed record AutomationTarget(
            string LaunchFileName,
            string LaunchArguments,
            string ProcessName,
            string[] WindowTitleKeywords,
            bool IsShellLaunch);

        private sealed record ChatAppProfile(ushort OpenChatShortcutKey);
#endif
    }
}