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

        protected override void OnApply(PageApplyEventArgs e)
        {
            base.OnApply(e);
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
