# C3D Subassembly Wadi Training

Terrain-adaptive left-bank wadi levee subassembly for Civil 3D 2026 / Subassembly Composer.

## Purpose

This packet draws a left-bank levee, fixed toe scour/apron protection, and terrain-adaptive protection at accepted concave ground breaks toward the thalweg.

## Current Deliverable

Use this Civil 3D import packet:

- `output/LeveeOnly.LeftBank.v012.civil3d.pkt`

Source packet:

- `output/LeveeOnly.LeftBank.v012.source.pkt`

## Required Targets

- `Existing Ground`: required surface target.
- `Thalweg Offset`: optional offset target. If assigned, scanning stops there. If not assigned, scanning stops at `Max Scan Distance`.

The origin is the wadi-side crown edge. Set the corridor/profile elevation there from your hydraulic line.

## What It Draws

- Levee crown, landward daylight slope, and wadi-side daylight slope.
- Fixed cyan scour protection from the wadi-side toe.
- Fixed cyan apron after the toe scour segment toward thalweg.
- Yellow existing-ground surface strips between protection zones.
- Cyan protection strips at accepted concave breaks.
- Cyan merge strips when protection runs are close enough to merge.
- Large visible concave and convex break marker diamonds.

Auxiliary 1 m sample points are calculation helpers only and have no display codes.

## Input Parameters

| Parameter | Default | Purpose |
|---|---:|---|
| `Crown Width` | `4.0 m` | Levee crest width. |
| `Levee Side Slope` | `0.5` | Grade value for 1V:2H side slopes. |
| `Sample Interval` | `1.0 m` | Hidden auxiliary terrain sample spacing. |
| `Max Scan Distance` | `250 m` | Scan limit when `Thalweg Offset` is not assigned. |
| `Slope Change Threshold` | `0.10` | Relative terrain trend change that creates a break candidate. |
| `Toe Scour Length` | `2.0 m` | Fixed scour protection length from the wadi-side toe. |
| `Toe Apron Length` | `5.0 m` | Fixed apron length after the toe scour segment. |
| `Min Mild Trend Length` | `5.0 m` | Required continuing mild run after a concave candidate before acceptance. |
| `Minimum Steep Length` | `0.6 m` | Minimum steep run before a concave candidate can be protected. |
| `Mild Protection Length` | `2.0 m` | Protection length placed on the mild side of an accepted concave break. |
| `Maximum Steep Protection Length` | `3.0 m` | Cap on protection length extending up the steep side. |
| `Break Marker Size` | `0.5 m` | Half-size of visible diamond break markers. |
| `Merge Distance` | `5.0 m` | Maximum gap for merging adjacent protection runs. |

## Codes

Map these codes in your Civil 3D Code Set Style:

| Code | Recommended color | Meaning |
|---|---|---|
| `SurfaceYellow` | Yellow | Existing-ground strips where no protection is placed. |
| `ProtectionCyan` | Cyan | All protection geometry. |
| `ToeScourProtection` | Cyan | Fixed toe scour protection. |
| `ToeApronProtection` | Cyan | Fixed bed apron protection. |
| `ProtectionSteep` | Cyan | Protection up the steep side of an accepted concave break. |
| `ProtectionMild` | Cyan | Protection on the mild side of an accepted concave break. |
| `ProtectionMerge` | Cyan | Joined gap between close protection runs. |
| `ConcaveBreakMarker` | Cyan | Accepted concave break marker. |
| `ConvexBreakMarker` | Any visible color | Ignored convex break marker for checking logic. |

This is still line-only geometry. No shapes are included.
