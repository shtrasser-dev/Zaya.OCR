# Zaya.OCR

Pluggable OCR abstractions for the Zaya ecosystem — zero implementation, pure contracts.

## Features

- **IOCRService** — async entry point: accepts raw image bytes, returns recognized result
- **IOCRResult** — aggregate result: word list with per-word confidence
- **IOCRWord** — individual word: recognized text, pixel bounding box, confidence score

## Installation

```xml
<PackageReference Include="Zaya.OCR" Version="0.1.0" />
```

## Quick Start

```csharp
using Zaya.OCR.Models;
using Zaya.OCR.Services;

// IOCRService is an interface — plug in any implementation:
IOCRService ocr = /* your implementation */;

var result = await ocr.RecognizeAsync(imageBytes);

foreach (var word in result.Words)
{
    Console.WriteLine($"'{word.Text}' at {word.Bounds} ({word.Confidence:P0})");
}
```

## Ecosystem

- **Zaya.Primitives** — shared types (PixelFormat, geometry)
- **Zaya.OCR.Tesseract** — Tesseract-based implementation (coming soon)
