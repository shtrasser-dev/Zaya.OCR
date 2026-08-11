# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
History starts at the current release line; older releases are not backfilled.

## [1.2.0.2] - 2026-08-11

### Changed

- **ProximityTextLayout `1.2.0.2`:** replace `leftEdgeAlignmentTolerance`, `firstLineIndentTolerance`, `enableCenterAlignment`, and `maxLineProtrusionPercent` with a single `lineOverhangTolerancePercent` (default `100`). Lines merge into a paragraph when at least one lies within the other along reading, allowing overhang up to that percent of line height.
- Show previous-line search and merge hysteresis even when stabilization is off; hide only hold / text-match / ghost settings under `enableStabilization`.
- GitHub Release notes are taken from this file’s `[Unreleased]` section (manual changelog input removed from Publish).

## [Unreleased]

### Changed

- **ProximityTextLayout `1.2.0.3`:** unify setting description units (percent / degrees / frames) and shorten previous-line search (along) copy.
