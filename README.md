# C3D Subassembly — Wadi Levee Scour Protection

A fully-adaptive **custom .NET (C#) subassembly for Autodesk Civil 3D 2026** for wadi flood-protection
levees with **morphology-aware scour protection**.

It builds the levee and its face protection, then walks the natural-ground target surface across each
corridor section, detects significant slope breaks with **least-squares regression**, and places
scour protection — **armor + filter + separate scour key + separate launching apron** — at the levee
face and at every qualifying break, recomputing every corridor rebuild. One subassembly serves both
banks via `Side ∈ {Left, Right, Both}`.

## Why it exists

In a wide wadi, scour is not a levee-toe-only problem: floodplains strip, the channel migrates toward
the levee, and scour initiates **down on the bank, often well below toe level**. A wide cross-section
has several morphological benches, and **each slope break needs its own protection**. This tool
detects those breaks adaptively and protects them.

## Status

**Design complete, implementation not started.** Source of truth:
[`docs/superpowers/specs/2026-06-27-wadi-levee-scour-subassembly-design.md`](docs/superpowers/specs/2026-06-27-wadi-levee-scour-subassembly-design.md).

## Highlights

- **.NET / C# only** — Subassembly Composer can't do regression/loops; Python can't be a live corridor
  subassembly.
- **Composite, fully-parametric break detection** — slope-change + outward-slope + segment-length +
  elevation-drop, all tunable. **Nothing hard-coded.**
- **Parameters** in numbered groups (`GG.s Name`); 10 offset-range override slots gated by
  `OverrideCount`.
- **Quantities** — rock/fill by volume, **geotextile by area**, plus cut, excavation, and stripping
  volumes; excavation/stripping/expropriation (ROW) limits emitted.

## Roadmap

- **v1:** levee + adaptive scour protection, fixed (tunable) scour sizing, quantity/limit outputs,
  README help, sample DWG.
- **v2:** computed scour (Lacey/regime), automatic thalweg detection, dynamic override count,
  plan-view continuity smoothing.

## Repository layout

```
docs/superpowers/specs/   Design spec (start here)
CLAUDE.md                 Guidance for Claude Code sessions
README.md                 This file (will grow into the subassembly help/instruction doc)
```

> The full per-parameter help/instruction content will be completed alongside implementation.
