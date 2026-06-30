# C3D Subassembly Wadi Training

Terrain-adaptive left-bank wadi levee subassembly for Civil 3D 2026 / Subassembly Composer.

## Purpose

This subassembly draws a left-bank levee and scans the wadi-side ground toward the thalweg to identify concave terrain breaks that may require scour protection.

It is intended for wadi flood-protection training and early geometry testing:
- draw levee crown and both side slopes
- sample existing ground from the wadi toe toward thalweg
- detect concave slope breaks using reference-point terrain trends
- ignore convex breaks but mark them visibly for checking
- place protection links on confirmed concave breaks
- use surface-strip links so visible ground/protection runs follow the Civil 3D surface, not straight sample chords

## Current Deliverable

Use this packet in Civil 3D:

- `output/LeveeOnly.LeftBank.v010.civil3d.pkt`

The source packet is also included:

- `output/LeveeOnly.LeftBank.v010.source.pkt`

## Required Targets

Set these targets after importing the PKT:

- `Existing Ground`: required surface target.
- `Thalweg Offset`: optional offset target. If assigned, scanning stops at this offset. If not assigned, scanning stops at `Max Scan Distance`.

The insertion point/origin is the wadi-side crown edge. Assign your hydraulic profile/elevation at the corridor baseline/profile level.

## Input Parameters

| Parameter | Default | Purpose |
|---|---:|---|
| `Crown Width` | `4.0 m` | Levee crest width. |
| `Levee Side Slope` | `0.5` | Grade value for 1V:2H side slopes. |
| `Sample Interval` | `1.0 m` | Auxiliary terrain sampling spacing for break detection. |
| `Max Scan Distance` | `250 m` | Scan limit when `Thalweg Offset` is not assigned. |
| `Slope Change Threshold` | `0.10` | Relative slope-change trigger for terrain break candidates. |
| `Min Mild Trend Length` | `5.0 m` | Required continuing mild run after a concave candidate before protection is accepted. |
| `Minimum Steep Length` | `0.6 m` | Minimum steep run before a concave candidate can be protected. |
| `Mild Protection Length` | `2.0 m` | Protection length placed on the mild side of an accepted concave break. |
| `Maximum Steep Protection Length` | `3.0 m` | Cap on protection length extending up the steep side. |
| `Break Marker Size` | `0.5 m` | Half-size of visible diamond markers at detected breaks. |
| `Merge Distance` | `5.0 m` | Maximum gap for merging adjacent protection runs. |

`Minimum Mild Length` was removed. Trend confirmation is controlled by `Min Mild Trend Length`; placed mild protection is controlled by `Mild Protection Length`.

## Codes And Display

Civil 3D colors are controlled by the Code Set Style. The PKT assigns these codes so you can map them:

| Code | Recommended color | Meaning |
|---|---|---|
| `SurfaceLink`, `SurfaceYellow` | Yellow | Existing-ground surface runs where no protection is placed. |
| `Protection`, `ProtectionCyan` | Cyan | All protection runs. |
| `ProtectionSteep` | Cyan | Protection on the steep side of a concave break. |
| `ProtectionMild` | Cyan | Protection on the mild side of a concave break. |
| `ProtectionMerge` | Cyan | Merged protection gap between close protection runs. |
| `ConcaveBreakMarker` | Cyan or magenta | Large visible marker at accepted concave break. |
| `ConvexBreakMarker` | Red or gray | Temporary visible marker at ignored convex break. |

## Notes

- Break markers are diamond link markers because SAC line geometry does not provide true circle symbols.
- Auxiliary sample points are used for calculations; visible surface and protection links use surface strips.
- This is still line-only geometry: no closed shapes are included.
