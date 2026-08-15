# OneOCR settings

`OneOcrService` (`EngineId`: `oneocr`) exposes engine settings through
[`SettingDescriptor`](xref:Zaya.Primitives.SettingDescriptor) for UI hosts,
and accepts the same keys as a dictionary in
[`CreateSessionAsync`](xref:Zaya.OCR.Impl.OneOcr.OneOcrService.CreateSessionAsync(System.Collections.Generic.IReadOnlyDictionary{System.String,System.Object},System.Threading.CancellationToken)).

Typed helper: [`OneOcrConfig`](xref:Zaya.OCR.Impl.OneOcr.OneOcrConfig) → `ToDictionary()`.

## Keys

| Key | Type | Default | Required when | Description |
|-----|------|---------|---------------|-------------|
| `source` | enum string | `auto` | always | Where to load `oneocr.dll` / model from |
| `directoryPath` | directory path | — | `source` = `directory` | Folder with `oneocr.dll`, `onnxruntime.dll`, `oneocr.onemodel` |
| `downloadUrl` | URL | [Zaya.External OneOCR.zip](https://github.com/shtrasser-dev/Zaya.External/releases/latest/download/OneOCR.zip) | `source` = `url` | Zip package URL; also used as `auto` fallback |
| `cacheDirectory` | directory path | `%TEMP%\Zaya\OneOcr` | — | Writable cache for copy/extract (`auto` / `snippingtool` / `url`) |
| `minConfidence` | int 0–100 | `70` | — | Drop words with confidence below this percent |

## `source` values

| Value | Behavior |
|-------|----------|
| `auto` | Use cache if complete; else try SnippingTool; on [`OneOcrSnippingToolNotFoundException`](xref:Zaya.OCR.Impl.OneOcr.OneOcrSnippingToolNotFoundException) download via `downloadUrl` |
| `snippingtool` | Use cache if complete; else resolve from Windows 11 SnippingTool and copy into `cacheDirectory` |
| `directory` | Load from `directoryPath` |
| `url` | Use cache if complete; else download zip from `downloadUrl` into `cacheDirectory` |

A cache directory is treated as complete when it contains `oneocr.dll`, `onnxruntime.dll`, and `oneocr.onemodel`.

## Visibility (UI)

Hosts should honor `SettingDescriptor.IsVisible` / `IsRequired`:

- `directoryPath` — visible only when `source` is `directory`
- `downloadUrl` — visible and required only when `source` is `url` (still used as `auto` fallback when needed)
- `cacheDirectory` — visible for `auto`, `snippingtool`, and `url`

Some hosts (for example ScreenTranslator) treat `cacheDirectory` as host-managed and inject a path under `%AppData%` instead of showing the field.

## Example

```csharp
using Zaya.OCR.Impl.OneOcr;
using Zaya.OCR.Impl.OneOcr;

using var ocr = new OneOcrService();

// Dictionary form
using var session = await ocr.CreateSessionAsync(new Dictionary<string, object>
{
    ["source"] = "auto",
    ["minConfidence"] = 40,
});

// Or typed config
var config = new OneOcrConfig
{
    Source = OneOcrSource.Directory,
    DirectoryPath = @"C:\tools\oneocr",
    MinConfidence = 40,
};
var settings = config.ToDictionary()
    .Where(kv => kv.Value is not null)
    .ToDictionary(kv => kv.Key, kv => kv.Value!);
using var session2 = await ocr.CreateSessionAsync(settings);
```

See also API remarks on [`OneOcrService`](xref:Zaya.OCR.Impl.OneOcr.OneOcrService).
