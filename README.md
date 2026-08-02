# Zaya.OCR

Pluggable OCR and text-layout abstractions for the Zaya ecosystem — engines expose metadata and `SettingDescriptor`s, hosts pass settings into `CreateSessionAsync`.

## Packages

| Package | Version | Role |
|---------|---------|------|
| **Zaya.OCR** | 1.0.0 | Abstractions: `IOCRService`, `IOCRSession`, `ITextLayoutService`, result models |
| **Zaya.OCR.Impl.OneOcr** | 1.0.0.3 | Windows OneOCR (`oneocr.dll` via P/Invoke; no WinRT / App SDK identity) |
| **Zaya.OCR.Impl.WindowsMediaOcr** | 1.0.0.1 | Official `Windows.Media.Ocr` WinRT API (Windows 10+; typically needs MSIX identity) |
| **Zaya.OCR.Impl.ProximityTextLayout** | 1.0.0.2 | Merges OCR words into lines/paragraphs by proximity heuristics |

Requires [Zaya.Primitives](https://github.com/shtrasser-dev/Zaya.Primitives) **1.0.0**. Update channel for plugins: `plugin-Zaya.OCR-v1.0-latest`. See [versioning](docs/versioning.md).

Docs: [API & articles](https://shtrasser-dev.github.io/Zaya.OCR)

## Features

- **IOCRService** — engine id, localized name/description, `Settings`, `PreferredPixelFormat`, `CreateSessionAsync`
- **IOCRSession** — `RecognizeAsync(IRawImage)` → `IOCRResult` (words + confidence)
- **ITextLayoutService** / **ITextLayoutSession** — structure OCR words into paragraphs/lines
- **SettingDescriptor** — typed UI settings (enum, URL, paths, ints, …) from [Zaya.Primitives](https://github.com/shtrasser-dev/Zaya.Primitives)
- Failures surface as `LocalizedException` for host UI

There is no separate `InitializeAsync` / `OcrEngineProvider`: create a session with defaults or an explicit settings dictionary.

## Installation

```xml
<PackageReference Include="Zaya.OCR" Version="1.0.0" />
<PackageReference Include="Zaya.OCR.Impl.OneOcr" Version="1.0.0.3" />
<!-- optional -->
<PackageReference Include="Zaya.OCR.Impl.WindowsMediaOcr" Version="1.0.0.1" />
<PackageReference Include="Zaya.OCR.Impl.ProximityTextLayout" Version="1.0.0.2" />
```

Plugin zips for ScreenTranslator hosts (stable names) from GitHub Releases (`plugin-Zaya.OCR-v1.0-latest`):

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
| `minConfidence` | `90` | Drop words below this percent (0–100) |

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

Merges OCR words into lines/paragraphs by proximity heuristics. Optional **stabilization** reuses previous-frame paragraphs when OCR flickers (shorter/equal text, bounds jitter).

```csharp
using Zaya.OCR.Impl.ProximityTextLayout.Services;

using var layout = new ProximityTextLayoutService();
using var layoutSession = await layout.CreateSessionAsync(new Dictionary<string, object>
{
    ["enableStabilization"] = true,
    ["centerThresholdPercent"] = 50,
});
var structured = await layoutSession.ProcessAsync(ocrResult);
```

### Layout heuristics

Integer thresholds are percent of word/line height (stored as ints, applied as `/100` at runtime).

| Key | Default | Notes |
|-----|---------|--------|
| `wordGapThreshold` | `50` | Max gap between words to merge into a line |
| `baselineDriftTolerance` | `50` | Max vertical drift of word centers to merge into a line |
| `lineSpacingThreshold` | `150` | Max vertical distance between line centers to merge into a paragraph |
| `leftEdgeAlignmentTolerance` | `100` | Max horizontal offset of left edges to merge into a paragraph |
| `firstLineIndentTolerance` | `300` | Max extra indentation of the first line |
| `fontSizeTolerance` | `50` | Max height difference between lines before splitting paragraphs |
| `enableCenterAlignment` | `false` | Also merge lines if their horizontal centers align |

### Stabilization (across frames)

| Key | Default | Notes |
|-----|---------|--------|
| `enableStabilization` | `true` | Reuse previous paragraphs when OCR flickers |
| `centerThresholdPercent` | `50` | Max paragraph-center drift (% of avg line height) to match previous frame |
| `levenshteinThreshold` | `8` | Max Levenshtein distance (% of longer text) to treat paragraphs as the same |
| `minLength` | `16` | Shorter texts require an exact match to pair with the previous frame |

`centerThresholdPercent`, `levenshteinThreshold`, and `minLength` are visible in UI only when `enableStabilization` is `true`.

## Requirements

- **Zaya.OCR** — .NET 8+
- **OneOCR** — Windows 10 build ≥ 22000 (Windows 11), x64; native `oneocr.dll` + model from SnippingTool, local folder, or download URL
- **Windows Media OCR** — Windows 10+; WinRT `Windows.Media.Ocr`; OCR language packs; desktop apps usually need package identity (MSIX)

## Ecosystem

- **Zaya.Primitives** — `IRawImage`, `PixelFormat`, `LocalizedString`, `SettingDescriptor`, `LocalizedException`
- **Zaya.ScreenTranslator** — host that loads OCR / layout plugins and binds settings in UI

## License

MIT
