# C3D Subassembly — Wadi Trend Finder (C2)

Custom Civil 3D 2026 .NET (C#) subassembly, packaged as an importable `.pkt`, for wadi
flood-protection levee design. It draws a one-side levee from the wadi-side crown (controlled by
the corridor baseline alignment + profile), scans existing ground toward the wadi, fits **straight
trend lines** to the terrain, and marks morphology breaks at the **intersections of consecutive
trend lines**:

- **Concave** (`WT_Concave`) — steep dropping trend turning milder: scour risk, to be protected.
- **Convex** (`WT_Convex`) — milder trend breaking into a steep drop: debug marker only.

Markers sit at the trend-line intersection, never on the surface, so rounded transitions get one
unambiguous marker (below the surface for concave, above for convex).

## Package

- Importable packet: [`output/WadiTrend_C2.pkt`](output/WadiTrend_C2.pkt) — tool `WadiTrend_OneSide_C2`.
- Build: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Build-Pkt.ps1`
- Tests: `dotnet run --project tests\SegmentationTests` (35 checks; also accepts a terrain CSV
  for debugging real sections: `dotnet run --project tests\SegmentationTests -- terrain.csv`).

## Targets

- `ExistingGround` (surface, required)
- `ScanLimitOffset` (offset target, optional — e.g. the thalweg alignment; else `Max Scan Distance`)

No elevation targets: the baseline profile controls the crown elevation.

## Algorithm (Core/TrendSegmentation.cs)

1. Seed segments from a vertical-tolerance simplification of the sampled ground.
2. **Pass A** — greedily merge adjacent segments while the combined least-squares fit stays
   within `Max Trend Residual`.
3. **Pass B** — merge adjacent segments whose slopes differ by less than
   `Slope Change Threshold` (small slope changes never split a trend).
4. **Pass C** — segments shorter than `Min Trend Length`: rounded transitions (slope between the
   neighbours') are dropped so the neighbours meet at their intersection; cliff steps (steeper
   than both neighbours) are kept.

The algorithm core has no Civil 3D dependency and is unit-tested in isolation.

## Key parameters (defaults tuned on the test wadi)

| Parameter | Default | Meaning |
|---|---|---|
| Max Trend Residual | 0.15 m | Max RMS vertical error of one trend line. Larger = fewer, longer trends. |
| Slope Change Threshold | 0.05 | Grade difference (5%) below which trends merge; also min steep-side grade. |
| Min Trend Length | 5.0 m | Shorter segments are bridged (rounded transition) or absorbed. |
| Analysis Sample Interval | 0.5 m | Ground sampling for analysis; drawn links follow the actual surface. |
| Show Trend Lines / Convex Markers | Yes | Debug visibility toggles. |

## Codes (map in Code Set Style)

Links: `WT_Crown`, `WT_WadiFace`, `WT_LandFace`, `WT_Surface`, `WT_Trend`, `WT_Concave`, `WT_Convex`.
Points: `WT_CrownPoint`, `WT_WadiToe`, `WT_LandToe`, `WT_Concave`, `WT_Convex`.
`tools/WadiToolbox` provides `(wadi-style-codes)` to create and map colored styles automatically.

## Dev toolbox (`tools/WadiToolbox`)

NETLOAD helper exposing LISP functions for scripted testing: import a pkt/atc
(`wadi-import-sub`), attach/erase subassemblies, set corridor targets, rebuild, dump computed
section geometry to JSON (`wadi-section-dump`), and auto-map code styles. Bump the
`AssemblyName` suffix before reloading into a running Civil 3D (loaded .NET assemblies never unload).

## History

- `CodexBackup` branch — previous development cycle (W2.3 sliding-window detector, Codex).
- The W1 sliding-window approach (with toe scour/apron + protection planner) is retained locally;
  protection placement will be ported onto the trend-finder engine next.
