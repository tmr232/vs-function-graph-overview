using System.Collections.Generic;

namespace FunctionGraphOverview
{
    public enum ColorSchemeMode
    {
        System,
        Dark,
        Light,
        Custom,
    }

    internal static class ColorSchemeDefinitions
    {
        // Based on https://github.com/tmr232/function-graph-overview/blob/main/src/control-flow/colors.ts
        // Background colors replaced with VS tool window defaults.

        public static List<ColorEntry> GetLightScheme()
        {
            return new List<ColorEntry>
            {
                new ColorEntry("node.default", "#d3d3d3"),
                new ColorEntry("node.entry", "#48AB30"),
                new ColorEntry("node.exit", "#AB3030"),
                new ColorEntry("node.throw", "#ffdddd"),
                new ColorEntry("node.yield", "#00bfff"),
                new ColorEntry("node.terminate", "#7256c6"),
                new ColorEntry("node.border", "#000000"),
                new ColorEntry("node.highlight", "#000000"),
                new ColorEntry("edge.regular", "#0000ff"),
                new ColorEntry("edge.consequence", "#008000"),
                new ColorEntry("edge.alternative", "#ff0000"),
                new ColorEntry("cluster.border", "#ffffff"),
                new ColorEntry("cluster.with", "#ffddff"),
                new ColorEntry("cluster.tryComplex", "#ddddff"),
                new ColorEntry("cluster.try", "#ddffdd"),
                new ColorEntry("cluster.finally", "#ffffdd"),
                new ColorEntry("cluster.except", "#ffdddd"),
                new ColorEntry("graph.background", "#F5F5F5"),
            };
        }

        public static List<ColorEntry> GetDarkScheme()
        {
            return new List<ColorEntry>
            {
                new ColorEntry("node.default", "#707070"),
                new ColorEntry("node.entry", "#48AB30"),
                new ColorEntry("node.exit", "#AB3030"),
                new ColorEntry("node.throw", "#590c0c"),
                new ColorEntry("node.yield", "#0a9aca"),
                new ColorEntry("node.terminate", "#7256c6"),
                new ColorEntry("node.border", "#000000"),
                new ColorEntry("node.highlight", "#dddddd"),
                new ColorEntry("edge.regular", "#2592a1"),
                new ColorEntry("edge.consequence", "#4ce34c"),
                new ColorEntry("edge.alternative", "#ff3e3e"),
                new ColorEntry("cluster.border", "#302e2e"),
                new ColorEntry("cluster.with", "#7d007d"),
                new ColorEntry("cluster.tryComplex", "#344c74"),
                new ColorEntry("cluster.try", "#1b5f1b"),
                new ColorEntry("cluster.finally", "#999918"),
                new ColorEntry("cluster.except", "#590c0c"),
                new ColorEntry("graph.background", "#1F1F1F"),
            };
        }
    }

    internal class ColorEntry
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
