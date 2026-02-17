# Function Graph Overview — Visual Studio Extension

A Visual Studio 2022 extension that displays a control flow graph (CFG) for the
function at the cursor position. Built on the
[function-graph-overview](https://github.com/tmr232/function-graph-overview)
webview SPA.

## Features

- **Live CFG rendering** — automatically generates and displays a control flow
  graph for the function under the cursor as you navigate your code.
- **Click-to-navigate** — click a node in the graph to jump to the
  corresponding source location.
- **Color scheme** — choose between Dark, Light, System (follows IDE theme), or
  Custom (paste your own color scheme JSON) via Tools → Options → Function Graph
  Overview. Dark and Light schemes use the canonical
  [function-graph-overview](https://github.com/tmr232/function-graph-overview)
  colors with VS-appropriate backgrounds.
- **Configurable** — toggle simplification, flat switch rendering, and current
  node highlighting via Tools → Options → Function Graph Overview.

## Supported Languages

| Language   | Extensions                         |
|------------|------------------------------------|
| C          | `.c`                               |
| C++        | `.cpp`, `.cxx`, `.cc`, `.h`, `.hpp`|
| Go         | `.go`                              |
| Python     | `.py`                              |
| TypeScript | `.ts`                              |
| JavaScript | `.js`                              |

## Usage

1. Open the tool window via **View → Other Windows → Function Graph Overview**.
2. Open a supported source file and place your cursor inside a function.
3. The graph updates automatically as you move between functions or edit code.

## Architecture

The extension hosts a WebView2 control that runs the function-graph-overview
Svelte SPA locally. The C# host monitors editor events and communicates with the
webview via `ExecuteScriptAsync` (C# → JS) and `WebMessageReceived` (JS → C#).

```
┌──────────────────────────────────────────────┐
│            Visual Studio 2022                │
│  ┌────────────────────────────────────────┐  │
│  │  EditorMonitor   ThemeMonitor          │  │
│  │       │               │                │  │
│  │       ▼               ▼                │  │
│  │     WebviewBridge (ExecuteScriptAsync)  │  │
│  │              │                         │  │
│  │     ┌────────▼──────────────────────┐  │  │
│  │     │  WebView2 (Svelte SPA)       │  │  │
│  │     │  Tree-Sitter → CFG → SVG     │  │  │
│  │     └───────────────────────────────┘  │  │
│  └────────────────────────────────────────┘  │
└──────────────────────────────────────────────┘
```

## Building

Requires Visual Studio 2022 with the **Visual Studio extension development**
workload installed.

1. Open `vs-function-graph-overview.sln` in Visual Studio 2022.
2. Build the solution (Ctrl+Shift+B).
3. Press F5 to launch the Experimental Instance with the extension loaded.

### Updating Webview Assets

The `FunctionGraphOverview/WebviewAssets/` directory contains pre-built files
from the [function-graph-overview](https://github.com/tmr232/function-graph-overview)
project. To update them:

1. Clone and build the webview project (`bun run build-webview`).
2. Copy the contents of `dist/webview/assets/` into `WebviewAssets/assets/`.

## CI / CD

GitHub Actions workflows live in `.github/workflows/`:

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| `build.yml` | Push to `main`, PRs | Builds the webview from [function-graph-overview](https://github.com/tmr232/function-graph-overview) and compiles the VSIX |
| `release.yml` | GitHub release published | Builds, publishes to the VS Marketplace, and uploads the VSIX as a release asset |

The webview is checked out from a dedicated `visualstudio-*` tag in the
function-graph-overview repo (mirroring the `jetbrains-*` tags used by the
[JetBrains plugin](https://github.com/tmr232/jb-function-graph-overview)).

### Workflow security

- **[ratchet](https://github.com/sethvargo/ratchet)** — all action references
  are pinned to commit SHAs (with version comments for readability).
  Run `ratchet pin <workflow>` after adding or updating an action.
- **[zizmor](https://github.com/zizmorcore/zizmor)** — static analysis for
  GitHub Actions. Runs as a [pre-commit](https://pre-commit.com/) hook via
  [zizmor-pre-commit](https://github.com/zizmorcore/zizmor-pre-commit) to catch
  issues like unpinned actions, excessive permissions, and template injection.

### Code formatting

- **[CSharpier](https://csharpier.com/)** — opinionated C# formatter, runs as a
  pre-commit hook via `dotnet tool run csharpier`.
