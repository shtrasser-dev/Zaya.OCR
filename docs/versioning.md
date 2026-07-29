# Versioning (Zaya.OCR)

Three independent axes — do not bump them together unless required.

| Axis | Source | Example |
|------|--------|---------|
| **primitivesChannel** | `ZayaPrimitivesVersion` → `MAJOR.MINOR` | `0.4` |
| **interfaceVersion** | Version of package **Zaya.OCR** (abstractions) | `0.4.1` |
| **pluginVersion** | Version of each **Impl** csproj | OneOcr `0.4.1`, others may differ |

- Host must ship the same **Zaya.OCR** assembly version as `interfaceVersion` in the zip.
- Bugfixes in one engine: raise only that Impl’s `<Version>`; leave abstractions unchanged.
- GitHub floating tag: `plugin-v{primitivesChannel}-latest` (e.g. `plugin-v0.4-latest`).
- Immutable tag: `plugin-v{maxPluginVersion}` among assets in the release.

## plugin.json

```json
{
  "id": "OneOcr",
  "type": "ocr",
  "interface": "Zaya.OCR",
  "interfaceVersion": "0.4.1",
  "pluginVersion": "0.4.1",
  "primitivesChannel": "0.4"
}
```

Release body lists per-asset versions (`Zaya.OCR.Impl.OneOcr.zip=0.4.1`) for the host updater.

## Bumping

1. Abstractions / host contract: edit default `Version` in `Directory.Build.props`, then update ScreenTranslator.
2. Single engine: set `<Version>` only in that Impl’s `.csproj`.
3. Run `build.cmd` / Publish workflow.
