# Zaya.OCR

Pluggable OCR and text-layout abstractions for the Zaya ecosystem — engines expose metadata and `SettingDescriptor`s, hosts pass settings into `CreateSessionAsync`.

## Packages

| Package | Version | Role |
|---------|---------|------|
| **Zaya.OCR** | 2.0.0 | Abstractions: `IOCRService`, `IOCRSession`, `ITextLayoutService`; debug Ext for layout tracking |
| **Zaya.OCR.Impl.OneOcr** | 2.0.0.0 | Windows OneOCR (`oneocr.dll` via P/Invoke; no WinRT / App SDK identity) |
| **Zaya.OCR.Impl.WindowsMediaOcr** | 2.0.0.0 | Official `Windows.Media.Ocr` WinRT API (Windows 10+; typically needs MSIX identity) |
| **Zaya.OCR.Impl.ProximityTextLayout** | 2.0.0.0 | Merges OCR words into lines/paragraphs; optional stabilization, merge hysteresis, and word/line/paragraph filters |

Requires [Zaya.Primitives](https://github.com/shtrasser-dev/Zaya.Primitives) **2.0.0**. Impl packages depend on [Zaya.Logging](https://github.com/shtrasser-dev/Zaya.Logging) **1.0.0**. Update channel for plugins: `plugin-Zaya.OCR-v2.0-latest`. See [versioning](docs/versioning.md) and [CHANGELOG](CHANGELOG.md).

Docs: [API & articles](https://shtrasser-dev.github.io/Zaya.OCR)

## Features

- **IOCRService** — engine id, localized name/description, `Settings`, `PreferredPixelFormat`, `CreateSessionAsync`
- **IOCRSession** — `RecognizeAsync(IRawImage)` → `IOCRResult` (words + confidence) from [Zaya.Primitives.OCR](https://github.com/shtrasser-dev/Zaya.Primitives)
- **ITextLayoutService** / **ITextLayoutSession** — structure OCR words into paragraphs/lines (`ITextResult`: `Paragraphs` + `FullText`)
- **ITextLineExt** / **ITextParagraphExt** — optional tracking/ghost metadata for debug overlay (`as ITextLineExt` / `as ITextParagraphExt`)
- **SettingDescriptor** — typed UI settings from `Zaya.Primitives.Settings`
- Failures surface as `LocalizedException` for host UI
- Impl constructors take `ILoggingWrapper` (parameterless uses `EmptyLoggingWrapper`) and wrap sessions / nested services for Trace/Debug logging
- Plugin zips include `plugin.json` with `entryPoint` (e.g. `Zaya.OCR.Impl.OneOcr.OneOcrService`)

There is no separate `InitializeAsync` / `OcrEngineProvider`: create a session with defaults or an explicit settings dictionary.

## Installation

```xml
<PackageReference Include="Zaya.OCR" Version="2.0.0" />
<PackageReference Include="Zaya.OCR.Impl.OneOcr" Version="2.0.0.0" />
<!-- optional -->
<PackageReference Include="Zaya.OCR.Impl.WindowsMediaOcr" Version="2.0.0.0" />
<PackageReference Include="Zaya.OCR.Impl.ProximityTextLayout" Version="2.0.0.0" />
```

`Zaya.Logging` is pulled transitively by the Impl packages.

Plugin zips for ScreenTranslator hosts (stable names) from GitHub Releases (`plugin-Zaya.OCR-v2.0-latest`):

- `Zaya.OCR.Impl.OneOcr.zip`
- `Zaya.OCR.Impl.WindowsMediaOcr.zip`
- `Zaya.OCR.Impl.ProximityTextLayout.zip`

## Quick start (OneOCR)

```csharp
using System.Drawing;
using Zaya.OCR.Impl.OneOcr;
using Zaya.OCR.Services;

using var ocr = new OneOcrService(); // or new OneOcrService(logging)

using var session = await ocr.CreateSessionAsync(new Dictionary<string, object>
{
    ["source"] = "auto",          // SnippingTool, then download URL fallback
    ["minConfidence"] = 40,       // 0–100
});

using var bitmap = new Bitmap(@"C:\screenshot.png");
var result = await session.RecognizeAsync(bitmap); // Bitmap extension in Impl.OneOcr

foreach (var word in result.Words)
    Console.WriteLine($"'{word.Text}' at {word.Bounds} ({word.Confidence:P0})");
```

Typed settings helper:

```csharp
var config = new OneOcrConfig
{
    Source = OneOcrSource.Auto,
    MinConfidence = 40,
};
using var session = await ocr.CreateSessionAsync(
    config.ToDictionary()
        .Where(kv => kv.Value is not null)
        .ToDictionary(kv => kv.Key, kv => kv.Value!));
```

Or DI:

```csharp
services.AddOneOcr();
// services.AddWindowsMediaOcr();
// services.AddProximityTextLayout();
```

## Engine lifecycle

```
Resolve IOCRService (new / DI / plugin host)
  → Read DisplayName / Description / Settings (build UI)
  → CreateSessionAsync(settings)   // or CreateSessionAsync() for defaults
  → RecognizeAsync(IRawImage | Bitmap)
  → Dispose session / service
```

Session creation (download, missing DLL, etc.) throws `LocalizedException` subclasses such as `OneOcrSnippingToolNotFoundException`, `OneOcrDllNotFoundException`.

## OneOCR settings (`EngineId`: `oneocr`)

| Key | Default | Notes |
|-----|---------|--------|
| `source` | `auto` | `auto` \| `snippingtool` \| `directory` \| `url` |
| `directoryPath` | — | Required when `source` = `directory` |
| `downloadUrl` | [Zaya.External OneOCR.zip](https://github.com/shtrasser-dev/Zaya.External/releases/latest/download/OneOCR.zip) | Used for `url` and as `auto` fallback |
| `cacheDirectory` | `%TEMP%\Zaya\OneOcr` | Shared cache for `auto` / `snippingtool` / `url`. If it already has `oneocr.dll`, `onnxruntime.dll`, and `oneocr.onemodel`, those files are used as-is (no SnippingTool lookup, no download). |
| `minConfidence` | `70` | Drop words below this percent (0–100) |

`source = auto`: use complete cache if present; else try SnippingTool; if not found, download via `downloadUrl`.

Full details: [docs/articles/oneocr-settings.md](docs/articles/oneocr-settings.md)

## Windows Media OCR settings (`EngineId`: `windows-media-ocr`)

| Key | Default | Notes |
|-----|---------|--------|
| `language` | `auto` | `auto` uses user profile languages; otherwise a BCP-47 OCR language tag |

Requires Windows 10+, OCR language packs, and typically MSIX package identity for desktop hosts. Details: [docs/articles/windows-media-ocr-settings.md](docs/articles/windows-media-ocr-settings.md)

## Input formats

| Source | API |
|--------|-----|
| `IRawImage` | `session.RecognizeAsync(rawImage)` — preferred (`PreferredPixelFormat` is BGRA32 for OneOCR) |
| `System.Drawing.Bitmap` | `session.RecognizeAsync(bitmap)` — extension in `Zaya.OCR.Impl.OneOcr` (`LockBits` → BGRA) |

## Proximity Text Layout settings (`EngineId`: `proximity-text-layout`)

Merges OCR words into lines/paragraphs by proximity heuristics. Optional **filters** (word / line / paragraph) run before layout finalization. Optional **stabilization** (`enableStabilization`) snaps line bounds to the previous frame, smooths text flicker, and keeps unmatched paragraphs as ghosts. Debug overlay can read match/ghost fields via `ITextLineExt` / `ITextParagraphExt` (e.g. `IsGhost`, `PreviousFrameText`).

```csharp
using Zaya.OCR.Impl.ProximityTextLayout;

using var layout = new ProximityTextLayoutService();
using var layoutSession = await layout.CreateSessionAsync(new Dictionary<string, object>
{
    ["enableStabilization"] = true,
    ["centerThresholdXPercent"] = 300,
    ["centerThresholdYPercent"] = 75,
});
var structured = await layoutSession.ProcessAsync(ocrResult);
```

### Layout heuristics

Integer thresholds are stored as ints and applied as `/100` at runtime when they are percentages. Units are stated in each setting description.

| Key | Default | Notes |
|-----|---------|--------|
| `wordGapThreshold` | `50` | Max gap along the baseline between words to merge into a line, in percent of word height; also snap tolerance for line ends vs the previous frame |
| `baselineDriftTolerance` | `50` | Max drift of word centers perpendicular to reading direction to merge into a line, in percent of word height |
| `angleToleranceDegrees` | `10` | Max angle difference for merging words into a line, lines into a paragraph, and matching a previous-frame line, in degrees |
| `lineSpacingThreshold` | `150` | Max center-to-center distance along the paragraph normal to merge lines into a paragraph, in percent of average line height |
| `lineOverhangTolerancePercent` | `100` | Merge lines if at least one stays within the other along reading; max overhang in percent of line height |
| `fontSizeTolerance` | `50` | Max height difference between lines to still merge into one paragraph, in percent of average line height |
| `verticalColumns` | `false` | Experimental manga mode: relabel upright CJK / punctuation / digits / square numbers so reading direction is top-to-bottom (columns assemble downward, right-to-left) |
| `wordFilters` | _(empty table)_ | Word-level filter rules (see Filters below) |
| `lineFilters` | _(empty table)_ | Line-level filter rules |
| `paragraphFilters` | _(empty table)_ | Paragraph-level filter rules |

### Filters (tables)

Each of `wordFilters`, `lineFilters`, and `paragraphFilters` is a table of rules applied at that stage (words → lines → paragraphs → stabilization). Case is always ignored.

| Column | Notes |
|--------|--------|
| `enabled` | Enable/disable the rule |
| `pattern` | Literal (full-string equality) or regex |
| `isRegex` | When `false`, require exact match of the whole word/line/paragraph text; when `true`, regex `IsMatch` / `Replace` |
| `action` | `Skip` — drop the whole block; `Strip` — remove the match and keep the rest (empty → drop) |
| `description` | Optional note for UI |

### Stabilization (across frames)

| Key | Default | Notes |
|-----|---------|--------|
| `enableStabilization` | `true` | Snap line bounds to the previous frame, smooth text flicker, and keep unmatched paragraphs as ghosts |
| `holdNewBlocks` | `false` | Hold a paragraph until its original text matches the previous frame, or until the previous paragraph was already shown and the normalized text is similar (within `levenshteinThreshold`) |
| `centerThresholdXPercent` | `300` | How far past the previous line ends to still match a word, in percent of line height |
| `centerThresholdYPercent` | `75` | How far off the previous baseline to still match a word, in percent of line height |
| `levenshteinThreshold` | `8` | Max Levenshtein distance to treat linked readings as the same (then longer text wins, equal length keeps previous), in percent of the longer compare-key |
| `ghostMaxFrames` | `3` | How long to keep an unmatched previous paragraph visible, in frames (`0` disables ghosts) |
| `paragraphMergeHysteresisPercent` | `120` | Scale factor that loosens or tightens merge tolerances to prefer the previous frame structure, in percent of the base tolerances |
| `sameLineWordGapHysteresisPercent` | `600` | How far along the baseline to pull words that shared a previous-frame line, in percent of the word gap threshold |

`holdNewBlocks`, `levenshteinThreshold`, and `ghostMaxFrames` are visible in UI only when `enableStabilization` is `true`. Tracking settings (`centerThreshold*`, merge/word-gap hysteresis) stay visible because line/paragraph matching always runs.

## Requirements

- **Zaya.OCR** — .NET 8+
- **OneOCR** — Windows 10 build ≥ 22000 (Windows 11), x64; native `oneocr.dll` + model from SnippingTool, local folder, or download URL
- **Windows Media OCR** — Windows 10+; WinRT `Windows.Media.Ocr`; OCR language packs; desktop apps usually need package identity (MSIX)

## Ecosystem

- **Zaya.Primitives** — `IRawImage`, `PixelFormat`, `LocalizedString`, `LocalizedException`, `BoundingBox`, `Zaya.Primitives.OCR.*`, `Zaya.Primitives.Settings.*` (**2.0.0**)
- **Zaya.Logging** — `ILoggingWrapper` / `EmptyLoggingWrapper` for Impl constructors and session wrapping (**1.0.0**)
- **Zaya.ScreenTranslator** — host that loads OCR / layout plugins and binds settings in UI

## License

MIT
