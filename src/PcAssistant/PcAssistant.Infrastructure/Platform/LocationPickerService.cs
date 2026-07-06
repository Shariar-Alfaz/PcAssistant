using PcAssistant.Application.Abstractions.Services;
using PcAssistant.Application.Models;


#if WINDOWS
using Microsoft.Maui.Controls;
using Windows.Storage.Pickers;
using WinRT.Interop;
#endif

namespace PcAssistant.Infrastructure.Platform;

public sealed class LocationPickerService : ILocationPickerService
{
    public async Task<DirectorySuggestion?> PickDirectoryAsync(CancellationToken cancellationToken = default)
    {
#if WINDOWS
        var window = global::Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView;
        if (window is null)
        {
            return null;
        }

        var picker = new FolderPicker();
        picker.FileTypeFilter.Add("*");
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(window));

        var folder = await picker.PickSingleFolderAsync();
        if (folder is null || cancellationToken.IsCancellationRequested)
        {
            return null;
        }

        return new DirectorySuggestion(folder.Path, folder.Name, "folder");
#else
        await Task.CompletedTask;
        return null;
#endif
    }

    public async Task<IReadOnlyList<string>> PickFilesAsync(bool allowMultiple = true, CancellationToken cancellationToken = default)
    {
#if WINDOWS
        var window = global::Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView;
        if (window is null)
        {
            return Array.Empty<string>();
        }

        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add("*");
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(window));

        if (allowMultiple)
        {
            var files = await picker.PickMultipleFilesAsync();
            if (cancellationToken.IsCancellationRequested)
            {
                return Array.Empty<string>();
            }

            return files.Select(file => file.Path).Where(path => !string.IsNullOrWhiteSpace(path)).ToArray();
        }

        var file = await picker.PickSingleFileAsync();
        if (file is null || cancellationToken.IsCancellationRequested)
        {
            return Array.Empty<string>();
        }

        return new[] { file.Path };
#else
        await Task.CompletedTask;
        return Array.Empty<string>();
#endif
    }
}
