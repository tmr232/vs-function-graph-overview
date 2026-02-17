using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;
using Microsoft.Web.WebView2.Core;

namespace FunctionGraphOverview
{
    /// <summary>
    /// Interaction logic for FunctionGraphToolWindowControl.
    /// </summary>
    public partial class FunctionGraphToolWindowControl : UserControl, IDisposable
    {
        private WebviewBridge _bridge;
        private EditorMonitor _editorMonitor;
        private ThemeMonitor _themeMonitor;
        private SettingsMonitor _settingsMonitor;
        private bool _disposed;

        internal WebviewBridge Bridge => _bridge;

        /// <summary>
        /// Initializes a new instance of the <see cref="FunctionGraphToolWindowControl"/> class.
        /// </summary>
        public FunctionGraphToolWindowControl()
        {
            this.InitializeComponent();
            InitializeWebViewAsync();
        }

        private async void InitializeWebViewAsync()
        {
            try
            {
                var userDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "FunctionGraphOverview"
                );
                var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                await webView.EnsureCoreWebView2Async(env);

                var extensionDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var webviewAssetsPath = Path.Combine(extensionDir, "WebviewAssets");

                webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "functiongraph.local",
                    webviewAssetsPath,
                    CoreWebView2HostResourceAccessKind.Allow
                );

                webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

                var navigationTcs = new TaskCompletionSource<bool>();
                void OnNavigationCompleted(object s, CoreWebView2NavigationCompletedEventArgs args)
                {
                    webView.CoreWebView2.NavigationCompleted -= OnNavigationCompleted;
                    navigationTcs.TrySetResult(args.IsSuccess);
                }
                webView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;

                webView.CoreWebView2.Navigate("https://functiongraph.local/index.html");

                await navigationTcs.Task;

                _bridge = new WebviewBridge(webView);
                _editorMonitor = new EditorMonitor(_bridge);
                _themeMonitor = new ThemeMonitor(_bridge);
                _settingsMonitor = new SettingsMonitor(_bridge, _themeMonitor);
            }
            catch (WebView2RuntimeNotFoundException)
            {
                MessageBox.Show(
                    "The WebView2 Runtime is required. Please download it from:\nhttps://developer.microsoft.com/microsoft-edge/webview2/",
                    "WebView2 Runtime Missing",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void CoreWebView2_WebMessageReceived(
            object sender,
            CoreWebView2WebMessageReceivedEventArgs e
        )
        {
            try
            {
                var messageString = e.TryGetWebMessageAsString();
                using (var doc = JsonDocument.Parse(messageString))
                {
                    var root = doc.RootElement;
                    if (
                        root.TryGetProperty("tag", out var tagProp)
                        && tagProp.GetString() == "navigateTo"
                        && root.TryGetProperty("offset", out var offsetProp)
                    )
                    {
                        int offset = offsetProp.GetInt32();
                        ThreadHelper.ThrowIfNotOnUIThread();
                        NavigationService.NavigateToByteOffset(offset);
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Log(ex);
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            _settingsMonitor?.Dispose();
            _editorMonitor?.Dispose();
            _themeMonitor?.Dispose();
            webView?.Dispose();
        }
    }
}
