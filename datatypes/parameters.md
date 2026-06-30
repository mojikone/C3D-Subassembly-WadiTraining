# Parameter Definitions

| Name | Default | Unit | Purpose |
|---|---:|---|---|
| `CrownWidth` | `4.0` | m | Levee crest width |
| `LeveeSideSlope` | `0.50` | grade | 1V:2H side slope |
| `SampleInterval` | `1.0` | m | Hidden existing-ground sampling interval |
| `MaxScanDistance` | `250.0` | m | Fallback scan limit if thalweg target is missing |
| `SlopeChangeThreshold` | `0.10` | ratio | Relative trend-change trigger |
| `ToeScourLength` | `2.0` | m | Fixed scour protection length from wadi-side toe |
| `ToeApronLength` | `5.0` | m | Fixed apron length after toe scour protection |
| `MinMildTrendLength` | `5.0` | m | Minimum confirmed mild trend length after a concave candidate |
| `MinSteepLength` | `0.6` | m | Minimum steep face length for protection |
| `MildProtectionLength` | `2.0` | m | Protection length placed on the mild side |
| `MaxSteepProtectionLength` | `3.0` | m | Maximum protection length up the steep face |
| `BreakMarkerSize` | `0.5` | m | Half-size of visible break marker diamond |
| `MergeDistance` | `5.0` | m | Maximum gap for merging protection runs |

Targets:
- `ExistingGround` - required surface target.
- `ThalwegOffset` - optional offset target; primary scan stop.

Direction convention:
- `P1` origin is the wadi-side crown edge.
- Landward is negative offset.
- Wadi/thalweg direction is positive offset.
