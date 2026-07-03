using System.Diagnostics;
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
}
