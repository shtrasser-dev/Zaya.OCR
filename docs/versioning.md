# Versioning (Zaya.OCR)

Aligned with the ScreenTranslator updater plan and **Zaya.Primitives** compatibility channel.

## Versions

| Artifact | Rule | Example |
|----------|------|---------|
| `Zaya.Primitives` | `MAJOR.MINOR.0` | `0.4.0` |
| This repo (`Directory.Build.props`) | Same `MAJOR.MINOR`, own `PATCH` | `0.4.0` |
| GitHub Release (immutable) | `plugin-v{MAJOR.MINOR.PATCH}` | `plugin-v0.4.0` |
| GitHub Release (floating) | `plugin-v{MAJOR.MINOR}-latest` | `plugin-v0.4-latest` |

All NuGet packages and plugin zips use the same `Version` from [`Directory.Build.props`](../Directory.Build.props).

## Plugin zips

Stable names (no version in the filename), multi-asset release:

- `Zaya.OCR.Impl.OneOcr.zip`
- `Zaya.OCR.Impl.WindowsMediaOcr.zip`
- `Zaya.OCR.Impl.ProximityTextLayout.zip`

Each contains `plugin.json`:

```json
{
  "id": "OneOcr",
  "type": "ocr",
  "interface": "Zaya.OCR",
  "interfaceVersion": "0.4.0",
  "pluginVersion": "0.4.0",
  "primitivesChannel": "0.4"
}
```

Hosts discover updates via:

`GET /repos/shtrasser-dev/Zaya.OCR/releases/tags/plugin-v0.4-latest`

Release `name` is `Plugin v0.4.0` (semver for comparison without downloading the zip).

## Bumping

1. Edit `Version` / `ZayaPrimitivesVersion` in `Directory.Build.props`.
2. Run `build.cmd` (or publish workflow).
3. CI creates `plugin-v{ver}` and refreshes `plugin-v{channel}-latest`.
