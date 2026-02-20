# Contributing

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
