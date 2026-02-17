using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;

namespace FunctionGraphOverview
{
    internal sealed class ThemeMonitor : IDisposable
    {
        private readonly WebviewBridge _bridge;
        private bool _disposed;

        public ThemeMonitor(WebviewBridge bridge)
        {
            _bridge = bridge;
            VSColorTheme.ThemeChanged += OnThemeChanged;
            SendColors();
        }

        private void OnThemeChanged(ThemeChangedEventArgs e)
        {
            SendColors();
        }

        public void SendColors()
        {
            var options = FunctionGraphOverviewPackage.Instance?.Options;
            var mode = options?.ColorSchemeMode ?? ColorSchemeMode.System;

            List<ColorEntry> colors;
            switch (mode)
            {
                case ColorSchemeMode.Dark:
                    colors = ColorSchemeDefinitions.GetDarkScheme();
                    break;
                case ColorSchemeMode.Light:
                    colors = ColorSchemeDefinitions.GetLightScheme();
                    break;
                case ColorSchemeMode.Custom:
                    var customJson = options?.CustomColorSchemeJson;
                    if (!string.IsNullOrWhiteSpace(customJson))
                    {
                        _bridge.SendColorsAsync(customJson).FireAndForget();
                        return;
                    }
                    // Fall back to system if custom JSON is empty.
                    colors = IsDarkTheme()
                        ? ColorSchemeDefinitions.GetDarkScheme()
                        : ColorSchemeDefinitions.GetLightScheme();
                    break;
                default: // System
                    colors = IsDarkTheme()
                        ? ColorSchemeDefinitions.GetDarkScheme()
                        : ColorSchemeDefinitions.GetLightScheme();
                    break;
            }

            var envelope = new { version = 1, scheme = colors };
            var json = JsonSerializer.Serialize(envelope);
            _bridge.SendColorsAsync(json).FireAndForget();
        }

        internal static bool IsDarkTheme()
        {
            var bg = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey);
            return (bg.R + bg.G + bg.B) / 3.0 < 128;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            VSColorTheme.ThemeChanged -= OnThemeChanged;
        }
    }
}
