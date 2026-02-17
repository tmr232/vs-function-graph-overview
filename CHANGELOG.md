# Changelog

All notable changes to the Function Graph Overview extension will be documented
in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added
- Output Window logging pane ("Function Graph Overview") for diagnostics (#2.1)
- ARM64 support — the extension now installs natively on ARM64 VS 2022 (#3.1)
- Marketplace metadata: tags, license, release notes, repo link (#3.2)
- `Microsoft.VisualStudio.SDK.Analyzers` for compile-time threading checks (#4.1)
- `.editorconfig` with C# naming and style conventions (#4.2)
- `CHANGELOG.md` (#7.1)

### Fixed
- Added missing `publish-manifest.json` required by the release workflow (#1.1)
- Fire-and-forget async calls now log errors instead of silently failing (#1.2)
- WebView2 control and monitors are now properly disposed (#1.3)
- Silent `catch` block in webview message handler now logs exceptions (#2.2)

### Changed
- Command icon switched from bitmap strip to `KnownMonikers.FlowChart` for
  proper DPI scaling and theme support (#5.1)
