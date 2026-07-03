using PcAssistant.Application.Abstractions;
using PcAssistant.Application.Models;

namespace PcAssistant.Infrastructure.Commands;

public sealed class CommandSafetyValidator : ICommandSafetyValidator
{
    public SafetyValidationResult ValidateFileSystemTarget(string? pathText)
    {
        if (string.IsNullOrWhiteSpace(pathText))
        {
            return SafetyValidationResult.Blocked("No folder path could be resolved.");
        }

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(pathText));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return SafetyValidationResult.Blocked("The folder path is not valid.");
        }

        // Safety boundary: refuse root, OS, program, and profile-root targets before any file action.
        var blockedPaths = GetBlockedPaths();
        foreach (var blockedPath in blockedPaths)
        {
            if (IsSamePath(fullPath, blockedPath.Path)
                || (blockedPath.IncludeChildren && IsChildOf(fullPath, blockedPath.Path)))
            {
                return SafetyValidationResult.Blocked($"This path is protected and cannot be modified: {blockedPath.Path}");
            }
        }

        if (Path.GetPathRoot(fullPath)?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Equals(fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparison.OrdinalIgnoreCase) == true)
        {
            return SafetyValidationResult.Blocked("Drive roots cannot be modified.");
        }

        if (fullPath.Contains($"{Path.DirectorySeparatorChar}System32{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
            || fullPath.EndsWith($"{Path.DirectorySeparatorChar}System32", StringComparison.OrdinalIgnoreCase))
        {
            return SafetyValidationResult.Blocked("System32 paths cannot be modified.");
        }

        return SafetyValidationResult.Allowed(fullPath);
    }

    private static IReadOnlyList<BlockedPath> GetBlockedPaths()
    {
        var paths = new List<BlockedPath?>
        {
            new(@"C:\", false),
            CreateBlockedPath(Environment.GetFolderPath(Environment.SpecialFolder.Windows), true),
            CreateBlockedPath(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), true),
            CreateBlockedPath(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), true),
            CreateBlockedPath(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), false),
        };

        return paths
            .Where(path => path is not null && !string.IsNullOrWhiteSpace(path.Path))
            .Select(path => new BlockedPath(Path.GetFullPath(path!.Path), path.IncludeChildren))
            .DistinctBy(path => path.Path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static BlockedPath? CreateBlockedPath(string? path, bool includeChildren)
    {
        return string.IsNullOrWhiteSpace(path) ? null : new BlockedPath(path, includeChildren);
    }

    private static bool IsSamePath(string path, string blockedPath)
    {
        return Normalize(path).Equals(Normalize(blockedPath), StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsChildOf(string path, string blockedPath)
    {
        var normalizedPath = Normalize(path) + Path.DirectorySeparatorChar;
        var normalizedBlockedPath = Normalize(blockedPath) + Path.DirectorySeparatorChar;
        return normalizedPath.StartsWith(normalizedBlockedPath, StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string path)
    {
        return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}

internal sealed class BlockedPath
{
    public BlockedPath(string path, bool includeChildren)
    {
        Path = path;
        IncludeChildren = includeChildren;
    }

    public string Path { get; }

    public bool IncludeChildren { get; }
}
