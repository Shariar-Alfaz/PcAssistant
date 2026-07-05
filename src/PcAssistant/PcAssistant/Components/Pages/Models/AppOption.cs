using System;
using System.Collections.Generic;
using System.Text;

namespace PcAssistant.Components.Pages.Models
{
    public sealed class AppOption(string key, string displayName, string defaultPath, string? iconSourcePath = null, bool isCustom = false)
    {
        public string Key { get; } = key;
        public string DisplayName { get; } = displayName;
        public string DefaultPath { get; } = defaultPath;
        public string? IconSourcePath { get; } = iconSourcePath;
        public string? IconDataUrl { get; set; }
        public bool IsCustom { get; } = isCustom || string.IsNullOrWhiteSpace(defaultPath);
    }
}
