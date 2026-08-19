# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
History starts at the current release line; older releases are not backfilled.

## [Unreleased]

### Changed

Version bumps from the Primitives 2.0 migration decisions:

| Axis | Was | Now | Decision |
|------|-----|-----|----------|
| **Zaya.Primitives** (NuGet) | `1.0.0` | `2.0.0` | published major |
| **ZayaVersionInterface** | `3` | `0` | reset for new Primitives major |
| **Zaya.OCR** (interface) | `1.3.0` | `2.0.0` | `Major.Interface.0` |
| **OneOcr / WindowsMediaOcr / ProximityTextLayout** | `1.3.0.0` | `2.0.0.0` | ImpMajor/ImpMinor stay `0.0` |
| **update channel** | `plugin-Zaya.OCR-v1.3-latest` | `plugin-Zaya.OCR-v2.0-latest` | host must match exactly |
| **Zaya.Logging** | `1.0.0` | `1.0.0` | unchanged |

API alignment with Primitives 2.0:

- Result/layout models (`BoundingBox`, `IOCRWord`, `IOCRResult`, `ITextLine`, `ITextParagraph`, `ITextResult`) live in `Zaya.Primitives` / `Zaya.Primitives.OCR`; local duplicates removed.
- `SettingDescriptor*` moved to `Zaya.Primitives.Settings`.
- Tracking/ghost metadata exposed as optional `ITextLineExt` / `ITextParagraphExt` (debug overlay); `ITextResult` is `Paragraphs` + `FullText` only.

## [1.3.0.0] - 2026-08-15

### Added

- **OneOcr / WindowsMediaOcr / ProximityTextLayout `1.3.0.0`:** constructors take `ILoggingWrapper`; sessions (and nested OneOcr/WindowsMedia engines and Proximity layout services) are created via `Wrap`.
- **`plugin.json`:** `entryPoint` — fully qualified service type (`Zaya.OCR.Impl.OneOcr.OneOcrService`, `…WindowsMediaOcrService`, `…ProximityTextLayoutService`).

### Changed

- **Interface channel `1.2` → `1.3`** (host must match exactly). Plugin update tag: `plugin-Zaya.OCR-v1.3-latest`.
- Reorganized Impl projects (`Constants/`, `Exceptions/`, `Extensions/`, `Models/`, `Services/` + `Services/Impl/`); public service types live in the root Impl namespace (e.g. `Zaya.OCR.Impl.OneOcr.OneOcrService`).
- Abstractions do not depend on Zaya.Logging, so `IOCRService` / `ITextLayoutService` can be used as generic type arguments (DI: `AddSingleton<IOCRService, …>`).
- Impl packages reference NuGet `Zaya.Logging` **1.0.0**.
- **OneOcr:** settings descriptors extracted; exceptions split one-per-file; `OneOcrSource` / `OneOcrConfig` split; `minConfidence` default documented as `70`.
- **WindowsMediaOcr:** same layout as OneOcr; `IWindowsMediaOcrEngine` wraps WinRT `OcrEngine`.
- **ProximityTextLayout:** settings descriptors extracted; pipeline helpers behind interfaces and wrapped for logging.

## [1.2.0.3] - 2026-08-12

### Changed

- **ProximityTextLayout `1.2.0.3`:** unify setting description units (percent / degrees / frames) and shorten previous-line search (along) copy.

## [1.2.0.2] - 2026-08-11

### Changed

- **ProximityTextLayout `1.2.0.2`:** replace `leftEdgeAlignmentTolerance`, `firstLineIndentTolerance`, `enableCenterAlignment`, and `maxLineProtrusionPercent` with a single `lineOverhangTolerancePercent` (default `100`). Lines merge into a paragraph when at least one lies within the other along reading, allowing overhang up to that percent of line height.
- Show previous-line search and merge hysteresis even when stabilization is off; hide only hold / text-match / ghost settings under `enableStabilization`.
- GitHub Release notes are taken from this file’s `[Unreleased]` section (manual changelog input removed from Publish).
