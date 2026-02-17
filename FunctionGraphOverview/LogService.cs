using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace FunctionGraphOverview
{
    internal static class LogService
    {
        private static readonly Guid PaneGuid = new Guid("f0e3a5d7-8b2c-4a1e-9d6f-3c7b5e8a0f12");

        private static IVsOutputWindowPane _pane;

        public static void Log(string message)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var pane = GetPane();
            pane?.OutputStringThreadSafe(message + Environment.NewLine);
        }

        public static void Log(Exception ex)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Log($"[Error] {ex}");
        }

        private static IVsOutputWindowPane GetPane()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_pane != null)
                return _pane;

            var outputWindow = (IVsOutputWindow)Package.GetGlobalService(typeof(SVsOutputWindow));
            if (outputWindow == null)
                return null;

            var guid = PaneGuid;
            outputWindow.CreatePane(ref guid, "Function Graph Overview", 1, 1);
            outputWindow.GetPane(ref guid, out _pane);
            return _pane;
        }
    }
}
