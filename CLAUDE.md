# CLAUDE.md — C3D Subassembly: Wadi Levee Scour Protection

Guidance for Claude Code working in this repository.

## What this project is

A fully-adaptive **custom .NET (C#) subassembly for Autodesk Civil 3D 2026** that draws wadi
flood-protection levees and **morphology-aware scour protection**. It samples the natural-ground
target surface across each corridor section, detects significant slope breaks via **least-squares
regression**, and places protection (armor + filter + **separate** scour key + **separate** launching
apron) at the levee face and at every qualifying break, recomputing each corridor rebuild.

## Status

**Design complete, pre-implementation.** The approved spec is the source of truth:
`docs/superpowers/specs/2026-06-27-wadi-levee-scour-subassembly-design.md`. Read it first.
Next step is the implementation plan (superpowers `writing-plans`), then code.

## Hard constraints (do not violate)

- **.NET / C# only.** Not Subassembly Composer/PKT (can't do regression/loops/arrays) and not Python
  (can't run as a live corridor subassembly). **No hybrid / no combination of technologies.**
- **Fully adaptive** — must run live inside the corridor rebuild, not as a pre-process or post-process.
- **Nothing hard-coded.** Every dimension and parameter is exposed and adjustable in the Properties
  panel.

## Key decisions

- Origin at levee crest; crest elevation from the baseline profile.
- `Side ∈ {Left, Right, Both}` — one codebase; place per-bank baselines for wide/oblique wadis.
- Composite, fully-parametric detection trigger: slope-change (trigger) + outward-slope +
  segment-length + elevation-drop (AND-gated), all tunable and individually neutralizable.
- Protection package = armor + filter + **separate** key + **separate** apron; merge rule joins
  segments within `Merge Gap`.
- Parameters: numbered groups (`GG.s Name`) to keep the large panel navigable; 10 offset-range
  override slots gated by `OverrideCount` (palette can't truly hide/reveal fields at runtime).
- Scour sizing = fixed tunable parameters in v1 (computed Lacey/regime is v2).
- Also outputs excavation/cut, stripping, and expropriation/ROW limits. Quantities: rock/fill by
  volume, geotextile by area, plus cut/excavation/stripping volumes.

## How to work here

- **TDD the algorithmic core.** The regression, break-detection, gates, and merge logic are **pure C#**
  with no Civil 3D dependency — build and unit-test them in isolation first, then wire the Civil 3D
  geometry/shape layer on top.
- Benchmark against the supplied cut & fill typical sections (see spec §1 / §10).
- Deliverables: `WadiLeveeScour.dll`, a help **README.md**, and a sample `.dwg`.

## Build / test

To be defined once the solution is scaffolded (target: .NET for C3D 2026 API, xUnit/NUnit for the
core). Update this section when the project structure exists.

## Conventions

- Keep parameter naming on the numbered `GG.s` convention.
- Reference the spec section numbers in commit messages and PRs when implementing a feature.
