# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
History starts at the current release line; older releases are not backfilled.

## [Unreleased]

### Changed

- **ProximityTextLayout `1.2.0.2`:** replace `leftEdgeAlignmentTolerance`, `firstLineIndentTolerance`, `enableCenterAlignment`, and `maxLineProtrusionPercent` with a single `lineOverhangTolerancePercent` (default `100`). Lines merge into a paragraph when at least one lies within the other along reading, allowing overhang up to that percent of line height.
- Clarify `enableStabilization` labels/descriptions (snap geometry across frames) in EN, RU, and other locales.
