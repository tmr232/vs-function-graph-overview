---
name: building-project
description: "Builds, tests, and formats this Visual Studio 2022 extension project. Use when asked to build, run tests, restore packages, or format code."
---

# Building the Project

This is a Visual Studio 2022 extension (VSIX) using an old-style .csproj
(non-SDK) targeting .NET Framework 4.7.2.

## Key Constraint

The main project (`FunctionGraphOverview`) **cannot be built with `dotnet
build`**. It requires MSBuild from a Visual Studio installation because it
depends on VS SDK targets (`Microsoft.VsSDK.targets`) and NuGet packages
(`Microsoft.VisualStudio.SDK`, `VSSDK.BuildTools`) that are only available
through the full MSBuild/NuGet toolchain.

The test project (`FunctionGraphOverview.Tests`) is SDK-style and _can_ use
`dotnet test`, but only **after** the main project has been built with MSBuild
(the test project references the compiled DLL, not a ProjectReference).

## Finding MSBuild

Use `vswhere` to locate MSBuild:

```powershell
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$installPath = & $vswhere -latest -property installationPath
$msbuild = "$installPath\MSBuild\Current\Bin\MSBuild.exe"
```

## Build Workflow

### 1. Restore NuGet packages

```powershell
# If nuget.exe is on PATH:
nuget restore vs-function-graph-overview.sln

# Otherwise MSBuild can restore inline:
& $msbuild FunctionGraphOverview\FunctionGraphOverview.csproj /restore /p:Configuration=Debug /v:minimal
```

### 2. Build the solution

```powershell
& $msbuild vs-function-graph-overview.sln /p:Configuration=Release /p:Platform="Any CPU"
```

- The VSIX packaging step may emit `VSSDK1310` about a missing `LICENSE.txt` in
  the archive. This is a packaging issue and does **not** prevent the DLL from
  being compiled. The DLL output is at
  `FunctionGraphOverview\bin\{Configuration}\FunctionGraphOverview.dll`.

### 3. Run tests

The test project references the built DLL via a `<Reference>` with a
`<HintPath>` pointing at `FunctionGraphOverview\bin\$(Configuration)\FunctionGraphOverview.dll`.
Build the main project first, then:

```powershell
dotnet test FunctionGraphOverview.Tests\FunctionGraphOverview.Tests.csproj -c Release --no-build
```

If the main project was built as Debug:

```powershell
dotnet test FunctionGraphOverview.Tests\FunctionGraphOverview.Tests.csproj -c Debug
```

### 4. Format code

CSharpier is installed as a .NET local tool (`.config/dotnet-tools.json`):

```powershell
dotnet tool restore
dotnet tool run csharpier format
```

Pre-commit hooks also run CSharpier on staged C# files and zizmor on GitHub
Actions workflow files.

## Project Structure

| Path | Type | Build tool |
|---|---|---|
| `FunctionGraphOverview/` | Old-style .csproj, VSIX extension | MSBuild only |
| `FunctionGraphOverview.Tests/` | SDK-style .csproj, xUnit tests | `dotnet test` (after MSBuild builds main) |

## Common Pitfalls

- **`dotnet build` on the main project fails** with hundreds of "namespace not
  found" errors for `Microsoft.VisualStudio.*`. This is expected — use MSBuild.
- **`dotnet restore` succeeds** on both projects but doesn't produce all
  required assemblies for the old-style project.
- **ProjectReference from the test project to the main project fails** because
  `dotnet build` cannot build the VSIX project. The test project uses a direct
  DLL reference instead. The solution file declares a `ProjectDependencies`
  section so MSBuild builds the main project before the test project.
- **Test configuration must match the main build configuration.** If you build
  Debug, test with `-c Debug`; if Release, use `-c Release`.

## CI (GitHub Actions)

The `build.yml` workflow:
1. Builds webview assets on Ubuntu (Bun)
2. On Windows: `nuget restore` → `msbuild` → `dotnet test` → upload VSIX artifact
