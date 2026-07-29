# Windows Media OCR settings

`EngineId`: **`windows-media-ocr`**

Uses the official WinRT API [`Windows.Media.Ocr.OcrEngine`](https://learn.microsoft.com/en-us/uwp/api/windows.media.ocr.ocrengine).

## Requirements

- Windows 10 or later
- At least one OCR language pack installed (Windows Settings → Time & language → Language & region)
- Desktop hosts typically need **package identity** (MSIX). Unpackaged Win32 apps may fail to create the engine.

## Settings

| Key | Default | Notes |
|-----|---------|--------|
| `language` | `auto` | `auto` → `OcrEngine.TryCreateFromUserProfileLanguages()`; otherwise a BCP-47 tag for `TryCreateFromLanguage` |

When available, the language dropdown lists installed OCR recognizer languages from `OcrEngine.AvailableRecognizerLanguages`. If that query fails, it falls back to `Languages.All` from Zaya.Primitives.

## Notes

- Preferred input is **BGRA32** (`IRawImage` / `Bitmap` extension).
- WinRT `OcrWord` has no confidence score; words are reported with confidence `1.0`.
- This is **not** the same engine as OneOCR (`oneocr.dll` from Snipping Tool).
