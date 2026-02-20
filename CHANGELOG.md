# Changelog

All notable changes to the Function Graph Overview extension will be documented
in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added
- Language detection now uses Visual Studio's content type as the primary source,
  falling back to file extensions. This enables support for extensionless files
  such as C++ standard library headers.
- Unit test project (`FunctionGraphOverview.Tests`) with xUnit covering
  `NavigationService.Utf8ByteOffsetToCharOffset`, `ColorSchemeDefinitions`, and
  `LanguageMap` (#8.1)
- Tests run automatically in CI via `dotnet test` (#8.2)
- Output Window logging pane ("Function Graph Overview") for diagnostics (#2.1)
- ARM64 support — the extension now installs natively on ARM64 VS 2022 (#3.1)
- Marketplace metadata: tags, license, release notes, repo link (#3.2)
- `Microsoft.VisualStudio.SDK.Analyzers` for compile-time threading checks (#4.1)
- `.editorconfig` with C# naming and style conventions (#4.2)
- `CHANGELOG.md` (#7.1)
- Automated version stamping: VSIX and assembly versions are derived from the
  release tag in CI (#6.1)

### Fixed
- VSIX build error VSSDK1310: added MIT license file to VSIX package and
  reordered manifest metadata to satisfy schema validation
- Command icon: use `Include href="KnownImageIds.vsct"` instead of
  `Extern href="KnownImageIds.h"` and switch to `ControlFlow` moniker
- Added missing `publish-manifest.json` required by the release workflow (#1.1)
- Fire-and-forget async calls now log errors instead of silently failing (#1.2)
- WebView2 control and monitors are now properly disposed (#1.3)
- Silent `catch` block in webview message handler now logs exceptions (#2.2)

### Changed
- Command icon switched from bitmap strip to `KnownMonikers.ControlFlow` for
  proper DPI scaling and theme support (#5.1)
