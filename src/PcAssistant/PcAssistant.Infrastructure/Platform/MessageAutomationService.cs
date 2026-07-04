using PcAssistant.Application.Abstractions.Services;
using PcAssistant.Application.Models;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PcAssistant.Infrastructure.Platform
{
    public class MessageAutomationService : IMessageAutomationService
    {
        private const int SW_RESTORE = 9;

        private const uint INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_UNICODE = 0x0004;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        public async Task SendMessageToSpecificPersonAsync(
            SendMessageRequest request,
            CancellationToken cancellationToken = default)
        {
#if WINDOWS
            ValidateRequest(request);

            string processName = Path.GetFileNameWithoutExtension(request.AppPath);

            Process process = await GetOrStartProcessAsync(
                request.AppPath,
                processName,
                cancellationToken);

            IntPtr windowHandle = await GetMainWindowHandleAsync(
                processName,
                process,
                timeoutMs: 10000,
                cancellationToken);

            if (windowHandle == IntPtr.Zero)
                throw new InvalidOperationException("Failed to get the application window handle.");

            FocusWindow(windowHandle);

            await Task.Delay(1000, cancellationToken);

            foreach (string recipientName in request.RecipientNames)
            {
                await SendMessageToRecipientAsync(
                    recipientName,
                    request.Message,
                    request.SearchShortCutKey,
                    cancellationToken);

                await Task.Delay(1000, cancellationToken);
            }
#else
            await Task.CompletedTask;
            throw new PlatformNotSupportedException("Message automation is only supported on Windows.");
#endif
        }

        private static async Task<Process> GetOrStartProcessAsync(
            string appPath,
            string processName,
            CancellationToken cancellationToken)
        {
            Process? existingProcess = GetRunningProcessWithWindow(processName);

            if (existingProcess != null)
            {
                return existingProcess;
            }

            Process? runningProcessWithoutWindow = Process
                .GetProcessesByName(processName)
                .FirstOrDefault(p => !p.HasExited);

            if (runningProcessWithoutWindow != null)
            {
                return runningProcessWithoutWindow;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = appPath,
                UseShellExecute = true
            };

            Process? startedProcess = Process.Start(startInfo);

            if (startedProcess == null)
                throw new InvalidOperationException("Failed to start the application.");

            await Task.Delay(3000, cancellationToken);

            return startedProcess;
        }

        private static async Task<IntPtr> GetMainWindowHandleAsync(
            string processName,
            Process startedOrExistingProcess,
            int timeoutMs,
            CancellationToken cancellationToken)
        {
            int waitedMs = 0;

            while (waitedMs < timeoutMs)
            {
                cancellationToken.ThrowIfCancellationRequested();

                startedOrExistingProcess.Refresh();

                if (!startedOrExistingProcess.HasExited &&
                    startedOrExistingProcess.MainWindowHandle != IntPtr.Zero)
                {
                    return startedOrExistingProcess.MainWindowHandle;
                }

                Process? processWithWindow = GetRunningProcessWithWindow(processName);

                if (processWithWindow != null)
                {
                    return processWithWindow.MainWindowHandle;
                }

                await Task.Delay(250, cancellationToken);
                waitedMs += 250;
            }

            return IntPtr.Zero;
        }

        private static Process? GetRunningProcessWithWindow(string processName)
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

        private static void FocusWindow(IntPtr windowHandle)
        {
            ShowWindow(windowHandle, SW_RESTORE);
            SetForegroundWindow(windowHandle);
        }

        private static async Task SendMessageToRecipientAsync(
            string recipientName,
            string message,
            ushort searchShortcutKey,
            CancellationToken cancellationToken)
        {
            // Open search box. Telegram Desktop usually uses Ctrl + K.
            SendHotKey(VirtualKeys.Control, searchShortcutKey);

            await Task.Delay(700, cancellationToken);

            // Search recipient.
            SendText(recipientName);

            await Task.Delay(700, cancellationToken);

            // Open selected chat.
            SendKey(VirtualKeys.Enter);

            await Task.Delay(1200, cancellationToken);

            // Type message.
            SendText(message);

            await Task.Delay(300, cancellationToken);

            // Send message.
            SendKey(VirtualKeys.Enter);
        }

        private static void ValidateRequest(SendMessageRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.AppPath))
                throw new ArgumentException("AppPath is required.", nameof(request.AppPath));

            if (request.RecipientNames == null || request.RecipientNames.Count == 0)
                throw new ArgumentException("At least one recipient is required.", nameof(request.RecipientNames));

            if (request.RecipientNames.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentException("Recipient name cannot be empty.", nameof(request.RecipientNames));

            if (string.IsNullOrWhiteSpace(request.Message))
                throw new ArgumentException("Message is required.", nameof(request.Message));
        }

        private static void SendText(string text)
        {
            foreach (char character in text)
            {
                SendUnicodeChar(character);
                Thread.Sleep(10);
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

        private static void KeyDown(ushort key)
        {
            INPUT[] inputs =
            {
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
            };

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        private static void KeyUp(ushort key)
        {
            INPUT[] inputs =
            {
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
            };

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
    }
}