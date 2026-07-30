# Versioning (Zaya.OCR)

| Axis | Source | Example |
|------|--------|---------|
| **ZayaPrimitivesVersion** | NuGet pin in `Directory.Build.props` (supplies **Major**) | `1.0.0` |
| **interfaceVersion** | `Zaya.OCR` → `ZayaVersionMinor` / `ZayaVersionPatch` | `1.0.0` |
| **pluginVersion** | Each Impl → own Minor/Patch | OneOcr `1.0.0` |
| **updateChannel** | Interface `MAJOR.MINOR` | `1.0` → floating tag `plugin-v1.0-latest` |

- Host loads a zip only if `plugin.json` `interfaceVersion` **exactly** matches the host’s `Zaya.OCR` assembly version.
- Host updater fetches `plugin-v{updateChannel}-latest` (not Primitives).
- Do **not** set `<Version>` in csproj. Set `ZayaVersionMinor` / `ZayaVersionPatch` only; Major is taken from Primitives. `Directory.Build.targets` fails the build if Major drifts.

## plugin.json

```json
{
  "id": "OneOcr",
  "type": "ocr",
  "interface": "Zaya.OCR",
  "interfaceVersion": "1.0.0",
  "pluginVersion": "1.0.0"
}
```

Release body lists per-asset plugin versions (`Zaya.OCR.Impl.OneOcr.zip=1.0.0`).

## Bumping

1. Interface contract: raise `ZayaVersionMinor` / `ZayaVersionPatch` in `Zaya.OCR.csproj`, then bump host’s OCR reference.
2. Single engine: raise Minor/Patch only in that Impl’s `.csproj`.
3. Run `build.cmd` / Publish workflow.
