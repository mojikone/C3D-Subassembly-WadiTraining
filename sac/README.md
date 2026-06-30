# SAC Working Files

`LeveeOnly.LeftBank.xaml` is the first line-only SAC workflow draft.

Validated Roadway-mode behavior against a flat preview surface at `Y=-5`:
- `P1` origin/wadi crown edge at `(0, 0)`
- `P2` land crown edge at `(-4, 0)`
- `P3` wadi toe at `(10, -5)`
- `P4` land toe at `(-14, -5)`
- `AP1` to `AP10` are 1 m auxiliary existing-ground samples from the wadi toe toward thalweg
- `AL1` to `AL10` are auxiliary sample links
- three real levee links, no shapes

Run from PowerShell:
- `..\tests\Validate-LeveeOnly.ps1`
- `.\Build-SourcePkt.ps1`

`Build-SourcePkt.ps1` creates the latest source packet in `..\output`. It is intended for SAC 2026 workflow opening/export, not yet as the final Civil 3D production packet.
