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

        private string getColorsJson()
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
                        return customJson;
                    }
                    // Fall back to system if custom JSON is empty.
                    goto default;
                default: // System
                    colors = IsDarkTheme()
                        ? ColorSchemeDefinitions.GetDarkScheme()
                        : ColorSchemeDefinitions.GetLightScheme();
                    break;
            }

            var envelope = new { version = 1, scheme = colors };
            var json = JsonSerializer.Serialize(envelope);
            return json;
        }

        public void SendColors()
        {
            var json = getColorsJson();
            _bridge
                .SendColorsAsync(
                    json,
                    IsDarkTheme(),
                    getHtmlColor(EnvironmentColors.ToolWindowBackgroundColorKey),
                    getHtmlColor(EnvironmentColors.ToolWindowTextColorKey)
                )
                .FireAndForget();
        }

        internal static string getHtmlColor(ThemeResourceKey themeResourceKey)
        {
            return System.Drawing.ColorTranslator.ToHtml(
                VSColorTheme.GetThemedColor(themeResourceKey)
            );
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
