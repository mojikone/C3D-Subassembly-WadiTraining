# Naming Convention

Main levee points, engineering notation:
- `P0` - origin, wadi-side crown edge
- `P1` - land-side crown edge
- `P2` - wadi-side levee toe on existing ground
- `P3` - land-side levee toe on existing ground

Main levee points, SAC notation:
- `P1` - origin, equivalent to engineering `P0`
- `P2` - land-side crown edge, equivalent to engineering `P1`
- `P3` - wadi-side levee toe, equivalent to engineering `P2`
- `P4` - land-side levee toe, equivalent to engineering `P3`

Scan/reference points:
- `Ref_0`, `Ref_1`, ... - active terrain trend reference point
- `AP1`, `AP2`, ... - 1 m auxiliary existing-ground sample points
- `B_001`, `B_002`, ... - detected break points
- `CX_001`, `CX_002`, ... - temporary convex break markers

Protection points:
- `PR_001_A` - steep-side protection start
- `PR_001_B` - break point
- `PR_001_C` - mild-side protection end

SAC link geometry names:
- use `L1`, `L2`, `L3`, etc.
- keep descriptive meaning in link codes, not geometry names

Levee links:
- `L1` - crown link, code `Crown`
- `L2` - wadi face link, code `WadiFace`
- `L3` - land face link, code `LandFace`

Future links:
- `L_Surface_###` should become numbered SAC links before saving
- `L_Protection_###` should become numbered SAC links before saving

Auxiliary scan links:
- use `AL1`, `AL2`, etc.
- these are for detection only, not final corridor surface links
