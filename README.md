# Wadi Training Levee Subassembly W2

Custom Civil 3D 2026 .NET subassembly for terrain-adaptive wadi levee protection.

## Output

- Draws levee crown, wadi-side face, and land-side face.
- Finds the wadi-side toe by daylighting the levee slope to `ExistingGround`.
- Draws toe scour and apron protection from the wadi-side toe toward the thalweg.
- Scans existing ground toward `ThalwegOffset` if assigned, otherwise to `Max Scan Distance`.
- Fits terrain trend lines and marks their intersection as:
  - `WT_Concave`: concave break marker, protected.
  - `WT_Convex`: convex break marker, shown for debugging only.
- Draws continuous ground-following links:
  - `WT_Surface`: unprotected surface links.
  - `WT_Protection`, `WT_ToeScour`, `WT_ToeApron`: protection links.

## Required Target

- `ExistingGround`: surface target.

## Optional Targets

- `ThalwegOffset`: offset target that stops one-side scanning.
- `LeftBankCrownOffset`, `RightBankCrownOffset`: used only when `Bank Mode = Both`.
- `LeftBankCrownElevation`, `RightBankCrownElevation`: reserved for later both-bank elevation targeting.

## Main Parameters

- `Bank Mode`: Right, Left, or Both.
- `Crown Width`: levee crown width.
- `Levee Side Slope`: vertical/horizontal grade; `0.5` means 1V:2H.
- `Max Scan Distance`: fallback scan length when thalweg is missing.
- `Analysis Sample Interval`: spacing used only for trend analysis.
- `Trend Window Length`: length used on each side to fit trend lines.
- `Min Mild Trend Length`: minimum mild trend length needed to accept a break.
- `Min Steep Trend Length`: minimum steep trend length needed to accept a break.
- `Slope Change Threshold`: dimensionless ratio; `0.20` means 20 percent.
- `Max Trend Residual`: maximum RMS vertical error for fitted trend lines.
- `Min Break Spacing`: suppresses duplicate same-kind break markers.
- `Mild Protection Length`: protected length on mild side of concave break.
- `Max Steep Protection Length`: capped protected length up steep side.
- `Merge Distance`: merges nearby protection intervals.
- `Toe Scour Length`: toe scour protection length, default `2 m`.
- `Toe Apron Length`: apron protection length, default `2 m`.
- `Break Marker Size`: visible diamond marker size.
- `Show Convex Markers`: shows convex breaks for debugging.

## Package

Generated packet:

`output/WadiTrainingLevee_W2.pkt`

Build command:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Build-Pkt.ps1
```
