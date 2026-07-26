# Getting Started

## Overview

Zaya.OCR provides interfaces for optical character recognition in .NET 8.0+. Consumers depend on abstractions (`IOCRService`, `IOCRSession`, `IOCRResult`, `IOCRWord`); implementations such as OneOCR are separate packages.

## Architecture

| Interface | Role |
|---|---|
| `IOCRService` | Engine metadata + settings; creates sessions via `CreateSessionAsync` |
| `IOCRSession` | Active recognition session — `RecognizeAsync(IRawImage)` → `IOCRResult` |
| `IOCRResult` | Aggregate result — read-only list of `IOCRWord` plus overall confidence |
| `IOCRWord` | Individual word — recognized text, pixel bounding box, per-word confidence |
| `ITextLayoutService` | Optional layout engine — structures OCR words into paragraphs/lines |

## Basic Usage (OneOCR)

```csharp
using System.Drawing;
using Zaya.OCR.Impl.OneOcr.Services;
using Zaya.OCR.Services;

using var ocr = new OneOcrService();

using var session = await ocr.CreateSessionAsync(new Dictionary<string, object>
{
    ["source"] = "auto",          // SnippingTool, then download URL fallback
    ["minConfidence"] = 40,       // 0–100
});

using var bitmap = new Bitmap("document.png");
var result = await session.RecognizeAsync(bitmap);

Console.WriteLine($"Overall confidence: {result.Confidence:P0}");
Console.WriteLine($"Words found: {result.Words.Count}");

foreach (var word in result.Words)
{
    Console.WriteLine($"  '{word.Text}' — bounds: {word.Bounds}, confidence: {word.Confidence:P0}");
}
```

Each `CreateSessionAsync` call builds a new engine from the supplied settings (or descriptor defaults). Dispose the session when finished; it owns the native engine.

## Defaults and DI

```csharp
// Defaults from SettingDescriptor list (source = auto, …)
using var session = await ocr.CreateSessionAsync();

// Or register in DI
services.AddOneOcr();
// services.AddProximityTextLayout();
```

Typed settings helper:

```csharp
using Zaya.OCR.Impl.OneOcr;

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

## Input formats

| Source | API |
|--------|-----|
| `IRawImage` | `session.RecognizeAsync(rawImage)` — preferred (`PreferredPixelFormat` is BGRA32 for OneOCR) |
| `System.Drawing.Bitmap` | `session.RecognizeAsync(bitmap)` — extension in `Zaya.OCR.Impl.OneOcr` |

## Cancellation

Both session creation and recognition accept an optional `CancellationToken`:

```csharp
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

using var session = await ocr.CreateSessionAsync(settings, cts.Token);
var result = await session.RecognizeAsync(image, cts.Token);
```

## Text layout

```csharp
using Zaya.OCR.Impl.ProximityTextLayout.Services;

using var layout = new ProximityTextLayoutService();
using var layoutSession = await layout.CreateSessionAsync();
var structured = await layoutSession.ProcessAsync(ocrResult);
```

## Implementing a custom service

```csharp
public sealed class MyOCRService : IOCRService
{
    public string EngineId => "my-ocr";
    public LocalizedString DisplayName { get; } = /* … */;
    public LocalizedString Description { get; } = /* … */;
    public bool IsAvailable => true;
    public IReadOnlyList<SettingDescriptor> Settings { get; } = [];
    public PixelFormat PreferredPixelFormat => PixelFormat.Bgra32;

    public Task<IOCRSession> CreateSessionAsync(CancellationToken cancellationToken = default)
        => CreateSessionAsync(new Dictionary<string, object>(), cancellationToken);

    public Task<IOCRSession> CreateSessionAsync(
        IReadOnlyDictionary<string, object> engineSettings,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IOCRSession>(new MyOCRSession(/* apply settings */));

    public void Dispose() { }
}

public sealed class MyOCRSession : IOCRSession
{
    public Task<IOCRResult> RecognizeAsync(IRawImage image, CancellationToken cancellationToken = default)
    {
        // Custom OCR logic here
        return Task.FromResult<IOCRResult>(new MyOCRResult(words));
    }

    public void Dispose() { }
}
```

## Next steps

- **[OneOCR settings](oneocr-settings.md)** — `source`, `downloadUrl`, `cacheDirectory`, and other engine keys
- **API Reference** — complete documentation generated from source code
