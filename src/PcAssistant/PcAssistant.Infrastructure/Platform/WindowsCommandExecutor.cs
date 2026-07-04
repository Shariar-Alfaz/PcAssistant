using System.Diagnostics;
using System.Text;
using PcAssistant.Application.Abstractions;
using PcAssistant.Application.Models;

namespace PcAssistant.Infrastructure.Platform;

public sealed class WindowsCommandExecutor(ICommandSafetyValidator safetyValidator) : IWindowsCommandExecutor
{
    public Task<CommandExecutionResult> ExecuteAsync(
        ParsedCommand command,
        bool confirmed,
        CancellationToken cancellationToken = default)
    {
        return command.CommandLabel switch
        {
            "filesystem.create_folder" => CreateFolderAsync(command),
            "filesystem.delete_folder" => DeleteFolderAsync(command, confirmed),
            "filesystem.open_folder" => OpenFolderAsync(command),
            "filesystem.get_file_details" => GetFileDetailsAsync(command),
            "system.restart" => RestartAsync(confirmed, cancellationToken),
            "system.cancel_restart" => CancelRestartAsync(cancellationToken),
            _ => Task.FromResult(CommandExecutionResult.Failure("I could not understand this as a PC command.")),
        };
    }

    private Task<CommandExecutionResult> CreateFolderAsync(ParsedCommand command)
    {
        var validation = safetyValidator.ValidateFileSystemTarget(command.ResolvedPath ?? command.PathText);
        if (!validation.IsAllowed || string.IsNullOrWhiteSpace(validation.ResolvedPath))
        {
            return Task.FromResult(CommandExecutionResult.Failure(validation.Message ?? "The folder path is blocked."));
        }

        Directory.CreateDirectory(validation.ResolvedPath);
        return Task.FromResult(CommandExecutionResult.Success($"Created folder: {validation.ResolvedPath}"));
    }

    private Task<CommandExecutionResult> DeleteFolderAsync(ParsedCommand command, bool confirmed)
    {
        if (!confirmed)
        {
            return Task.FromResult(CommandExecutionResult.Failure("Deleting folders requires confirmation."));
        }

        var validation = safetyValidator.ValidateFileSystemTarget(command.ResolvedPath ?? command.PathText);
        if (!validation.IsAllowed || string.IsNullOrWhiteSpace(validation.ResolvedPath))
        {
            return Task.FromResult(CommandExecutionResult.Failure(validation.Message ?? "The folder path is blocked."));
        }

        if (!Directory.Exists(validation.ResolvedPath))
        {
            return Task.FromResult(CommandExecutionResult.Failure($"Folder not found: {validation.ResolvedPath}"));
        }

#if WINDOWS
        Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(
            validation.ResolvedPath,
            Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
            Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);

        return Task.FromResult(CommandExecutionResult.Success($"Moved folder to Recycle Bin: {validation.ResolvedPath}"));
#else
        return Task.FromResult(CommandExecutionResult.Failure("Folder deletion is only enabled on Windows."));
#endif
    }

    private static Task<CommandExecutionResult> OpenFolderAsync(ParsedCommand command)
    {
        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult(CommandExecutionResult.Failure("Opening folders is only available on Windows."));
        }

        var path = ResolveReadOnlyPath(command);
        if (string.IsNullOrWhiteSpace(path))
        {
            return Task.FromResult(CommandExecutionResult.Failure("No folder path could be resolved."));
        }

        if (!Directory.Exists(path))
        {
            return Task.FromResult(CommandExecutionResult.Failure($"Folder not found: {path}"));
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
            });
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return Task.FromResult(CommandExecutionResult.Failure($"Could not open folder: {ex.Message}"));
        }

        return Task.FromResult(CommandExecutionResult.Success($"Opened folder: {path}"));
    }

    private static Task<CommandExecutionResult> GetFileDetailsAsync(ParsedCommand command)
    {
        var path = ResolveReadOnlyPath(command);
        if (string.IsNullOrWhiteSpace(path))
        {
            return Task.FromResult(CommandExecutionResult.Failure("No file path could be resolved."));
        }

        if (!File.Exists(path))
        {
            return Task.FromResult(CommandExecutionResult.Failure($"File not found: {path}"));
        }

        var file = new FileInfo(path);
        file.Refresh();

        var details = new StringBuilder()
            .AppendLine($"File details: {file.FullName}")
            .AppendLine($"Name: {file.Name}")
            .AppendLine($"Directory: {file.DirectoryName}")
            .AppendLine($"Extension: {file.Extension}")
            .AppendLine($"Size: {FormatByteSize(file.Length)} ({file.Length:N0} bytes)")
            .AppendLine($"Created: {file.CreationTime:yyyy-MM-dd HH:mm:ss zzz}")
            .AppendLine($"Modified: {file.LastWriteTime:yyyy-MM-dd HH:mm:ss zzz}")
            .AppendLine($"Accessed: {file.LastAccessTime:yyyy-MM-dd HH:mm:ss zzz}")
            .AppendLine($"Attributes: {file.Attributes}");

        var versionInfo = FileVersionInfo.GetVersionInfo(file.FullName);
        AppendIfPresent(details, "Author", versionInfo.CompanyName);
        AppendIfPresent(details, "Product", versionInfo.ProductName);
        AppendIfPresent(details, "Description", versionInfo.FileDescription);
        AppendIfPresent(details, "File version", versionInfo.FileVersion);
        AppendIfPresent(details, "Product version", versionInfo.ProductVersion);
        AppendIfPresent(details, "Copyright", versionInfo.LegalCopyright);

        return Task.FromResult(CommandExecutionResult.Success(details.ToString().TrimEnd()));
    }

    private static async Task<CommandExecutionResult> RestartAsync(bool confirmed, CancellationToken cancellationToken)
    {
        if (!confirmed)
        {
            return CommandExecutionResult.Failure("Restart requires confirmation.");
        }

        if (!OperatingSystem.IsWindows())
        {
            return CommandExecutionResult.Failure("Restart is only available on Windows.");
        }

        return await RunShutdownAsync("/r /t 30", "Restart scheduled in 30 seconds.", cancellationToken);
    }

    private static async Task<CommandExecutionResult> CancelRestartAsync(CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows())
        {
            return CommandExecutionResult.Failure("Cancel restart is only available on Windows.");
        }

        return await RunShutdownAsync("/a", "Pending restart or shutdown was cancelled.", cancellationToken);
    }

    private static async Task<CommandExecutionResult> RunShutdownAsync(
        string arguments,
        string successMessage,
        CancellationToken cancellationToken)
    {
#if WINDOWS
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "shutdown",
            Arguments = arguments,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        });

        if (process is null)
        {
            return CommandExecutionResult.Failure("Could not start the Windows shutdown command.");
        }

        await process.WaitForExitAsync(cancellationToken);
        if (process.ExitCode == 0)
        {
            return CommandExecutionResult.Success(successMessage);
        }

        var error = await process.StandardError.ReadToEndAsync(cancellationToken);
        return CommandExecutionResult.Failure(string.IsNullOrWhiteSpace(error) ? "Windows rejected the command." : error.Trim());
#else
        await Task.CompletedTask;
        return CommandExecutionResult.Failure("Shutdown commands are only available on Windows.");
#endif
    }

    private static string? ResolveReadOnlyPath(ParsedCommand command)
    {
        var pathText = command.ResolvedPath ?? command.PathText;
        if (string.IsNullOrWhiteSpace(pathText))
        {
            return null;
        }

        try
        {
            return Path.GetFullPath(Environment.ExpandEnvironmentVariables(pathText));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }

    private static string FormatByteSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var size = (double)bytes;
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return unit == 0 ? $"{bytes} {units[unit]}" : $"{size:N2} {units[unit]}";
    }

    private static void AppendIfPresent(StringBuilder builder, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            builder.AppendLine($"{label}: {value.Trim()}");
        }
    }
}
