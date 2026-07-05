using PcAssistant.Application.Abstractions.Services;
using PcAssistant.Application.Models;
using System.Text;

#if WINDOWS
using System.Xml.Linq;
using Windows.Management.Deployment;
#endif

namespace PcAssistant.Infrastructure.Platform;

public sealed class InstalledAppDiscoveryService : IInstalledAppDiscoveryService
{
    public Task<IReadOnlyList<InstalledAppOption>> ListInstalledAppsAsync(
        CancellationToken cancellationToken = default)
    {
#if WINDOWS
        return Task.Run<IReadOnlyList<InstalledAppOption>>(
            () =>
            {
                var apps = new Dictionary<string, InstalledAppOption>(
                    StringComparer.OrdinalIgnoreCase);

                foreach (var shortcutPath in EnumerateStartMenuShortcuts())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var shortcut = ResolveShortcut(shortcutPath);
                    if (shortcut is null)
                    {
                        continue;
                    }

                    var displayName = Path.GetFileNameWithoutExtension(shortcutPath);
                    if (string.IsNullOrWhiteSpace(displayName))
                    {
                        continue;
                    }

                    apps.TryAdd(
                        shortcut.LaunchPath,
                        new InstalledAppOption(
                            displayName.Trim(),
                            shortcut.LaunchPath,
                            shortcut.IconSourcePath));
                }

                var shellDisplayNames = ReadAppsFolderDisplayNames();

                foreach (var packageApp in EnumeratePackagedApps(shellDisplayNames))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    apps.TryAdd(
                        packageApp.AppPath,
                        packageApp);
                }

                return apps.Values
                    .Where(app => !string.IsNullOrWhiteSpace(app.DisplayName))
                    .OrderBy(app => app.DisplayName)
                    .ThenBy(app => app.AppPath)
                    .ToArray();
            },
            cancellationToken);
#else
        return Task.FromResult<IReadOnlyList<InstalledAppOption>>(
            Array.Empty<InstalledAppOption>());
#endif
    }

    public Task<string?> GetAppIconDataUrlAsync(
        string appPath,
        string? iconSourcePath = null,
        CancellationToken cancellationToken = default)
    {
#if WINDOWS
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(
            ResolveIconDataUrl(appPath, iconSourcePath));
#else
        return Task.FromResult<string?>(null);
#endif
    }

#if WINDOWS
    private static IEnumerable<string> EnumerateStartMenuShortcuts()
    {
        var startMenuFolders = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
        };

        foreach (var folder in startMenuFolders
                     .Where(Directory.Exists)
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            foreach (var shortcut in Directory.EnumerateFiles(
                         folder,
                         "*.lnk",
                         SearchOption.AllDirectories))
            {
                yield return shortcut;
            }
        }
    }

    private static ShortcutApp? ResolveShortcut(string shortcutPath)
    {
        try
        {
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType is null)
            {
                return File.Exists(shortcutPath)
                    ? new ShortcutApp(shortcutPath, shortcutPath)
                    : null;
            }

            dynamic shell = Activator.CreateInstance(shellType)!;
            dynamic shortcut = shell.CreateShortcut(shortcutPath);

            string targetPath = shortcut.TargetPath;
            string iconLocation = shortcut.IconLocation;

            var launchPath = !string.IsNullOrWhiteSpace(targetPath)
                             && File.Exists(targetPath)
                ? targetPath.Trim()
                : shortcutPath;

            return File.Exists(launchPath)
                ? new ShortcutApp(
                    launchPath,
                    ResolveIconPath(iconLocation) ?? shortcutPath)
                : null;
        }
        catch
        {
            return File.Exists(shortcutPath)
                ? new ShortcutApp(shortcutPath, shortcutPath)
                : null;
        }
    }

    private static Dictionary<string, string> ReadAppsFolderDisplayNames()
    {
        var result = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase);

        try
        {
            var thread = new Thread(() =>
            {
                foreach (var item in ReadAppsFolderDisplayNamesCore())
                {
                    result.TryAdd(item.Key, item.Value);
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }
        catch
        {
            // Ignore shell lookup failure.
        }

        return result;
    }

    private static Dictionary<string, string> ReadAppsFolderDisplayNamesCore()
    {
        var result = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase);

        try
        {
            var shellType = Type.GetTypeFromProgID("Shell.Application");
            if (shellType is null)
            {
                return result;
            }

            dynamic shell = Activator.CreateInstance(shellType)!;
            dynamic appsFolder = shell.NameSpace("shell:AppsFolder");

            if (appsFolder is null)
            {
                return result;
            }

            dynamic items = appsFolder.Items();

            foreach (dynamic item in items)
            {
                string? name = TryReadComString(() => item.Name);
                string? path = TryReadComString(() => item.Path);
                string? appUserModelId = TryReadComString(
                    () => item.ExtendedProperty("System.AppUserModel.ID"));

                name = CleanPackagedDisplayName(name);

                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                AddShellDisplayName(result, path, name);
                AddShellDisplayName(result, appUserModelId, name);

                if (!string.IsNullOrWhiteSpace(path))
                {
                    AddShellDisplayName(
                        result,
                        $"shell:AppsFolder\\{path}",
                        name);
                }

                if (!string.IsNullOrWhiteSpace(appUserModelId))
                {
                    AddShellDisplayName(
                        result,
                        $"shell:AppsFolder\\{appUserModelId}",
                        name);
                }
            }
        }
        catch
        {
            // Ignore shell lookup failure.
        }

        return result;
    }

    private static void AddShellDisplayName(
        IDictionary<string, string> result,
        string? key,
        string displayName)
    {
        if (string.IsNullOrWhiteSpace(key)
            || string.IsNullOrWhiteSpace(displayName))
        {
            return;
        }

        result.TryAdd(key.Trim(), displayName.Trim());
    }

    private static string? TryReadComString(Func<object?> reader)
    {
        try
        {
            return reader()?.ToString();
        }
        catch
        {
            return null;
        }
    }

    private static IEnumerable<InstalledAppOption> EnumeratePackagedApps(
        IReadOnlyDictionary<string, string> shellDisplayNames)
    {
        PackageManager packageManager;

        try
        {
            packageManager = new PackageManager();
        }
        catch
        {
            yield break;
        }

        IEnumerable<Windows.ApplicationModel.Package> packages;

        try
        {
            packages = packageManager.FindPackagesForUser(string.Empty);
        }
        catch
        {
            yield break;
        }

        foreach (var package in packages)
        {
            string? installPath;
            string packageFamilyName;
            string packageName;
            string? packageDisplayName;

            try
            {
                installPath = package.InstalledLocation?.Path;
                packageFamilyName = package.Id.FamilyName;
                packageName = package.Id.Name;
                packageDisplayName = package.DisplayName;
            }
            catch
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(installPath))
            {
                continue;
            }

            var manifestPath = Path.Combine(installPath, "AppxManifest.xml");
            if (!File.Exists(manifestPath))
            {
                continue;
            }

            foreach (var app in ReadPackagedAppsFromManifest(
                         manifestPath,
                         installPath,
                         packageFamilyName,
                         packageName,
                         packageDisplayName,
                         shellDisplayNames))
            {
                yield return app;
            }
        }
    }

    private static IEnumerable<InstalledAppOption> ReadPackagedAppsFromManifest(
        string manifestPath,
        string installPath,
        string packageFamilyName,
        string packageName,
        string? packageDisplayName,
        IReadOnlyDictionary<string, string> shellDisplayNames)
    {
        XDocument manifest;

        try
        {
            manifest = XDocument.Load(manifestPath);
        }
        catch
        {
            yield break;
        }

        foreach (var app in manifest
                     .Descendants()
                     .Where(element => element.Name.LocalName == "Application"))
        {
            var appId = app.Attribute("Id")?.Value;

            if (string.IsNullOrWhiteSpace(appId)
                || IsBackgroundPackagedApp(appId))
            {
                continue;
            }

            var appUserModelId = $"{packageFamilyName}!{appId}";
            var shellLaunchPath = $"shell:AppsFolder\\{appUserModelId}";

            var visualElements = app.Elements()
                .FirstOrDefault(element => element.Name.LocalName == "VisualElements");

            var manifestDisplayName = visualElements?
                .Attribute("DisplayName")?
                .Value;

            var displayName = ResolvePackagedDisplayName(
                shellDisplayNames,
                appUserModelId,
                shellLaunchPath,
                packageDisplayName,
                manifestDisplayName,
                packageName,
                appId);

            if (string.IsNullOrWhiteSpace(displayName)
                || IsBackgroundPackagedApp(displayName))
            {
                continue;
            }

            var executable = app.Attribute("Executable")?.Value;
            var executablePath = BuildExistingPath(installPath, executable);

            var logoPath = FindPackagedLogoPath(
                installPath,
                visualElements?.Attribute("Square44x44Logo")?.Value);

            yield return new InstalledAppOption(
                displayName,
                shellLaunchPath,
                logoPath ?? executablePath);
        }
    }

    private static string ResolvePackagedDisplayName(
        IReadOnlyDictionary<string, string> shellDisplayNames,
        string appUserModelId,
        string shellLaunchPath,
        string? packageDisplayName,
        string? manifestDisplayName,
        string packageName,
        string appId)
    {
        if (shellDisplayNames.TryGetValue(appUserModelId, out var shellName)
            && !string.IsNullOrWhiteSpace(shellName))
        {
            return shellName.Trim();
        }

        if (shellDisplayNames.TryGetValue(shellLaunchPath, out shellName)
            && !string.IsNullOrWhiteSpace(shellName))
        {
            return shellName.Trim();
        }

        var cleanPackageDisplayName = CleanPackagedDisplayName(packageDisplayName);
        if (!string.IsNullOrWhiteSpace(cleanPackageDisplayName))
        {
            return cleanPackageDisplayName;
        }

        var cleanManifestDisplayName = CleanPackagedDisplayName(manifestDisplayName);
        if (!string.IsNullOrWhiteSpace(cleanManifestDisplayName))
        {
            return cleanManifestDisplayName;
        }

        if (!string.IsNullOrWhiteSpace(packageName))
        {
            return HumanizeIdentifier(packageName);
        }

        return HumanizeIdentifier(appId);
    }

    private static string? CleanPackagedDisplayName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        value = value.Trim();

        if (value.Equals("App", StringComparison.OrdinalIgnoreCase)
            || value.Equals("Application", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("ms-resource:", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("@{", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return value;
    }

    private static string HumanizeIdentifier(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        value = value
            .Replace('.', ' ')
            .Replace('_', ' ')
            .Replace('-', ' ')
            .Trim();

        var builder = new StringBuilder();

        for (var i = 0; i < value.Length; i++)
        {
            var current = value[i];

            if (i > 0
                && char.IsUpper(current)
                && !char.IsWhiteSpace(value[i - 1])
                && !char.IsUpper(value[i - 1]))
            {
                builder.Append(' ');
            }

            builder.Append(current);
        }

        return builder.ToString().Trim();
    }

    private static bool IsBackgroundPackagedApp(string value)
    {
        return value.Contains("update", StringComparison.OrdinalIgnoreCase)
            || value.Contains("autostart", StringComparison.OrdinalIgnoreCase)
            || value.Contains("remote module", StringComparison.OrdinalIgnoreCase)
            || value.Contains("background", StringComparison.OrdinalIgnoreCase);
    }

    private static string? BuildExistingPath(string rootPath, string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        var path = Path.IsPathRooted(relativePath)
            ? relativePath
            : Path.Combine(rootPath, relativePath);

        return File.Exists(path) ? path : null;
    }

    private static string? FindPackagedLogoPath(
        string rootPath,
        string? logoPath)
    {
        var exactPath = BuildExistingPath(rootPath, logoPath);
        if (exactPath is not null)
        {
            return exactPath;
        }

        if (string.IsNullOrWhiteSpace(logoPath))
        {
            return null;
        }

        var directory = Path.GetDirectoryName(
            Path.Combine(rootPath, logoPath));

        var name = Path.GetFileNameWithoutExtension(logoPath);

        if (string.IsNullOrWhiteSpace(directory)
            || string.IsNullOrWhiteSpace(name)
            || !Directory.Exists(directory))
        {
            return null;
        }

        return Directory.EnumerateFiles(
                directory,
                $"{name}*.png",
                SearchOption.TopDirectoryOnly)
            .OrderByDescending(path =>
                path.Contains("targetsize-48", StringComparison.OrdinalIgnoreCase))
            .ThenBy(path => path.Length)
            .FirstOrDefault();
    }

    private static string? ResolveIconDataUrl(
        string targetPath,
        string? iconSourcePath)
    {
        try
        {
            var iconPath = new[] { iconSourcePath, targetPath }
                .Select(ResolveIconPath)
                .FirstOrDefault(path =>
                    !string.IsNullOrWhiteSpace(path)
                    && File.Exists(path));

            if (string.IsNullOrWhiteSpace(iconPath))
            {
                return null;
            }

            if (IsImageFile(iconPath))
            {
                var extension = Path.GetExtension(iconPath).ToLowerInvariant();

                var contentType = extension == ".jpg" || extension == ".jpeg"
                    ? "image/jpeg"
                    : "image/png";

                return $"data:{contentType};base64,{Convert.ToBase64String(File.ReadAllBytes(iconPath))}";
            }

            using var icon = System.Drawing.Icon.ExtractAssociatedIcon(iconPath);
            if (icon is null)
            {
                return null;
            }

            using var bitmap = icon.ToBitmap();
            using var stream = new MemoryStream();

            bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);

            return $"data:image/png;base64,{Convert.ToBase64String(stream.ToArray())}";
        }
        catch
        {
            return null;
        }
    }

    private static string? ResolveIconPath(string? iconLocation)
    {
        if (string.IsNullOrWhiteSpace(iconLocation))
        {
            return null;
        }

        var iconPath = Environment
            .ExpandEnvironmentVariables(iconLocation.Trim().Trim('"'));

        var commaIndex = iconPath.LastIndexOf(',');
        if (commaIndex > 1)
        {
            iconPath = iconPath[..commaIndex].Trim().Trim('"');
        }

        return iconPath;
    }

    private static bool IsImageFile(string path)
    {
        var extension = Path.GetExtension(path);

        return extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record ShortcutApp(
        string LaunchPath,
        string? IconSourcePath);
#endif
}