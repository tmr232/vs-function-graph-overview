using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Wpf;

namespace FunctionGraphOverview
{
    /// <summary>
    /// Sends messages to the webview SPA by calling JS functions via ExecuteScriptAsync.
    /// </summary>
    internal class WebviewBridge
    {
        private readonly WebView2 _webView;

        public WebviewBridge(WebView2 webView)
        {
            _webView = webView;
        }

        public async Task SendCodeAsync(string code, int offset, string language)
        {
            var jsCode = JsonSerializer.Serialize(code);
            var jsLang = JsonSerializer.Serialize(language);
            await _webView.ExecuteScriptAsync(
                $"window.VisualStudio?.ToWebview?.setCode({jsCode}, {offset}, {jsLang})");
        }

        public async Task SendSettingsAsync(bool simplify, bool flatSwitch, bool highlight)
        {
            await _webView.ExecuteScriptAsync(
                $"window.VisualStudio?.ToWebview?.setSimplify({BoolToJs(simplify)})");
            await _webView.ExecuteScriptAsync(
                $"window.VisualStudio?.ToWebview?.setFlatSwitch({BoolToJs(flatSwitch)})");
            await _webView.ExecuteScriptAsync(
                $"window.VisualStudio?.ToWebview?.setHighlight({BoolToJs(highlight)})");
        }

        public async Task SendColorsAsync(string colorsJson)
        {
            var json = JsonSerializer.Serialize(colorsJson);
            await _webView.ExecuteScriptAsync(
                $"window.VisualStudio?.ToWebview?.setColors({json})");
        }

        private static string BoolToJs(bool v) => v ? "true" : "false";
    }
}
