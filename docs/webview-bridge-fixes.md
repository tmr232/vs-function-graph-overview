# Webview Bridge Fixes

## Summary

Fixed three issues preventing the webview from rendering function graphs.

## Issues & Fixes

### 1. Race condition: scripts executed before page load

**File:** `FunctionGraphOverview/FunctionGraphToolWindowControl.xaml.cs`

`InitializeWebViewAsync` created the `WebviewBridge`, `EditorMonitor`, and `ThemeMonitor` immediately after calling `Navigate()`, without waiting for the page to finish loading. Both `EditorMonitor` and `ThemeMonitor` call `ExecuteScriptAsync` in their constructors, but the page's JavaScript hadn't loaded yet so the calls had no effect.

**Fix:** Subscribe to `NavigationCompleted`, await it after `Navigate()`, and only then create the bridge and monitors.

### 2. Color scheme name mismatch

**File:** `FunctionGraphOverview/ThemeMonitor.cs`

The C# side sent color entries with names like `node.default.background`, `node.highlight.border`, `edge.exception`, `text.default`. The JS renderer expects a specific set of 17 names: `node.default`, `node.entry`, `node.exit`, `node.throw`, `node.yield`, `node.border`, `node.highlight`, `edge.regular`, `edge.consequence`, `edge.alternative`, `cluster.border`, `cluster.with`, `cluster.tryComplex`, `cluster.try`, `cluster.finally`, `cluster.except`, `graph.background`.

The mismatched list caused `find(...).hex` to throw during reactive re-rendering, which blocked all graph output.

**Fix:** Updated `ExtractColors()` to emit all 17 color names matching the JS scheme.

### 3. Language identifier mismatch

**File:** `FunctionGraphOverview/EditorMonitor.cs`

The C# `LanguageMap` sent lowercase language identifiers (`"c"`, `"cpp"`, `"python"`, `"typescript"`, `"javascript"`). The JS `isValidLanguage` checks against a case-sensitive list: `["C", "Go", "Java", "Python", "C++", "TypeScript", "TSX"]`. Every `setCode` call was silently rejected.

**Fix:** Updated the map values to match the JS identifiers exactly. Also added missing `.java` and `.tsx` entries, and removed `.js` (no corresponding tree-sitter wasm).
