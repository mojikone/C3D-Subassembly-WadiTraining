# Note For Claude

## Purpose

This repository builds a Civil 3D 2026 custom .NET subassembly for terrain-adaptive wadi levee protection. The goal is to draw the levee, scan existing ground toward the wadi, detect terrain morphology breaks, mark concave/convex trend intersections, and place protection only at concave scour-risk breaks plus the embankment toe.

Current package:

- Version: `W2.3`
- PKT: `output/WadiTrainingLevee_W2.pkt`
- Build: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Build-Pkt.ps1`
- Tests: `dotnet run --project tests/WadiTrainingAlgorithmTests/WadiTrainingAlgorithmTests.csproj`

## User-Visible Behavior

- One-side tool: baseline/profile controls the levee crown elevation; user targets only `ExistingGround` and optional `ScanLimitOffset`.
- Both-sides tool: baseline/profile is centerline/thalweg control; user targets `ExistingGround`, `LeftBankCrownOffset`, and `RightBankCrownOffset`.
- No profile/elevation targets are required for bank crowns.
- Draws levee crown, wadi face, land face, toe scour, apron, terrain-following surface/protection links, and break markers.
- Convex markers are debug markers controlled by `Show Convex Markers`.
- Colors are not hard-coded; Civil 3D Code Set Style must map `WT_*` codes.

## Code Map

- `src/WadiTrainingSubassembly/Subassembly/WadiTrainingLevee.cs`
  - Entry point for Civil 3D.
  - Defines `WadiTrainingLeveeOneSide` and `WadiTrainingLeveeBothSides`.
  - Reads targets/parameters, creates `TerrainSampler`, then calls shared drawing logic.

- `src/WadiTrainingSubassembly/Runtime/ParameterDefinitions.cs`
  - Registers Civil 3D input parameters, output parameters, and target names.
  - Important: `Slope Change Threshold` is dimensionless, e.g. `0.10 = 10%`.

- `src/WadiTrainingSubassembly/Models/WadiParameters.cs`
  - Converts Civil 3D runtime parameters into typed C# values.
  - Holds defaults/fallbacks used by algorithm and layout preview.

- `src/WadiTrainingSubassembly/Terrain/TerrainSampler.cs`
  - Reads existing ground from Civil 3D surface.
  - Finds levee daylight/toe.
  - Builds the analysis terrain run from the wadi toe to scan limit/centerline/max distance.
  - Uses `SampleSection` when drawing surface links so final links follow the actual Civil surface, not only analysis samples.

- `src/WadiTrainingSubassembly/Analysis/TrendDetector.cs`
  - Detects terrain trend breaks.
  - Cleans and sorts sampled ground points.
  - Simplifies terrain using `Max Trend Residual`.
  - Fits trend lines on both sides of each candidate using `Trend Window Length`.
  - Classifies:
    - Concave: steep dropping trend becomes milder; eligible for protection.
    - Convex: mild trend becomes steeper dropping trend; debug marker only.
  - Places markers at fitted trend-line intersection, projected to surface elevation.
  - Suppresses duplicate same-kind breaks using `Min Break Spacing`.

- `src/WadiTrainingSubassembly/Analysis/ProtectionPlanner.cs`
  - Creates protection intervals along the terrain run.
  - Always adds toe scour and toe apron from the wadi-side embankment toe.
  - Adds concave-break protection: capped steep-side length plus fixed mild-side length.
  - Merges nearby intervals using `Merge Distance`.

- `src/WadiTrainingSubassembly/Geometry/GeometryWriter.cs`
  - Converts algorithm results into Civil 3D points and links.
  - Draws levee geometry.
  - Draws terrain-following surface/protection links.
  - Draws large diamond markers for concave/convex breaks.

- `src/WadiTrainingSubassembly/Geometry/GeometryCodes.cs`
  - Central list of link/point codes such as `WT_Surface`, `WT_Protection`, `WT_Concave`, `WT_Convex`.

- `src/WadiTrainingSubassembly/Runtime/CivilRuntime.cs`
  - Thin wrapper around Civil 3D `CorridorState`.
  - Keeps Civil API access out of the algorithm classes.

- `src/WadiTrainingSubassembly/Models/*`
  - Small data models: terrain points/runs, trend lines, break candidates, protection intervals, bank mode, break kind.

- `scripts/Build-Pkt.ps1`
  - Builds the .NET DLL and packages it with the generated `.atc` into a Civil 3D importable `.pkt`.

- `tests/WadiTrainingAlgorithmTests/Program.cs`
  - Console tests for concave detection, convex detection, curved-drop duplicate suppression, toe protection, and concave protection intervals.

## Important Next Checks

- Test in Civil 3D section views with real surfaces after importing the PKT.
- If links look wrong, check `TerrainSampler.SampleCivilSurface()` and Code Set Style mapping first.
- If break locations look wrong, tune `Trend Window Length`, `Min Mild Trend Length`, `Slope Change Threshold`, and `Max Trend Residual` before changing classification logic.
