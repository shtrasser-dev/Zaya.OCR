# Getting Started

## Overview

Zaya.OCR provides a set of interfaces for optical character recognition in .NET 8.0+. It follows the dependency inversion principle: consumers depend on abstractions (`IOCRService`, `IOCRResult`, `IOCRWord`), and implementations are provided separately.

## Architecture

| Interface | Role |
|---|---|
| `IOCRService` | Entry point — accepts raw image bytes (`byte[]`), returns `IOCRResult` |
| `IOCRResult` | Aggregate result — read-only list of `IOCRWord` plus overall confidence |
| `IOCRWord` | Individual word — recognized text, pixel bounding box, per-word confidence |

## Basic Usage

```csharp
using Zaya.OCR.Models;
using Zaya.OCR.Services;

// Obtain an implementation of IOCRService (e.g., from DI container)
IOCRService ocr = /* your implementation */;
byte[] imageBytes = File.ReadAllBytes("document.png");

var result = await ocr.RecognizeAsync(imageBytes);

Console.WriteLine($"Overall confidence: {result.Confidence:P0}");
Console.WriteLine($"Words found: {result.Words.Count}");

foreach (var word in result.Words)
{
    Console.WriteLine($"  '{word.Text}' — bounds: {word.Bounds}, confidence: {word.Confidence:P0}");
}
```

## Cancellation

`RecognizeAsync` accepts an optional `CancellationToken`:

```csharp
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
var result = await ocr.RecognizeAsync(imageBytes, cts.Token);
```

## Implementing a Custom Service

```csharp
public class MyOCRService : IOCRService
{
    public async Task<IOCRResult> RecognizeAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        // Custom OCR logic here
        return new MyOCRResult(words);
    }
}
```
