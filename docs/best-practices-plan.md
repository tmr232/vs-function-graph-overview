# Best Practices Improvement Plan

Gaps identified by comparing against well-maintained VS extensions (VsVim,
Community.VisualStudio.Toolkit, GitHub for Visual Studio).

## Workflow

After completing each task below:

1. **Document** — update `CHANGELOG.md` (create it in task 7.1 if it doesn't
   exist yet; until then, note the change in the commit message body).
2. **Commit** — make a focused commit with a message referencing the task
   number, e.g. `fix: dispose WebView2 and monitors (#1.3)`.
3. **Verify** — build the solution (`msbuild /p:Configuration=Release`) and
   confirm no regressions before moving to the next task.

## 1. Bugs & correctness

- [x] **1.1 — Add `publish-manifest.json`**
  `release.yml:67` references `publish-manifest.json` but the file doesn't exist
  in the repo. Create it with the required Marketplace metadata (publisher name,
  identity, categories, etc.) or the release workflow will fail.

- [x] **1.2 — Fix fire-and-forget async calls**
  Several places discard `Task` results with no error handling:
  - `ThemeMonitor.cs:45,61` — `_ = _bridge.SendColorsAsync(...)`
  - `SettingsMonitor.cs:41` — `_ = _bridge.SendSettingsAsync(...)`
  - `EditorMonitor.cs:167–173` — has a `TODO` acknowledging the problem

  Wrap each in `JoinableTaskFactory.RunAsync(() => ...).FireAndForget()` or add
  `.ContinueWith(t => Log(t.Exception), OnlyOnFaulted)`.

- [x] **1.3 — Dispose WebView2 and monitors**
  `FunctionGraphToolWindowControl` creates `_bridge`, `_editorMonitor`,
  `_themeMonitor`, and `_settingsMonitor` but never disposes them.
  - Implement `IDisposable` on the control (or override `Dispose` in
    `FunctionGraphToolWindow` pane).
  - Call `webView.Dispose()` and dispose all four monitors.

## 2. Diagnostics & error handling

- [x] **2.1 — Add a dedicated Output Window pane for logging**
  Create a helper (e.g. `LogService`) that lazily creates an Output Window pane
  named "Function Graph Overview" and exposes `Log(string)` /
  `LogAsync(Exception)`.

- [x] **2.2 — Replace silent `catch` blocks with logging**
  `FunctionGraphToolWindowControl.cs:108` catches all exceptions and discards
  them. Log to the pane from 2.1 so users can diagnose webview communication
  failures.

## 3. VSIX manifest & Marketplace

- [x] **3.1 — Add ARM64 `ProductArchitecture`**
  Add a second `<InstallationTarget>` with `<ProductArchitecture>arm64</ProductArchitecture>`
  so the extension installs natively on ARM64 VS 2022.

- [x] **3.2 — Add Marketplace metadata**
  Add the following elements to `source.extension.vsixmanifest`:
  - `<Icon>` — 90×90 PNG
  - `<PreviewImage>` — screenshot of the graph in action
  - `<Tags>` — e.g. `cfg, control flow, graph, code visualization`
  - `<ReleaseNotes>` — link to GitHub releases
  - `<License>` — `LICENSE.txt`
  - `<MoreInfo>` — link to the repo

## 4. Code quality tooling

- [x] **4.1 — Add `Microsoft.VisualStudio.SDK.Analyzers`**
  Add the NuGet package to the `.csproj`. It catches threading violations and
  missing `ThrowIfNotOnUIThread()` at compile time.

- [x] **4.2 — Add `.editorconfig`**
  Cover rules CSharpier doesn't enforce: underscore-prefixed private fields,
  `Async` suffix on async methods, expression-body preferences, etc.

## 5. Icons

- [x] **5.1 — Switch to `KnownMonikers` for the command icon**
  Replace the bitmap strip in `.vsct` with a `KnownMonikers` reference (e.g.
  `FlowChart` or `GraphLeftToRight`). Remove the unused
  `FunctionGraphToolWindowCommand.png` bitmap strip. This auto-themes and
  scales correctly on all DPI settings.

## 6. Versioning

- [x] **6.1 — Automate version stamping**
  The assembly version and VSIX manifest version are both hardcoded to `1.0`.
  Either:
  - Use Nerdbank.GitVersioning (`nbgv`), or
  - Stamp the version from the git tag in CI (e.g.
    `msbuild /p:Version=${{ github.event.release.tag_name }}`).

## 7. Documentation

- [x] **7.1 — Add `CHANGELOG.md`**
  Track user-visible changes per release. Link from the VSIX manifest
  `<ReleaseNotes>` and from GitHub Releases.

## 8. Testing

- [x] **8.1 — Add a unit test project**
  Create `FunctionGraphOverview.Tests` (xUnit). Start with pure-logic tests:
  - `NavigationService.Utf8ByteOffsetToCharOffset` (extract to `internal` +
    `InternalsVisibleTo`)
  - `ColorSchemeDefinitions.GetDarkScheme()` / `GetLightScheme()` — verify
    expected color count and names
  - `EditorMonitor.LanguageMap` — verify extension → language mapping

- [x] **8.2 — Run tests in CI**
  Add a `dotnet test` step to `build.yml` after the MSBuild step.
