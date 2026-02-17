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
            var highlight = GetColor(EnvironmentColors.SystemHighlightColorKey);

            var nodeDefault = isDark ? Lighten(bg, 0.1) : Darken(bg, 0.05);
            var nodeBorder = isDark ? Lighten(bg, 0.25) : Darken(bg, 0.2);
            var green = isDark
                ? Color.FromArgb(0x4E, 0xC9, 0xB0)
                : Color.FromArgb(0x00, 0x80, 0x00);
            var red = isDark ? Color.FromArgb(0xD7, 0xBA, 0x7D) : Color.FromArgb(0xA3, 0x15, 0x15);
            var darkRed = isDark
                ? Color.FromArgb(0xF4, 0x48, 0x47)
                : Color.FromArgb(0xCC, 0x00, 0x00);
            var blue = isDark ? Color.FromArgb(0x0A, 0x9A, 0xCA) : Color.FromArgb(0x0A, 0x9A, 0xCA);
            var clusterBorder = isDark ? Lighten(bg, 0.15) : Darken(bg, 0.15);

            return new List<ColorEntry>
            {
                new ColorEntry("node.default", ToHex(nodeDefault)),
                new ColorEntry("node.entry", ToHex(green)),
                new ColorEntry("node.exit", ToHex(red)),
                new ColorEntry("node.throw", ToHex(darkRed)),
                new ColorEntry("node.yield", ToHex(blue)),
                new ColorEntry("node.border", ToHex(nodeBorder)),
                new ColorEntry("node.highlight", ToHex(highlight)),
                new ColorEntry("edge.regular", ToHex(fg)),
                new ColorEntry("edge.consequence", ToHex(green)),
                new ColorEntry("edge.alternative", ToHex(red)),
                new ColorEntry("cluster.border", ToHex(clusterBorder)),
                new ColorEntry(
                    "cluster.with",
                    ToHex(
                        isDark ? Color.FromArgb(0x7D, 0x00, 0x7D) : Color.FromArgb(0x7D, 0x00, 0x7D)
                    )
                ),
                new ColorEntry(
                    "cluster.tryComplex",
                    ToHex(
                        isDark ? Color.FromArgb(0x34, 0x4C, 0x74) : Color.FromArgb(0x34, 0x4C, 0x74)
                    )
                ),
                new ColorEntry(
                    "cluster.try",
                    ToHex(
                        isDark ? Color.FromArgb(0x1B, 0x5F, 0x1B) : Color.FromArgb(0x1B, 0x5F, 0x1B)
                    )
                ),
                new ColorEntry(
                    "cluster.finally",
                    ToHex(
                        isDark ? Color.FromArgb(0x99, 0x99, 0x18) : Color.FromArgb(0x99, 0x99, 0x18)
                    )
                ),
                new ColorEntry(
                    "cluster.except",
                    ToHex(
                        isDark ? Color.FromArgb(0x59, 0x0C, 0x0C) : Color.FromArgb(0x59, 0x0C, 0x0C)
                    )
                ),
                new ColorEntry("graph.background", ToHex(bg)),
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
