using PcAssistant.Application.Abstractions;
using PcAssistant.Application.Models;

namespace PcAssistant.Infrastructure.Platform;

public sealed class DirectorySuggestionService : IDirectorySuggestionService
{
    public Task<IReadOnlyList<DirectorySuggestion>> SuggestAsync(
        string? query,
        int count,
        CancellationToken cancellationToken = default)
    {
        var trimmedQuery = query?.Trim() ?? string.Empty;
        var suggestions = string.IsNullOrWhiteSpace(trimmedQuery)
            ? GetDefaultDirectories()
            : GetMatchingDirectories(trimmedQuery);

        var result = suggestions
            .Where(suggestion => !string.IsNullOrWhiteSpace(suggestion.Path))
            .DistinctBy(suggestion => suggestion.Path, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(1, count))
            .ToArray();

        return Task.FromResult<IReadOnlyList<DirectorySuggestion>>(result);
    }

    public Task<IReadOnlyList<DirectorySuggestion>> SuggestChildrenAsync(
        string directoryPath,
        string? query,
        int count,
        CancellationToken cancellationToken = default)
    {
        var trimmedPath = directoryPath.Trim();
        if (string.IsNullOrWhiteSpace(trimmedPath) || !Directory.Exists(trimmedPath))
        {
            return Task.FromResult<IReadOnlyList<DirectorySuggestion>>(Array.Empty<DirectorySuggestion>());
        }

        var trimmedQuery = query?.Trim() ?? string.Empty;
        var searchRoot = trimmedPath;
        var namePrefix = trimmedQuery;
        if (!string.IsNullOrWhiteSpace(trimmedQuery))
        {
            var combinedQuery = Path.Combine(trimmedPath, trimmedQuery);
            var queryParent = GetSearchParent(combinedQuery);
            if (IsSameOrChildPath(trimmedPath, queryParent))
            {
                searchRoot = queryParent;
                namePrefix = GetNamePrefix(combinedQuery);
            }
        }

        var suggestions = SafeEnumerateDirectories(searchRoot)
            .Where(path => MatchesName(path, namePrefix))
            .Select(path => ToSuggestion(path, "folder"))
            .Concat(SafeEnumerateFiles(searchRoot)
                .Where(path => MatchesName(path, namePrefix))
                .Select(path => ToSuggestion(path, "file")))
            .DistinctBy(suggestion => suggestion.Path, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(1, count))
            .ToArray();

        return Task.FromResult<IReadOnlyList<DirectorySuggestion>>(suggestions);
    }

    private static IEnumerable<DirectorySuggestion> GetDefaultDirectories()
    {
        foreach (var drive in GetDrives())
        {
            yield return ToSuggestion(drive.RootDirectory.FullName, "drive");
        }

        foreach (var path in GetKnownFolders())
        {
            yield return ToSuggestion(path, "known-folder");
        }

        foreach (var drive in GetDrives().Where(drive => drive.IsReady))
        {
            foreach (var child in SafeEnumerateDirectories(drive.RootDirectory.FullName))
            {
                yield return ToSuggestion(child, "folder");
            }
        }
    }

    private static IEnumerable<DirectorySuggestion> GetMatchingDirectories(string query)
    {
        var expandedQuery = Environment.ExpandEnvironmentVariables(query);

        foreach (var drive in GetDrives())
        {
            var drivePath = drive.RootDirectory.FullName;
            var driveName = drivePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (drivePath.StartsWith(query, StringComparison.OrdinalIgnoreCase)
                || driveName.StartsWith(query, StringComparison.OrdinalIgnoreCase)
                || drive.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            {
                yield return ToSuggestion(drivePath, "drive");
            }
        }

        if (Directory.Exists(expandedQuery))
        {
            yield return ToSuggestion(Path.GetFullPath(expandedQuery), IsDriveRoot(expandedQuery) ? "drive" : "folder");

            foreach (var child in SafeEnumerateDirectories(expandedQuery))
            {
                yield return ToSuggestion(child, "folder");
            }

            yield break;
        }

        var parent = GetSearchParent(expandedQuery);
        var namePrefix = GetNamePrefix(expandedQuery);
        foreach (var child in SafeEnumerateDirectories(parent))
        {
            var name = Path.GetFileName(child.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (string.IsNullOrWhiteSpace(namePrefix)
                || name.StartsWith(namePrefix, StringComparison.OrdinalIgnoreCase)
                || child.Contains(namePrefix, StringComparison.OrdinalIgnoreCase))
            {
                yield return ToSuggestion(child, "folder");
            }
        }

        foreach (var knownFolder in GetKnownFolders())
        {
            var name = Path.GetFileName(knownFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (knownFolder.Contains(query, StringComparison.OrdinalIgnoreCase)
                || name.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                yield return ToSuggestion(knownFolder, "known-folder");
            }
        }
    }

    private static IEnumerable<DriveInfo> GetDrives()
    {
        try
        {
            return DriveInfo.GetDrives().OrderBy(drive => drive.Name, StringComparer.OrdinalIgnoreCase);
        }
        catch (IOException)
        {
            return Array.Empty<DriveInfo>();
        }
    }

    private static IEnumerable<string> GetKnownFolders()
    {
        var folders = new[]
        {
            Environment.SpecialFolder.DesktopDirectory,
            Environment.SpecialFolder.MyDocuments,
            Environment.SpecialFolder.UserProfile,
            Environment.SpecialFolder.MyMusic,
            Environment.SpecialFolder.MyPictures,
            Environment.SpecialFolder.MyVideos,
        };

        foreach (var folder in folders)
        {
            var path = Environment.GetFolderPath(folder);
            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
            {
                yield return path;
            }
        }

        var downloads = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads");
        if (Directory.Exists(downloads))
        {
            yield return downloads;
        }
    }

    private static string GetSearchParent(string query)
    {
        try
        {
            var directoryName = Path.GetDirectoryName(query);
            if (!string.IsNullOrWhiteSpace(directoryName) && Directory.Exists(directoryName))
            {
                return directoryName;
            }

            var root = Path.GetPathRoot(query);
            if (!string.IsNullOrWhiteSpace(root) && Directory.Exists(root))
            {
                return root;
            }
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
        }

        return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }

    private static string GetNamePrefix(string query)
    {
        try
        {
            return Path.GetFileName(query.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }
        catch (ArgumentException)
        {
            return string.Empty;
        }
    }

    private static IEnumerable<string> SafeEnumerateDirectories(string path)
    {
        try
        {
            return Directory.EnumerateDirectories(path).Order(StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or DirectoryNotFoundException or IOException or ArgumentException)
        {
            return Array.Empty<string>();
        }
    }

    private static IEnumerable<string> SafeEnumerateFiles(string path)
    {
        try
        {
            return Directory.EnumerateFiles(path).Order(StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or DirectoryNotFoundException or IOException or ArgumentException)
        {
            return Array.Empty<string>();
        }
    }

    private static bool MatchesName(string path, string namePrefix)
    {
        if (string.IsNullOrWhiteSpace(namePrefix))
        {
            return true;
        }

        var name = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return name.StartsWith(namePrefix, StringComparison.OrdinalIgnoreCase)
            || name.Contains(namePrefix, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSameOrChildPath(string parentPath, string candidatePath)
    {
        try
        {
            var parent = Path.GetFullPath(parentPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var candidate = Path.GetFullPath(candidatePath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            return candidate.Equals(parent, StringComparison.OrdinalIgnoreCase)
                || candidate.StartsWith(parent + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                || candidate.StartsWith(parent + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }

    private static string GetDisplayName(string path)
    {
        var trimmedPath = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (IsDriveRoot(path))
        {
            return trimmedPath;
        }

        var name = Path.GetFileName(trimmedPath);
        return string.IsNullOrWhiteSpace(name) ? path : name;
    }

    private static DirectorySuggestion ToSuggestion(string path, string kind)
    {
        return new DirectorySuggestion(path, GetDisplayName(path), kind);
    }

    private static bool IsDriveRoot(string path)
    {
        var root = Path.GetPathRoot(path);
        return !string.IsNullOrWhiteSpace(root)
            && root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Equals(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);
    }
}
