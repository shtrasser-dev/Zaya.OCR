# Versioning (Zaya.OCR)

| Axis | Source | Example |
|------|--------|---------|
| **ZayaPrimitivesVersion** | `Directory.Build.props` (supplies **Major**) | `1.0.0` |
| **interfaceVersion** | `Zaya.OCR.csproj` → only **`ZayaVersionInterface`** → `Major.Interface.0` | `1.3.0` |
| **pluginVersion** | Each Impl → **`ZayaVersionImpMajor`** + **`ZayaVersionImpMinor`**; Interface read from abstractions → `Major.Interface.ImpMajor.ImpMinor` | `1.3.0.0` |
| **updateChannel** | Interface `MAJOR.Interface` | `1.3` → `plugin-Zaya.OCR-v1.3-latest` |

Rules:

- Abstractions: only `ZayaVersionInterface`. Version always ends with `.0`. Contract/assembly change → bump Interface.
- Plugin: only `ZayaVersionImpMajor` / `ZayaVersionImpMinor`. Interface digit is taken from `Zaya.OCR.csproj` automatically.
- Do not set `<Version>` manually. `Directory.Build.targets` builds it and checks Major vs Primitives.
- Host loads a zip only if `interfaceVersion` **exactly** matches host’s `Zaya.OCR` version.
- **One interface → one floating GitHub tag:** `plugin-Zaya.OCR-v{channel}-latest` (immutable: `plugin-Zaya.OCR-v{pluginVersion}`). All OCR/layout zips ship in that release.
- `build.cmd` writes `out/interfaces.json` for the Publish workflow.

## plugin.json

```json
{
  "id": "OneOcr",
  "type": "ocr",
  "interface": "Zaya.OCR",
  "interfaceVersion": "1.3.0",
  "pluginVersion": "1.3.0.0",
  "entryPoint": "Zaya.OCR.Impl.OneOcr.OneOcrService"
}
```

`entryPoint` is the fully qualified type that implements `IOCRService` / `ITextLayoutService` (hosts construct it with `ILoggingWrapper`, or use the parameterless ctor). Other plugins: `Zaya.OCR.Impl.WindowsMediaOcr.WindowsMediaOcrService`, `Zaya.OCR.Impl.ProximityTextLayout.ProximityTextLayoutService`.

Release body lists per-asset plugin versions (`Zaya.OCR.Impl.OneOcr.zip=1.3.0.0`).

## Changelog

Use root [`CHANGELOG.md`](../CHANGELOG.md) ([Keep a Changelog](https://keepachangelog.com/)):

1. While working, append notes under `## [Unreleased]`.
2. Run the Publish workflow — GitHub Release body is taken from `[Unreleased]` (plus release metadata). There is no changelog input on the action.
3. After a successful publish, move that block to a dated section, e.g. `## [1.3.0.0] - 2026-08-13`, and leave `[Unreleased]` empty for the next cycle.

Do not backfill older releases; history starts from the current line.

## Bumping

1. Interface: raise `ZayaVersionInterface` in `Zaya.OCR.csproj`, update host, republish plugins.
2. Single engine: raise `ZayaVersionImpMajor` / `ZayaVersionImpMinor` only in that Impl’s `.csproj`.
3. Update `CHANGELOG.md` `[Unreleased]`, then run `build.cmd` / Publish workflow.
