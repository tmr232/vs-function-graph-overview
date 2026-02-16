# Implementation Plan — VS Extension for Function Graph Overview

> Converting the existing VSCode/JetBrains function-graph-overview webview SPA
> into a Visual Studio 2022 VSIX extension.

---

## Architecture Overview

```
┌─────────────────────────────────────────────────┐
│              Visual Studio 2022                 │
│                                                 │
│  ┌─────────────────────────────────────────┐    │
│  │   FunctionGraphOverviewPackage (C#)     │    │
│  │                                         │    │
│  │  ┌─────────────────┐  ┌──────────────┐  │    │
│  │  │ EditorMonitor   │  │ ThemeMonitor │  │    │
│  │  │ (cursor/text    │  │ (color       │  │    │
│  │  │  events)        │  │  extraction) │  │    │
│  │  └────────┬────────┘  └──────┬───────┘  │    │
│  │           │                  │           │    │
│  │           ▼                  ▼           │    │
│  │  ┌──────────────────────────────────┐   │    │
│  │  │     MessageBridge (C#↔JS)       │   │    │
│  │  │  ExecuteScriptAsync / AddHost   │   │    │
│  │  └──────────┬───────────────────────┘   │    │
│  │             │                           │    │
│  │  ┌──────────▼───────────────────────┐   │    │
│  │  │     WebView2 Control (WPF)      │   │    │
│  │  │  ┌───────────────────────────┐  │   │    │
│  │  │  │  Webview SPA (Svelte)    │  │   │    │
│  │  │  │  Tree-Sitter → CFG →     │  │   │    │
│  │  │  │  Graphviz → SVG          │  │   │    │
│  │  │  └───────────────────────────┘  │   │    │
│  │  └──────────────────────────────────┘   │    │
│  └─────────────────────────────────────────┘    │
└─────────────────────────────────────────────────┘
```

The extension follows the same **host ↔ webview split** as the VSCode and
JetBrains versions: the C# host monitors IDE events and sends lightweight
messages; the webview SPA (reused from the original project) owns all heavy
computation.

---

## Existing State

The project already has:
- A working VSIX project targeting VS 2022 (17.x)
- WebView2 NuGet package referenced
- A tool window (`FunctionGraphToolWindow`) with WebView2 control
- WebView2 initialization with user-data-folder fix
- Currently navigates to the hosted web app at `tmr232.github.io`

---

## Communication Strategy: WebView2 ↔ C#

WebView2 provides two mechanisms for host↔web communication:

| Direction | Mechanism |
|---|---|
| **C# → JS** | `CoreWebView2.ExecuteScriptAsync(js)` — call JS functions directly |
| **JS → C#** | `CoreWebView2.WebMessageReceived` event + `window.chrome.webview.postMessage()` |

This matches the **JetBrains pattern** (direct function calls + callback injection)
rather than the VSCode pattern (bidirectional `postMessage`). The webview SPA
already supports both patterns via its `initVSCode()` and `initJetBrains()`
functions.

### Webview Adaptation

The SPA's `App.svelte` already has platform-specific init functions. We will add
an `initVisualStudio(stateHandler)` function following the JetBrains pattern:

**C# → Webview (host calls into JS):**
```js
// Injected on window.VisualStudio.ToWebview
window.VisualStudio.ToWebview.setCode(code, offset, language)
window.VisualStudio.ToWebview.setColors(colorsJson)
window.VisualStudio.ToWebview.setSimplify(flag)
window.VisualStudio.ToWebview.setFlatSwitch(flag)
window.VisualStudio.ToWebview.setHighlight(flag)
```

The C# host calls these via `ExecuteScriptAsync()`.

**Webview → C# (JS calls back to host):**
```js
// navigateTo sends a message the C# host receives
window.chrome.webview.postMessage({ tag: "navigateTo", offset: 42 });
```

The C# host handles this via the `WebMessageReceived` event.

---

## Implementation Steps

Each step is an independently committable unit. Steps are ordered by
dependency — later steps may depend on earlier ones.

---

### Step 1: Clean Up Scaffold & Rename

**Goal:** Remove placeholder UI elements and rename the tool window to reflect
its actual purpose.

**Files to modify:**
- `FunctionGraphToolWindowControl.xaml` — Remove the `TextBlock` and `Button`; make the
  `WebView2` fill the entire window (`HorizontalAlignment="Stretch"`,
  `VerticalAlignment="Stretch"`, remove fixed size)
- `FunctionGraphToolWindowControl.xaml.cs` — Remove `button1_Click` handler
- `FunctionGraphToolWindow.cs` — Change `Caption` from `"FunctionGraphToolWindow"` to
  `"Function Graph Overview"`
- `FunctionGraphOverviewPackage.vsct` — Change `ButtonText` from
  `"FunctionGraphToolWindow"` to `"Function Graph Overview"`
- `source.extension.vsixmanifest` — Update `Description` from
  `"Empty VSIX Project."` to something meaningful

**Verification:** Build the extension, launch in Exp instance, open the tool
window from View → Other Windows. It should show an empty WebView2 filling the
entire pane with the title "Function Graph Overview".

---

### Step 2: Bundle Webview Assets Locally

**Goal:** Load the webview SPA from local files bundled in the VSIX instead of
navigating to a remote URL.

**Tasks:**
1. Create a `WebviewAssets/` directory in the project.
2. Add a build step or manual process to copy the pre-built SPA files
   (`index.js`, `index.css`, and any `.wasm` files) into `WebviewAssets/`.
3. Include these files as `Content` in the `.csproj` with
   `CopyToOutputDirectory` and as VSIX content.
4. In `FunctionGraphToolWindowControl.xaml.cs`, replace the remote URL navigation with
   `CoreWebView2.SetVirtualHostNameToFolderMapping()` to map a virtual
   hostname (e.g., `functiongraph.local`) to the `WebviewAssets/` folder.
5. Generate the host HTML page (similar to the VSCode extension's
   `overview-view.ts` HTML template) that references the local JS/CSS and
   provides the `<div id="app">` mount point.
6. Navigate to `https://functiongraph.local/index.html`.

**Key consideration:** The webview SPA needs `wasm-unsafe-eval` to run
Tree-Sitter and Graphviz WASM. WebView2 does not enforce CSP by default (the
page itself sets CSP), so the generated HTML should either include a permissive
CSP or omit it entirely since WebView2 is not a sandboxed origin like VSCode's
webview.

**Verification:** The tool window should load the SPA and display the default
empty/demo graph (the "Hello → World" graph shown when no code is provided).

---

### Step 3: Add `initVisualStudio` Bridge to Webview SPA

**Goal:** Add a Visual Studio–specific initialization path to the webview SPA
so it can receive messages from the C# host.

**Files to modify (in the function-graph-overview webview project):**

1. **`App.svelte`** — Add `initVisualStudio(stateHandler)` function:
   ```typescript
   function initVisualStudio(stateHandler: StateHandler): void {
     // Expose callable functions on a global namespace
     window.VisualStudio ??= {};
     window.VisualStudio.ToWebview = {
       setCode: (code: string, offset: number, language: string) => {
         stateHandler.update({ code, offset, language });
       },
       setSimplify: (flag: boolean) =>
         stateHandler.update({ config: { simplify: flag } }),
       setFlatSwitch: (flag: boolean) =>
         stateHandler.update({ config: { flatSwitch: flag } }),
       setHighlight: (flag: boolean) =>
         stateHandler.update({ config: { highlight: flag } }),
       setColors: (colors: string) => {
         try {
           colorList = deserializeColorList(colors);
           document.body.style.backgroundColor = colorList.find(
             ({ name }) => name === "graph.background",
           ).hex;
         } catch (error) {
           console.trace(error);
         }
       },
     };

     // navigateTo → postMessage back to C# host
     stateHandler.onNavigateTo((offset: number) => {
       window.chrome?.webview?.postMessage(
         JSON.stringify({ tag: "navigateTo", offset })
       );
     });
   }
   ```

2. **Add TypeScript declarations** for the `window.VisualStudio` and
   `window.chrome.webview` interfaces.

3. **Call `initVisualStudio(stateHandler)`** alongside the existing
   `initVSCode` and `initJetBrains` calls.

4. **Rebuild the SPA** and update the bundled assets in `WebviewAssets/`.

**Verification:** Open browser dev tools in the WebView2 (enable with
`CoreWebView2Settings.AreDevToolsEnabled = true` temporarily). Confirm that
`window.VisualStudio.ToWebview` exists. Manually call
`window.VisualStudio.ToWebview.setCode("function foo() { if (true) {} }", 0, "javascript")`
in the console and verify a graph renders.

---

### Step 4: Implement C# → Webview Message Bridge

**Goal:** Build the C# side of the message bridge that calls into the webview's
`window.VisualStudio.ToWebview.*` functions.

**New file:** `WebviewBridge.cs`

```csharp
using System.Text.Json;
using Microsoft.Web.WebView2.Wpf;

namespace FunctionGraphOverview
{
    /// <summary>
    /// Sends messages to the webview SPA by calling JS functions via
    /// ExecuteScriptAsync.
    /// </summary>
    internal class WebviewBridge
    {
        private readonly WebView2 _webView;

        public WebviewBridge(WebView2 webView) { _webView = webView; }

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

        public async Task SendColorsAsync(IEnumerable<(string name, string hex)> colors)
        {
            var json = JsonSerializer.Serialize(
                colors.Select(c => new { name = c.name, hex = c.hex }));
            await _webView.ExecuteScriptAsync(
                $"window.VisualStudio?.ToWebview?.setColors({json})");
        }

        private static string BoolToJs(bool v) => v ? "true" : "false";
    }
}
```

**Files to modify:**
- `FunctionGraphToolWindowControl.xaml.cs` — Create `WebviewBridge` after WebView2
  initialization completes. Store it as a field for use by the editor monitor.

**Verification:** After the webview loads, call `SendCodeAsync` with a hardcoded
snippet. Confirm the graph renders.

---

### Step 5: Handle Webview → C# Messages (`navigateTo`)

**Goal:** Receive `navigateTo` messages from the webview and move the VS editor
cursor to the specified byte offset.

**Tasks:**
1. In `FunctionGraphToolWindowControl.xaml.cs`, subscribe to
   `CoreWebView2.WebMessageReceived` after initialization.
2. Parse the incoming JSON message, extract `tag` and `offset`.
3. When `tag == "navigateTo"`:
   a. Get the active `IVsTextView` via `IVsTextManager`.
   b. Get the `IVsTextLines` buffer from the view.
   c. Convert the byte offset to a line/column position using
      `IVsTextLines.GetLineIndexOfPosition()` (note: VS uses character offsets,
      not byte offsets — a conversion may be needed for multi-byte characters).
   d. Call `IVsTextView.SetCaretPos(line, column)` and
      `IVsTextView.EnsureVisible(line, column)`.

**Key concern — byte offset vs character offset:**
The webview operates in byte offsets (Tree-Sitter convention). VS text buffers
use character (UTF-16) offsets. For ASCII-only files these are identical; for
files with multi-byte characters, conversion is needed. The simplest approach:
read the file text, encode to UTF-8 bytes, and map byte offset → char index.
This can be deferred to a later step if needed (start with ASCII assumption).

**New file:** `NavigationService.cs` — encapsulates cursor-to-offset and
offset-to-cursor conversions plus the `NavigateTo` logic.

**Verification:** Render a graph for a simple function. Click a node. Verify
the VS editor cursor moves to the corresponding code location.

---

### Step 6: Monitor Editor Events (Cursor & Text Changes)

**Goal:** Detect when the user moves the cursor or changes the active document,
and send the current code + offset to the webview.

**Tasks:**
1. Subscribe to `IVsRunningDocumentTable` events or use the
   `DTE.Events.TextEditorEvents` / `IVsTextManager` to detect:
   - Active document changes
   - Cursor position changes (caret movement)
   - Text changes (document edits)

2. On each relevant event:
   a. Get the active `IVsTextView` and its `IVsTextLines`.
   b. Read the full document text.
   c. Get the cursor position (line, column).
   d. Convert cursor position to byte offset (UTF-8 encoding of text up to
      cursor position).
   e. Determine the language from the file extension or VS content type.
   f. Call `WebviewBridge.SendCodeAsync(text, byteOffset, language)`.

3. **Debounce/throttle** the cursor-move handler to avoid flooding the webview
   with messages on rapid cursor movements (e.g., holding arrow key). A 50–100ms
   debounce is appropriate.

**New file:** `EditorMonitor.cs`

**Language mapping table:**

| File Extension | VS ContentType | Webview Language ID |
|---|---|---|
| `.c` | `C/C++` | `c` |
| `.cpp`, `.cxx`, `.cc`, `.h`, `.hpp` | `C/C++` | `cpp` |
| `.go` | `Go` | `go` |
| `.py` | `Python` | `python` |
| `.ts` | `TypeScript` | `typescript` |
| `.js` | `JavaScript` | `javascript` |

Use file extension for simplicity (VS content types can be inconsistent across
language extensions).

**Verification:** Open a supported source file, move the cursor into different
functions. Verify the graph updates to show the current function's CFG and
highlights the current node.

---

### Step 7: Extract and Send Theme Colors

**Goal:** Read the current VS color theme and send color values to the webview
so the graph matches the IDE appearance.

**Tasks:**
1. Create a color mapping from VS theme colors to the graph's expected color
   names.

   **Required color names** (from the VSCode extension):
   ```
   graph.background
   node.default.background
   node.default.border
   node.highlight.background
   node.highlight.border
   edge.regular
   edge.consequence
   edge.alternative
   edge.exception
   text.default
   ```

2. Read VS theme colors using `IVsUIShell5.GetThemedColor()` or
   `VSColorTheme.GetThemedColor()` with appropriate `ThemeResourceKey` values.

3. Map VS theme colors to graph color names:
   - `graph.background` ← `EnvironmentColors.ToolWindowBackgroundColorKey`
   - `node.default.background` ← `EnvironmentColors.ToolWindowContentGridColorKey`
     (or derive from background)
   - `edge.regular` ← `EnvironmentColors.ToolWindowTextColorKey`
   - etc.

4. Send the color list via `WebviewBridge.SendColorsAsync()`.

5. Subscribe to `VSColorTheme.ThemeChanged` event to re-send colors when the
   user switches themes.

**New file:** `ThemeMonitor.cs`

**Verification:** Switch between Light, Dark, and Blue themes. Verify the graph
colors update to match.

---

### Step 8: Add Extension Settings (Options Page)

**Goal:** Expose the three boolean settings (`simplify`, `flatSwitch`,
`highlightCurrentNode`) in VS Options dialog.

**Tasks:**
1. Create a `DialogPage` subclass for the options:
   ```csharp
   [Guid("...")]
   public class FunctionGraphOptions : DialogPage
   {
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
   }
   ```

2. Register it in `FunctionGraphOverviewPackage` with `[ProvideOptionPage]`.

3. Send settings to the webview on change and on initial load.

**Verification:** Open Tools → Options → Function Graph Overview. Change
settings. Verify the graph updates (e.g., toggling "Simplify" changes the graph
structure).

---

### Step 9: Polish & Error Handling

**Goal:** Handle edge cases and improve robustness.

**Tasks:**
- Handle unsupported languages gracefully (don't send messages, or send empty
  code to clear the graph)
- Handle the case where the tool window is open but no editor is active (show
  empty/placeholder state)
- Handle WebView2 runtime not installed (already partially done with the
  `try-catch` in `InitializeWebViewAsync`)
- Ensure the webview doesn't process stale messages when switching between files
  rapidly
- Handle VS shutdown gracefully (dispose WebView2, unsubscribe from events)

**Verification:** Exercise edge cases: close all editors, open non-supported
files, rapidly switch between files, switch themes, resize the tool window.

---

## Summary of New Files

| File | Purpose |
|---|---|
| `WebviewAssets/` | Pre-built SPA files (JS, CSS, WASM) |
| `WebviewBridge.cs` | C# → JS message sending |
| `NavigationService.cs` | Byte-offset ↔ editor-position conversion, cursor navigation |
| `EditorMonitor.cs` | Listens to VS editor events, sends `updateCode` |
| `ThemeMonitor.cs` | Extracts VS theme colors, sends `updateSettings` |
| `FunctionGraphOptions.cs` | VS Options dialog page |

## Summary of Modified Files (Webview SPA — external project)

| File | Change |
|---|---|
| `App.svelte` | Add `initVisualStudio()` function + call it |
| Type declarations | Add `window.VisualStudio` and `window.chrome.webview` types |

---

## Dependency Order

```
Step 1 (cleanup)
    │
    ▼
Step 2 (local assets)
    │
    ▼
Step 3 (webview bridge JS) ──── requires change to external webview project
    │
    ▼
Step 4 (C# → webview bridge)
    │
    ├──► Step 5 (navigateTo / webview → C#)
    │
    ├──► Step 6 (editor monitor) ──► Step 7 (theme colors)
    │
    └──► Step 8 (settings)
         │
         ▼
      Step 9 (polish)
```

Steps 5, 6, and 8 can be developed in parallel after Step 4.
Step 7 depends on Step 6 (needs EditorMonitor lifecycle to trigger initial color send).
Step 9 is final polish after all features work.
