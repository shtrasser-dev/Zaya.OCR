# Zaya.OCR

Pluggable OCR abstractions for the Zaya ecosystem — engine discovery, localized settings, explicit initialization.

## Features

- **IOCRService** — engine entry point: metadata, settings descriptors, explicit `InitializeAsync`, session factory
- **IOCRSession** — per-recognition session: accepts `IRawImage` (raw pixels), returns recognized result
- **IOCRResult** — aggregate result: word list with per-word confidence
- **IOCRWord** — individual word: recognized text, pixel bounding box, confidence score
- **OcrOptions** — per-session configuration: language, word-level confidence
- **OcrEngineProvider** — auto-discovers all `IOCRService` implementations via reflection + plugin directories

## Installation

```xml
<PackageReference Include="Zaya.OCR" Version="0.2.0" />
```

## Quick Start

```csharp
using Zaya.OCR.Models;
using Zaya.OCR.Services;

// Discover engines
var provider = new OcrEngineProvider(new[] { @"C:\plugins" });
var engineInfo = provider.GetById("oneocr")!;

// Read metadata and settings for UI
var name = engineInfo.Service.DisplayName.GetValue(CultureInfo.CurrentUICulture);
var settings = engineInfo.Service.Settings; // IReadOnlyList<SettingDescriptor>

// Initialize with engine-specific settings
var config = new Dictionary<string, object?>
{
    ["source"] = "snippingtool",
    ["cacheDirectory"] = @"C:\ocr_cache"
};
await engineInfo.Service.InitializeAsync(config);

// Create a session and recognize from an IRawImage (e.g. a captured frame)
using var session = await engineInfo.Service.CreateSessionAsync();
var result = await session.RecognizeAsync(rawImage);

// Or recognize from a System.Drawing.Bitmap (requires Zaya.OCR.Impl.OneOcr)
using var bitmap = new Bitmap(@"C:\screenshot.png");
result = await session.RecognizeAsync(bitmap); // extension method

foreach (var word in result.Words)
{
    Console.WriteLine($"'{word.Text}' at {word.Bounds} ({word.Confidence:P0})");
}
```

## Engine Lifecycle

```
Discover (OcrEngineProvider)
   → Read DisplayName / Description / Settings
   → Show UI, user configures
   → InitializeAsync(Dictionary)  ← explicit, no magic static init
   → IsAvailable == true
   → CreateSessionAsync → RecognizeAsync
```

`InitializeAsync` throws `LocalizedException` on failure — host catches and shows localized error to user.

## Input formats

| Source | How to pass |
|--------|-------------|
| `IRawImage` | `session.RecognizeAsync(rawImage)` — direct pixel buffer, no copy |
| `System.Drawing.Bitmap` | `session.RecognizeAsync(bitmap)` — extension method in `Zaya.OCR.Impl.OneOcr`, converts to BGRA |

The `Bitmap` extension converts via `LockBits` — one copy, no intermediate PNG encoding.

## Implementations

- **Zaya.OCR.Impl.OneOcr** — Windows 11 OneOCR engine (SnippingTool P/Invoke), no WinRT dependency

## Ecosystem

- **Zaya.Primitives** — shared types (PixelFormat, LocalizedString, SettingDescriptor, LocalizedException)
- *Zaya.OCR.Tesseract* — Tesseract-based implementation (coming soon)

## License

MIT
