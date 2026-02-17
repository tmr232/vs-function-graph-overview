using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace FunctionGraphOverview
{
    [Guid("a1f3c4e7-2b8d-4f6a-9e5c-7d1b0a3f8e42")]
    public class FunctionGraphOptions : DialogPage
    {
        public event EventHandler SettingsChanged;

        [Category("Function Graph Overview")]
        [DisplayName("Simplify")]
        [Description("Simplify the CFG by merging linear chains")]
        public bool Simplify { get; set; } = true;

        [Category("Function Graph Overview")]
        [DisplayName("Flat Switch")]
        [Description("Flatten switch/case structures")]
        public bool FlatSwitch { get; set; } = true;

        [Category("Function Graph Overview")]
        [DisplayName("Highlight Current Node")]
        [Description("Highlight the CFG node at cursor position")]
        public bool HighlightCurrentNode { get; set; } = true;

        [Category("Function Graph Overview")]
        [DisplayName("Color Scheme")]
        [Description("Dark, Light, System (follows IDE theme), or Custom (paste JSON)")]
        [TypeConverter(typeof(EnumConverter))]
        public ColorSchemeMode ColorSchemeMode { get; set; } = ColorSchemeMode.System;

        [Category("Function Graph Overview")]
        [DisplayName("Custom Color Scheme JSON")]
        [Description(
            "Paste a color scheme JSON string (used when Color Scheme is Custom). Format: {\"version\":1,\"scheme\":[{\"name\":\"...\",\"hex\":\"#...\"},...]}"
        )]
        public string CustomColorSchemeJson { get; set; } = "";

        protected override void OnApply(PageApplyEventArgs e)
        {
            base.OnApply(e);
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
