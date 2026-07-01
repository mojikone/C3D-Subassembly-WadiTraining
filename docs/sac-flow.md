# SAC Flow - W1 v013

1. Create `P1` at the origin: wadi-side crown edge.
2. Create `P2` landward by `CrownWidth`.
3. Create the crown link `P1-P2`.
4. Daylight from `P1` to `ExistingGround`; create wadi toe `P3`.
5. Daylight from `P2` to `ExistingGround`; create land toe `P4`.
6. Set `ScanLimitX` from `ThalwegOffset` when assigned, otherwise `P3.X + MaxScanDistance`.
7. Draw fixed cyan toe scour link from `P3` to `ToeScourEndX`.
8. Draw fixed cyan apron link from `ToeScourEndX` to `FixedToeProtectionEndX`.
9. Set the first terrain reference point to `P3`.
10. Create hidden auxiliary ground samples every `SampleInterval`.
11. Calculate all terrain trend slopes from the active reference point.
12. Detect slope trend changes using `SlopeChangeThreshold`.
13. At steep-to-mild changes, store a concave candidate.
14. While a concave candidate is active, do not overwrite it with minor curve segments.
15. Accept the candidate only after the mild trend continues at least `MinMildTrendLength`.
16. Compute the accepted break X at the intersection of the steep and mild trend lines.
17. Ignore candidates inside the fixed toe scour/apron zone, but still advance the trend reference.
18. At mild-to-steep changes, draw a visible convex marker at the trend-line intersection and skip protection.
19. At accepted concave breaks, draw a visible concave marker at the trend-line intersection.
20. Draw yellow existing-ground surface-snapped links between protection zones.
21. Draw cyan surface-snapped protection links:
    - steep side capped by `MaxSteepProtectionLength`
    - mild side equal to `MildProtectionLength`
22. Merge close protection runs when gap <= `MergeDistance`.
23. Set the accepted concave break as the new terrain reference.
24. Draw the final yellow existing-ground link to thalweg or max scan distance.
