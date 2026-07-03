using PcAssistant.Application.Models;

namespace PcAssistant.Infrastructure.Commands.Parsing;

public sealed class LocationAliasResolver
{
    public FolderLocation Resolve(string text)
    {
        var normalized = text.ToLowerInvariant();
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        var aliases = new (string Alias, string Type, Func<string> Resolve)[]
        {
            ("desktop", "known-folder", () => Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)),
            ("downloads", "known-folder", () => Path.Combine(userProfile, "Downloads")),
            ("download", "known-folder", () => Path.Combine(userProfile, "Downloads")),
            ("documents", "known-folder", () => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)),
            ("pictures", "known-folder", () => Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)),
            ("videos", "known-folder", () => Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)),
            ("music", "known-folder", () => Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)),
            ("temp folder", "temp-folder", Path.GetTempPath),
            ("project folder", "current-directory", () => Environment.CurrentDirectory),
            ("repository folder", "current-directory", () => Environment.CurrentDirectory),
            ("current directory", "current-directory", () => Environment.CurrentDirectory),
            ("d drive", "drive-root", () => @"D:\"),
            ("e drive", "drive-root", () => @"E:\"),
        };

        foreach (var alias in aliases)
        {
            if (normalized.Contains(alias.Alias, StringComparison.OrdinalIgnoreCase))
            {
                return new FolderLocation(alias.Alias, alias.Type, alias.Resolve());
            }
        }

        return new FolderLocation("current directory", "current-directory", Environment.CurrentDirectory);
    }
}
