# Zaya.OCR

Pluggable OCR and text-layout abstractions for the Zaya ecosystem — engines expose metadata and `SettingDescriptor`s, hosts pass settings into `CreateSessionAsync`.

## Packages

| Package | Version | Role |
|---------|---------|------|
| **Zaya.OCR** | 1.0.0 | Abstractions: `IOCRService`, `IOCRSession`, `ITextLayoutService`, result models |
| **Zaya.OCR.Impl.OneOcr** | 1.0.0.3 | Windows OneOCR (`oneocr.dll` via P/Invoke; no WinRT / App SDK identity) |
| **Zaya.OCR.Impl.WindowsMediaOcr** | 1.0.0.1 | Official `Windows.Media.Ocr` WinRT API (Windows 10+; typically needs MSIX identity) |
| **Zaya.OCR.Impl.ProximityTextLayout** | 1.0.0.2 | Merges OCR words into lines/paragraphs by proximity heuristics |

Requires [Zaya.Primitives](https://github.com/shtrasser-dev/Zaya.Primitives) **1.0.0**. See [versioning](versioning.md).

## Features

- **IOCRService** — engine id, localized name/description, `Settings`, `PreferredPixelFormat`, `CreateSessionAsync`
- **IOCRSession** — `RecognizeAsync(IRawImage)` → `IOCRResult` (words + confidence)
- **ITextLayoutService** / **ITextLayoutSession** — structure OCR words into paragraphs/lines
- Failures surface as `LocalizedException` for host UI

There is no separate `InitializeAsync`: create a session with defaults or an explicit settings dictionary.

## Installation

```xml
<PackageReference Include="Zaya.OCR" Version="1.0.0" />
<PackageReference Include="Zaya.OCR.Impl.OneOcr" Version="1.0.0.3" />
<!-- optional -->
<PackageReference Include="Zaya.OCR.Impl.WindowsMediaOcr" Version="1.0.0.1" />
<PackageReference Include="Zaya.OCR.Impl.ProximityTextLayout" Version="1.0.0.2" />
```

## Quick Start

```csharp
using System.Drawing;
using Zaya.OCR.Impl.OneOcr.Services;
using Zaya.OCR.Services;

using var ocr = new OneOcrService();

using var session = await ocr.CreateSessionAsync(new Dictionary<string, object>
{
    ["source"] = "auto",
    ["minConfidence"] = 40,
});

using var bitmap = new Bitmap(@"C:\screenshot.png");
var result = await session.RecognizeAsync(bitmap); // Bitmap extension in Impl.OneOcr

foreach (var word in result.Words)
    Console.WriteLine($"'{word.Text}' at {word.Bounds} ({word.Confidence:P0})");
```

## Next Steps

- **[Getting Started](articles/getting-started.md)** — detailed usage guide
- **[OneOCR settings](articles/oneocr-settings.md)** — `source`, `downloadUrl`, `cacheDirectory`, and other engine keys
- **[API Reference](xref:Zaya.OCR.Services)** — complete API documentation generated from source code
