using System;
using Microsoft.VisualStudio.Shell;

namespace FunctionGraphOverview
{
    internal sealed class SettingsMonitor : IDisposable
    {
        private readonly WebviewBridge _bridge;
        private readonly FunctionGraphOptions _options;
        private readonly ThemeMonitor _themeMonitor;
        private bool _disposed;

        public SettingsMonitor(WebviewBridge bridge, ThemeMonitor themeMonitor)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _bridge = bridge;
            _themeMonitor = themeMonitor;

            _options = FunctionGraphOverviewPackage.Instance?.Options;

            if (_options != null)
            {
                _options.SettingsChanged += OnSettingsChanged;
            }

            SendSettings();
        }

        private void OnSettingsChanged(object sender, EventArgs e)
        {
            SendSettings();
            _themeMonitor?.SendColors();
        }

        private void SendSettings()
        {
            if (_options == null)
                return;

            _bridge
                .SendSettingsAsync(
                    _options.Simplify,
                    _options.FlatSwitch,
                    _options.HighlightCurrentNode
                )
                .FireAndForget();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            if (_options != null)
            {
                _options.SettingsChanged -= OnSettingsChanged;
            }
        }
    }
}
