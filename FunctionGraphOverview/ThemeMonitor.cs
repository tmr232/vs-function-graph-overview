using System;
using System.Collections.Generic;
using System.Drawing;
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

        private void SendColors()
        {
            var colors = ExtractColors();
            var envelope = new { version = 1, scheme = colors };
            var json = JsonSerializer.Serialize(envelope);
            _ = _bridge.SendColorsAsync(json);
        }

        private static List<ColorEntry> ExtractColors()
        {
            bool isDark = IsDarkTheme();

            var bg = GetColor(EnvironmentColors.ToolWindowBackgroundColorKey);
            var fg = GetColor(EnvironmentColors.ToolWindowTextColorKey);
            var panelBg = GetColor(EnvironmentColors.CommandBarGradientBeginColorKey);
            var highlight = GetColor(EnvironmentColors.SystemHighlightColorKey);
            var highlightText = GetColor(EnvironmentColors.SystemHighlightTextColorKey);

            return new List<ColorEntry>
            {
                new ColorEntry("graph.background", ToHex(bg)),
                new ColorEntry("node.default.background", ToHex(isDark ? Lighten(bg, 0.1) : Darken(bg, 0.05))),
                new ColorEntry("node.default.border", ToHex(isDark ? Lighten(bg, 0.25) : Darken(bg, 0.2))),
                new ColorEntry("node.highlight.background", ToHex(highlight)),
                new ColorEntry("node.highlight.border", ToHex(isDark ? Lighten(highlight, 0.15) : Darken(highlight, 0.15))),
                new ColorEntry("edge.regular", ToHex(fg)),
                new ColorEntry("edge.consequence", ToHex(isDark ? Color.FromArgb(0x4E, 0xC9, 0xB0) : Color.FromArgb(0x00, 0x80, 0x00))),
                new ColorEntry("edge.alternative", ToHex(isDark ? Color.FromArgb(0xD7, 0xBA, 0x7D) : Color.FromArgb(0xA3, 0x15, 0x15))),
                new ColorEntry("edge.exception", ToHex(isDark ? Color.FromArgb(0xF4, 0x48, 0x47) : Color.FromArgb(0xCC, 0x00, 0x00))),
                new ColorEntry("text.default", ToHex(fg)),
            };
        }

        private static bool IsDarkTheme()
        {
            var bg = GetColor(EnvironmentColors.ToolWindowBackgroundColorKey);
            return (bg.R + bg.G + bg.B) / 3.0 < 128;
        }

        private static Color GetColor(ThemeResourceKey key)
        {
            return VSColorTheme.GetThemedColor(key);
        }

        private static string ToHex(Color c)
        {
            return $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        }

        private static Color Lighten(Color c, double amount)
        {
            int r = Math.Min(255, (int)(c.R + (255 - c.R) * amount));
            int g = Math.Min(255, (int)(c.G + (255 - c.G) * amount));
            int b = Math.Min(255, (int)(c.B + (255 - c.B) * amount));
            return Color.FromArgb(r, g, b);
        }

        private static Color Darken(Color c, double amount)
        {
            int r = Math.Max(0, (int)(c.R * (1 - amount)));
            int g = Math.Max(0, (int)(c.G * (1 - amount)));
            int b = Math.Max(0, (int)(c.B * (1 - amount)));
            return Color.FromArgb(r, g, b);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            VSColorTheme.ThemeChanged -= OnThemeChanged;
        }

        private class ColorEntry
        {
            public string name { get; set; }
            public string hex { get; set; }

            public ColorEntry(string name, string hex)
            {
                this.name = name;
                this.hex = hex;
            }
        }
    }
}
