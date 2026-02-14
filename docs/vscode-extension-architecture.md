# VSCode Extension Architecture — function-graph-overview

> Reference analysis of [github.com/tmr232/function-graph-overview](https://github.com/tmr232/function-graph-overview)
> to inform the Visual Studio extension implementation.

---

## Table of Contents

- [Overview](#overview)
- [Build System](#build-system)
- [Extension Host](#extension-host)
- [Webview](#webview)
- [Message Protocol](#message-protocol)
- [Rendering Pipeline](#rendering-pipeline)
- [Security](#security)
- [Supported Languages](#supported-languages)
- [Key Takeaways for VS Implementation](#key-takeaways-for-vs-implementation)

---

## Overview

The VSCode extension displays a **Control-Flow Graph (CFG)** of the function
surrounding the user's cursor. It is split into two isolated processes:

| Layer | Technology | Responsibility |
|---|---|---|
| **Extension Host** | Node.js (TypeScript) | Listens to editor events, extracts code + cursor offset, manages settings |
| **Webview** | Svelte SPA (sandboxed browser) | Parses code, builds CFG, renders SVG via Graphviz, handles pan/zoom |

The extension host does **no** code analysis. It sends raw source code and a
cursor offset to the webview, which owns the entire rendering pipeline.

---

## Build System

The project uses a **dual build**:

| Target | Tool | Entry Point | Output |
|---|---|---|---|
| Extension host | esbuild | `src/vscode/extension.ts` | `dist/vscode/extension.cjs` |
| Webview frontend | Vite + Svelte | `src/webview/src/main.js` | `dist/webview/assets/index.{js,css}` |

Both outputs are packaged together into the `.vsix` distributable. The webview
assets are referenced via `webview.asWebviewUri()` at runtime.

---

## Extension Host

### Source Files

```
src/vscode/
├── extension.ts        # activate(), event handlers, command registration
├── overview-view.ts    # WebviewViewProvider implementation
└── messages.ts         # Type-safe message definitions
```

### Activation (`extension.ts`)

The `activate()` function:

1. Creates an `OverviewViewProvider` instance.
2. Registers it with `vscode.window.registerWebviewViewProvider()`.
3. Registers the `functionGraphOverview.focus` command.
4. Attaches three event listeners:

| Event | Action |
|---|---|
| `onDidChangeTextEditorSelection` | Extracts code + cursor offset, sends `updateCode` message |
| `onDidChangeConfiguration` | Reloads settings, sends `updateSettings` message |
| `onDidChangeActiveColorTheme` | Reloads colors, sends `updateSettings` message |

### Code Extraction

On every cursor move, the extension:

1. Gets the active editor's `document.getText()` (full file content).
2. Computes the cursor offset with `document.offsetAt(position)`.
3. Determines the language from `document.languageId`.
4. Sends all three values to the webview.

The extension does **not** extract just the current function — the webview
handles function isolation using Tree-Sitter.

### Settings Managed

| Setting | Type | Purpose |
|---|---|---|
| `simplify` | `boolean` | Simplify the CFG by merging linear chains |
| `flatSwitch` | `boolean` | Flatten switch/case structures |
| `highlightCurrentNode` | `boolean` | Highlight the CFG node at cursor |
| `colorList` | `ColorList` | Theme-derived colors for graph elements |

Colors are loaded from the current VSCode theme and mapped to graph-specific
color names (e.g., `graph.background`, `node.default`, `edge.regular`).

---

## Webview

### Source Files

```
src/webview/
├── index.html              # Dev server HTML (not used in production)
├── vite.config.js          # Vite build config
└── src/
    ├── main.js             # Entry point — mounts Svelte app
    ├── App.svelte          # Root component + message listener
    ├── app.css             # Global styles (full-height flexbox)
    └── reset.css           # CSS reset

src/components/
├── WebviewRenderer.svelte  # Main rendering orchestrator
├── PanzoomComp.svelte      # Pan & zoom wrapper
├── renderer.ts             # Renderer class (code → CFG → DOT → SVG)
├── utils.ts                # Parser initialization helpers
├── caching.ts              # Memoization utilities
└── panzoom-utils.ts        # Pan/zoom helpers
```

### HTML Injection (`overview-view.ts`)

The `OverviewViewProvider.resolveWebviewView()` method generates the HTML
served to the webview:

```html
<!DOCTYPE html>
<html lang="en">
  <head>
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <meta http-equiv="Content-Security-Policy" content="...">
    <link rel="stylesheet" type="text/css" nonce="${nonce}" href="${stylesUri}">
    <title>Function Graph Overview</title>
  </head>
  <body data-theme="${isDark ? 'dark' : 'light'}">
    <div id="app"></div>
    <script type="module" nonce="${nonce}" src="${scriptUri}"></script>
  </body>
</html>
```

- `stylesUri` → `dist/webview/assets/index.css`
- `scriptUri` → `dist/webview/assets/index.js`
- URIs are converted with `webview.asWebviewUri()` for the sandboxed context.

### Webview Options

```typescript
webviewView.webview.options = {
  enableScripts: true,
  localResourceRoots: [this._extensionUri],
};
```

---

## Message Protocol

Defined in `src/vscode/messages.ts` using TypeScript discriminated unions.

### Extension → Webview

#### `updateCode`

Sent on every cursor movement.

```typescript
{
  tag: "updateCode";
  code: string;       // Full file source code
  offset: number;     // Cursor byte offset within the file
  language: Language;  // e.g., "typescript", "python", "go"
}
```

#### `updateSettings`

Sent on configuration or theme change.

```typescript
{
  tag: "updateSettings";
  flatSwitch: boolean;
  simplify: boolean;
  highlightCurrentNode: boolean;
  colorList: ColorList;   // Array of {name, hex} color entries
}
```

### Webview → Extension

#### `navigateTo`

Sent when the user clicks a CFG node.

```typescript
{
  tag: "navigateTo";
  offset: number;   // Byte offset of the code corresponding to the clicked node
}
```

### Message Handler

A generic `MessageHandler<Msg>` class dispatches messages by `tag`:

```typescript
class MessageHandler<Msg extends Message> {
  constructor(private messageHandlers: MessageHandlersOf<Msg>) {}

  handleMessage<T extends MessageTagOf<Msg>>(message: MessageMapOf<Msg>[T]) {
    const handler = this.messageHandlers[message.tag];
    handler(message);
  }
}
```

This ensures compile-time type safety: each handler receives the correctly
typed payload for its tag.

---

## Rendering Pipeline

All rendering happens **inside the webview**. The pipeline:

```
Source Code + Offset
       │
       ▼
 Tree-Sitter WASM Parser
       │  (language-specific grammar)
       ▼
 AST (Abstract Syntax Tree)
       │
       ▼
 getFunctionAtOffset(tree, offset)
       │  (isolates the enclosing function)
       ▼
 CFG Builder (language-specific)
       │  (builds control-flow graph)
       ▼
 Simplify / Flatten (optional)
       │
       ▼
 DOT String Generation
       │
       ▼
 Graphviz WASM (dot → SVG)
       │
       ▼
 SVG with node-to-offset mapping
       │
       ▼
 PanzoomComp (interactive display)
       │  Highlights current node based on offset
       │  Click on node → navigateTo message
       ▼
 Rendered Graph in Sidebar
```

### Key Components

| Component | Role |
|---|---|
| **Tree-Sitter WASM** | Parses source code into an AST; one `.wasm` grammar file per language |
| **CFG Builder** | Language-specific; converts AST subtree into a control-flow graph |
| **Renderer class** | Orchestrates parse → CFG → DOT; caches results; maps nodes ↔ offsets |
| **Graphviz WASM** | Compiles DOT notation into SVG |
| **PanzoomComp** | Wraps the SVG with pan/zoom interactivity; auto-pans to highlighted node |

### Caching & Optimization

- Parser instances are loaded once per language and reused.
- The `Renderer` class memoizes rendering results.
- Function identity tracking avoids unnecessary panzoom resets when the same
  function is re-rendered (e.g., cursor moves within the same function).

---

## Security

The webview uses a strict Content Security Policy:

```
default-src 'none';
connect-src ${webview.cspSource};
style-src ${webview.cspSource} 'nonce-${nonce}';
script-src ${webview.cspSource} 'wasm-unsafe-eval' 'nonce-${nonce}';
img-src ${webview.cspSource};
font-src ${webview.cspSource};
```

- All resources must come from the extension's URI.
- Scripts and styles require a per-session nonce.
- `wasm-unsafe-eval` is required for Tree-Sitter and Graphviz WASM execution.
- No inline scripts or styles without the nonce.

---

## Supported Languages

| Language | Language ID | CFG Builder |
|---|---|---|
| C | `c` | `cfg-c.ts` |
| C++ | `cpp` | `cfg-cpp.ts` |
| Go | `go` | `cfg-go.ts` |
| Python | `python` | `cfg-python.ts` |
| TypeScript | `typescript` | `cfg-typescript.ts` |
| JavaScript | `javascript` | `cfg-typescript.ts` (shared) |

Each language has a dedicated Tree-Sitter `.wasm` grammar and a CFG builder
that understands its control-flow constructs (loops, conditionals, switches,
try/catch, etc.).

---

## Key Takeaways for VS Implementation

### 1. Webview Pattern

The extension follows a **host ↔ webview** split. The host monitors the editor
and sends lightweight messages; the webview owns all heavy computation. This
same pattern can be replicated in Visual Studio using a **tool window with a
WebView2 control**.

### 2. Minimal Host Responsibility

The extension host sends only:
- The **full source code** of the active file
- The **cursor byte offset**
- The **language identifier**
- **Settings and colors**

It does not parse, analyze, or transform code. This keeps the host simple.

### 3. Reusable Webview Code

Since the webview is a standalone Svelte SPA that communicates via
`postMessage`, the **same webview bundle** could potentially be reused in the
VS extension's WebView2 control, with only the host-side message bridge needing
to be reimplemented.

### 4. Bidirectional Navigation

- **Editor → Graph**: Cursor offset determines which CFG node to highlight.
- **Graph → Editor**: Clicking a node sends its code offset back to the host,
  which moves the editor cursor.

Both directions use byte offsets as the common coordinate system.

### 5. Color Theming

Colors are extracted from the IDE theme on the host side and sent to the
webview as a flat list of `{name, hex}` pairs. The webview applies them
dynamically. This avoids the webview needing to know about IDE theme APIs.
