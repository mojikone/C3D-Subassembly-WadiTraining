# SAC Flow - W1 Prototype

1. Create `P1` at the origin: wadi-side crown edge.
2. Create `P2` landward by `CrownWidth`.
3. Create crown link `P1-P2`.
4. Daylight from `P1` to `ExistingGround` at `LeveeSideSlope`; create wadi toe `P3`.
5. Daylight from `P2` to `ExistingGround` at `LeveeSideSlope`; create land toe `P4`.
6. Set scan limit:
   - `ThalwegOffset` when assigned
   - otherwise `P3.X + MaxScanDistance`
7. Set first terrain reference to `P3`.
8. Create auxiliary ground samples `AP1...AP250` every `SampleInterval`.
9. Calculate trial slopes from current reference point to each sample.
10. Compare current trend against previous trend using `SlopeChangeThreshold`.
11. At a steep-to-mild change, store a concave candidate.
12. Do not protect immediately.
13. Confirm the candidate only when the mild trend continues at least `MinMildTrendLength`.
14. Reject short mild runs and continue scanning.
15. At a mild-to-steep change, skip protection and do not draw a marker.
16. At accepted concave breaks, create a visible concave marker.
17. Place protection using ground points and normal SAC links:
   - steep side length capped by `MaxSteepProtectionLength`
   - mild side length = `MildProtectionLength`
18. Protection start/end points are generated on `ExistingGround`.
19. Generate existing-ground surface links between hidden auxiliary sample points.
20. Merge close protection runs when gap <= `MergeDistance`.
21. Set the accepted concave break as the new terrain reference.
22. Continue until thalweg target or fallback scan distance.
