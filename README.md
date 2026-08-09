# Zaya.OCR

Pluggable OCR and text-layout abstractions for the Zaya ecosystem — engines expose metadata and `SettingDescriptor`s, hosts pass settings into `CreateSessionAsync`.

## Packages

| Package | Version | Role |
|---------|---------|------|
| **Zaya.OCR** | 1.2.0 | Abstractions: `IOCRService`, `IOCRSession`, `ITextLayoutService`, result models |
| **Zaya.OCR.Impl.OneOcr** | 1.2.0.0 | Windows OneOCR (`oneocr.dll` via P/Invoke; no WinRT / App SDK identity) |
| **Zaya.OCR.Impl.WindowsMediaOcr** | 1.2.0.0 | Official `Windows.Media.Ocr` WinRT API (Windows 10+; typically needs MSIX identity) |
| **Zaya.OCR.Impl.ProximityTextLayout** | 1.2.0.0 | Merges OCR words into lines/paragraphs; optional stabilization, merge hysteresis, and word/line/paragraph filters |

Requires [Zaya.Primitives](https://github.com/shtrasser-dev/Zaya.Primitives) **1.0.0**. Update channel for plugins: `plugin-Zaya.OCR-v1.2-latest`. See [versioning](docs/versioning.md).

Docs: [API & articles](https://shtrasser-dev.github.io/Zaya.OCR)

## Features

- **IOCRService** — engine id, localized name/description, `Settings`, `PreferredPixelFormat`, `CreateSessionAsync`
- **IOCRSession** — `RecognizeAsync(IRawImage)` → `IOCRResult` (words + confidence)
- **ITextLayoutService** / **ITextLayoutSession** — structure OCR words into paragraphs/lines with stable `Id`, previous-frame match flags/ages, and ghost metadata
- **SettingDescriptor** — typed UI settings (enum, URL, paths, ints, …) from [Zaya.Primitives](https://github.com/shtrasser-dev/Zaya.Primitives)
- Failures surface as `LocalizedException` for host UI

There is no separate `InitializeAsync` / `OcrEngineProvider`: create a session with defaults or an explicit settings dictionary.

## Installation

```xml
<PackageReference Include="Zaya.OCR" Version="1.2.0" />
<PackageReference Include="Zaya.OCR.Impl.OneOcr" Version="1.2.0.0" />
<!-- optional -->
<PackageReference Include="Zaya.OCR.Impl.WindowsMediaOcr" Version="1.2.0.0" />
<PackageReference Include="Zaya.OCR.Impl.ProximityTextLayout" Version="1.2.0.0" />
```

Plugin zips for ScreenTranslator hosts (stable names) from GitHub Releases (`plugin-Zaya.OCR-v1.2-latest`):

- `Zaya.OCR.Impl.OneOcr.zip`
- `Zaya.OCR.Impl.WindowsMediaOcr.zip`
- `Zaya.OCR.Impl.ProximityTextLayout.zip`

## Quick start (OneOCR)

```csharp
using System.Drawing;
using Zaya.OCR.Impl.OneOcr;
using Zaya.OCR.Impl.OneOcr.Services;
using Zaya.OCR.Services;

using var ocr = new OneOcrService();

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

Merges OCR words into lines/paragraphs by proximity heuristics. Optional **filters** (word / line / paragraph) run before stabilization. Optional **stabilization** matches lines to the previous frame (stable `Id`, snap geometry, smooth text flicker, ghosts via `IsGhost` / `GhostAge`) and can hold new paragraphs until their text settles. Hosts can compare `Text` to `PreviousFrameText` when they need the old case-insensitive text-equality signal.

```csharp
using Zaya.OCR.Impl.ProximityTextLayout.Services;

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

Integer thresholds are percent of word/line height (stored as ints, applied as `/100` at runtime), except `angleToleranceDegrees` and `levenshteinThreshold` / `ghostMaxFrames` which are absolute.

| Key | Default | Notes |
|-----|---------|--------|
| `wordGapThreshold` | `50` | Max gap along the baseline between words to merge into a line; also snap tolerance for line ends vs the previous frame |
| `baselineDriftTolerance` | `50` | Max drift of word centers perpendicular to reading direction to merge into a line |
| `angleToleranceDegrees` | `10` | Max angle difference for merging words into a line, lines into a paragraph, and matching a previous-frame line |
| `lineSpacingThreshold` | `150` | Max center-to-center distance along the paragraph normal to merge lines into a paragraph |
| `leftEdgeAlignmentTolerance` | `100` | Max offset of left edges to merge into a paragraph |
| `firstLineIndentTolerance` | `300` | Max extra indentation of the first line |
| `fontSizeTolerance` | `50` | Max allowed height difference between lines to still merge them into one paragraph |
| `enableCenterAlignment` | `false` | Also merge lines if their centers align along the reading direction |
| `maxLineProtrusionPercent` | `10` | When merging lines into a paragraph: each line may stick out past the shared overlap by at most this % of its length |
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
| `enableStabilization` | `true` | Match lines/paragraphs to the previous frame: snap geometry, smooth text flicker, and keep unmatched previous paragraphs as ghosts |
| `holdNewBlocks` | `false` | Hold a paragraph until its original text matches the previous frame, or until the previous paragraph was already shown and the normalized text is similar (within `levenshteinThreshold`) |
| `centerThresholdXPercent` | `300` | How far beyond the previous line ends to still match a word (`300` = 3× line height) |
| `centerThresholdYPercent` | `75` | How far off the previous baseline to still match a word (`75` = 0.75× line height) |
| `levenshteinThreshold` | `8` | Max Levenshtein distance (% of longer compare-key) to treat linked readings as the same; then longer text wins, equal length keeps previous |
| `ghostMaxFrames` | `3` | How many frames to keep an unmatched previous paragraph visible (`0` = disable ghosts) |
| `paragraphMergeHysteresisPercent` | `120` | Loosen/tighten merge tolerances to prefer the previous frame’s line and paragraph structure (`100` = off, `120` = ×1.2 when preferring merge, ×1/1.2 when preferring split) |
| `sameLineWordGapHysteresisPercent` | `600` | How far along the baseline to pull words that shared a previous-frame line (`100` = normal gap, `600` = 6×; bridges dropped tokens) |

`holdNewBlocks`, `centerThresholdXPercent`, `centerThresholdYPercent`, `levenshteinThreshold`, `ghostMaxFrames`, `paragraphMergeHysteresisPercent`, and `sameLineWordGapHysteresisPercent` are visible in UI only when `enableStabilization` is `true`.

## Requirements

- **Zaya.OCR** — .NET 8+
- **OneOCR** — Windows 10 build ≥ 22000 (Windows 11), x64; native `oneocr.dll` + model from SnippingTool, local folder, or download URL
- **Windows Media OCR** — Windows 10+; WinRT `Windows.Media.Ocr`; OCR language packs; desktop apps usually need package identity (MSIX)

## Ecosystem

- **Zaya.Primitives** — `IRawImage`, `PixelFormat`, `LocalizedString`, `SettingDescriptor`, `LocalizedException`
- **Zaya.ScreenTranslator** — host that loads OCR / layout plugins and binds settings in UI

## License

MIT
