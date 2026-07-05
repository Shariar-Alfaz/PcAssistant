using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Versioning;
using PcAssistant.Application.Abstractions.Services;

namespace PcAssistant.Infrastructure.Platform;

public sealed class PythonAiApiProcessService : IAiApiProcessService, IAsyncDisposable
{
    private static readonly Uri HealthUri = new("http://127.0.0.1:8000/health");
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static Process? _process;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var executablePath = GetExecutablePath();

        await Gate.WaitAsync(cancellationToken);
        try
        {
            if (_process is { HasExited: false })
            {
                return;
            }

            _process = FindBundledApiProcess(executablePath);
            if (_process is { HasExited: false } || await IsApiHealthyAsync(cancellationToken))
            {
                return;
            }

            if (!File.Exists(executablePath))
            {
                Debug.WriteLine($"Python AI API executable was not found: {executablePath}");
                return;
            }

            _process = Process.Start(new ProcessStartInfo
            {
                FileName = executablePath,
                WorkingDirectory = Path.GetDirectoryName(executablePath),
                UseShellExecute = false,
                CreateNoWindow = true,
            });
        }
        finally
        {
            Gate.Release();
        }

        await WaitForApiAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        await Gate.WaitAsync(cancellationToken);
        try
        {
            var executablePath = GetExecutablePath();
            var processes = FindBundledApiProcesses(executablePath).ToList();
            if (_process is not null && processes.All(process => process.Id != _process.Id))
            {
                processes.Add(_process);
            }

            foreach (var process in processes)
            {
                await StopProcessAsync(process, cancellationToken);
            }

            _process = null;
        }
        finally
        {
            Gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }

    private static string GetExecutablePath()
    {
        return Path.Combine(AppContext.BaseDirectory, "Api", "pcAssistantApi.exe");
    }

    [SupportedOSPlatform("windows")]
    private static async Task StopProcessAsync(Process process, CancellationToken cancellationToken)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(cancellationToken);
            }
        }
        catch (InvalidOperationException)
        {
        }
        catch (Win32Exception)
        {
        }
        finally
        {
            process.Dispose();
        }
    }

    [SupportedOSPlatform("windows")]
    private static Process? FindBundledApiProcess(string executablePath)
    {
        return FindBundledApiProcesses(executablePath).FirstOrDefault();
    }

    [SupportedOSPlatform("windows")]
    private static IEnumerable<Process> FindBundledApiProcesses(string executablePath)
    {
        foreach (var process in Process.GetProcessesByName("pcAssistantApi"))
        {
            if (IsBundledApiProcess(process, executablePath))
            {
                yield return process;
            }
            else
            {
                process.Dispose();
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private static bool IsBundledApiProcess(Process process, string executablePath)
    {
        try
        {
            return process.MainModule?.FileName?.Equals(executablePath, StringComparison.OrdinalIgnoreCase) == true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (Win32Exception)
        {
            return false;
        }
    }

    private static async Task WaitForApiAsync(CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));

        while (!timeout.IsCancellationRequested)
        {
            if (await IsApiHealthyAsync(timeout.Token))
            {
                return;
            }

            try
            {
                await Task.Delay(500, timeout.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private static async Task<bool> IsApiHealthyAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(800) };
            using var response = await client.GetAsync(HealthUri, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }
}
