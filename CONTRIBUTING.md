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

## Debugging

### Extension Logs

The extension writes diagnostic messages to a dedicated Output Window pane. To
view them:

1. Open **View → Output** (or <kbd>Ctrl+Alt+O</kbd>).
2. In the **Show output from** dropdown, select **Function Graph Overview**.

Errors from webview message handling and background tasks are logged here
automatically.

## Pre-commit Hooks

The repo uses [pre-commit](https://pre-commit.com/) hooks (configured in
`.pre-commit-config.yaml`) to enforce code quality on every commit:

| Hook | What it does |
|------|-------------|
| `zizmor` | Static analysis for GitHub Actions workflows |
| `dotnet-tool-restore` | Restores .NET tools (runs before CSharpier) |
| `csharpier` | Opinionated C# formatter |

### Setup

Install and activate the hooks using [prek](https://prek.j178.dev/), a fast,
single-binary runner for pre-commit hooks:

```bash
prek install
```

Once installed, hooks run automatically on `git commit`. To run them manually:

```bash
prek run --all-files
```
