# Parameter Definitions

| Name | Default | Unit | Purpose |
|---|---:|---|---|
| `CrownWidth` | `4.0` | m | Levee crest width |
| `LeveeSideSlope` | `0.50` | grade | 1V:2H side slope |
| `SampleInterval` | `1.0` | m | Existing-ground sampling interval |
| `SlopeChangeThreshold` | `0.10` | ratio | Trend-change trigger |
| `MinMildTrendLength` | `5.0` | m | Minimum confirmed mild trend length after a concave candidate |
| `MinSteepLength` | `0.6` | m | Minimum steep face length for protection |
| `MildProtectionLength` | `2.0` | m | Protection length placed on the mild side |
| `MaxSteepProtectionLength` | `3.0` | m | Maximum protection length up steep face |
| `BreakMarkerSize` | `0.5` | m | Half-size of visible break marker diamond |
| `MergeDistance` | `5.0` | m | Maximum gap for merging protection runs |
| `MaxScanDistance` | `100.0` | m | Fallback scan limit if thalweg target is missing |

Targets:
- `ExistingGround` - required surface target
- `HydraulicProfile` - elevation/profile target at origin
- `ThalwegOffset` - optional offset target; primary scan stop

Direction convention for left-bank prototype:
- `P0` origin is the wadi-side crown edge
- landward is negative offset
- wadi/thalweg direction is positive offset
