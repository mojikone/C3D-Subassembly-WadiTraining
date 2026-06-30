# SAC Flow - W1 v012

1. Create `P1` at the origin: wadi-side crown edge.
2. Create `P2` landward by `CrownWidth`.
3. Create the crown link `P1-P2`.
4. Daylight from `P1` to `ExistingGround`; create wadi toe `P3`.
5. Daylight from `P2` to `ExistingGround`; create land toe `P4`.
6. Set `ScanLimitX` from `ThalwegOffset` when assigned, otherwise `P3.X + MaxScanDistance`.
7. Draw fixed cyan toe scour strip from `P3` to `ToeScourEndX`.
8. Draw fixed cyan apron strip from `ToeScourEndX` to `FixedToeProtectionEndX`.
9. Set the first terrain reference point to `P3`.
10. Create hidden auxiliary ground samples every `SampleInterval`.
11. Calculate all terrain trend slopes from the active reference point.
12. Detect slope trend changes using `SlopeChangeThreshold`.
13. At steep-to-mild changes, store a concave candidate.
14. Accept the candidate only after the mild trend continues at least `MinMildTrendLength`.
15. Ignore candidates inside the fixed toe scour/apron zone.
16. At mild-to-steep changes, draw a visible convex marker and skip protection.
17. At accepted concave breaks, draw a visible concave marker.
18. Draw yellow existing-ground strips between protection zones.
19. Draw cyan protection strips:
    - steep side capped by `MaxSteepProtectionLength`
    - mild side equal to `MildProtectionLength`
20. Merge close protection runs when gap <= `MergeDistance`.
21. Set the accepted concave break as the new terrain reference.
22. Draw the final yellow existing-ground strip to thalweg or max scan distance.
