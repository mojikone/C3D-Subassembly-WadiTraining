# Wadi Levee Scour-Protection Subassembly — Design Spec

- **Date:** 2026-06-27
- **Status:** Draft for review
- **Author:** Design dialogue (Renardet / Subassembly project)
- **Target platform:** Autodesk Civil 3D 2026
- **Technology:** Custom **.NET (C#) subassembly** loaded live into the corridor

---

## 1. Context & problem

A wadi flood-protection scheme places levees along both banks. The levee **crest follows the
design hydraulic line** (identical crest elevation left & right bank at a given station), but the
natural ground is irregular, so each levee's **height and exposed wadi-side face vary** by bank and
by station.

The governing failure mode is **scour**. The key engineering insight driving this tool:

- Scour is **not** a levee-toe-only problem. In a wide wadi (tens to hundreds of metres) the levee
  may sit back on the floodplain. During a large flood the floodplain strips, the active channel
  migrates toward the levee, and the bank edge retreats until the failure plane reaches the toe —
  but scour **initiates down on the bank, often well below toe level**.
- A wide wadi cross-section is **multi-segmented**: several morphological benches / slope breaks
  (terrace edge → upper bank → lower bank → channel shoulder → thalweg). **Each break** concentrates
  flow and can develop a steep scour face, so **each break needs its own protection**, not just the
  toe.

The tool must therefore be **terrain-adaptive**: build the levee + its protection, then read the
natural cross-section, detect the significant slope breaks, and place scour protection at each one,
recomputing every corridor rebuild.

## 2. Goals (v1) and non-goals

**Goals**
- A single, fully-adaptive `.dll` subassembly for C3D 2026.
- Builds the levee body, faces, and face protection (cut and fill cases).
- Detects qualifying slope breaks across the natural ground via **least-squares regression**, with a
  fully **parametric composite trigger**.
- Places, at the levee face and at each qualifying break, a protection package:
  **armor + filter + separate scour key + separate launching apron**.
- Handles both banks via a **`Side ∈ {Left, Right, Both}`** parameter (one codebase).
- Outputs **excavation limits, stripping limits, and expropriation/ROW extents** for quantities.
- **Every dimension/parameter is exposed and adjustable. Nothing is hard-coded.**
- Ships with a **README.md** help/instruction file.

**Non-goals (v2 / future)**
- Computed scour depth (Lacey/Blench regime, or discharge/velocity driven).
- Automatic thalweg detection.
- Dynamic (truly runtime-variable) number of override slots.
- Plan-view (station-to-station) continuity smoothing of the detected geometry.

## 3. Why .NET (and not SAC/PKT or Python)

Two hard constraints — **(i) a fully adaptive subassembly running live in the corridor**, and
**(ii) one technology, no hybrid/combination** — narrow the field decisively:

- **Python** cannot *be* a corridor subassembly (it can only drive Civil 3D from outside as a
  one-shot/post-process). Violates "fully adaptive." → **out.**
- **Subassembly Composer / PKT** has **no general loops, no arrays, no accumulators, no
  regression** by design; its only iteration is fixed benching to a surface target. It **cannot host**
  variable-count regression break-detection + a merge rule. → **cannot do this algorithm.**
- **.NET custom subassembly** runs live in the corridor exactly like a PKT but executes arbitrary
  C#: real regression, variable break count, approach length, and the merge rule are all feasible,
  with full access to the target surface for sampling. → **selected.**

**Feasibility confirmed:** a .NET subassembly receives a `CorridorState` exposing target surfaces /
offset / elevation targets with `GetElevation`-style sampling; worst case it reads the target
`TinSurface` and calls `FindElevationAtXY` at any offset — so **arbitrary cross-section sampling for
the regression walk is supported.** (Exact API method names finalized in the implementation plan.)

## 4. Architecture overview

- **Origin:** at the **levee crest**; crest elevation is driven by the **baseline profile** (the
  "identical top elevation both banks" design line).
- **Single "build-one-bank" routine:** levee + adaptive wadi-side bed treatments marching toward the
  thalweg. `Side = Left/Right` calls it once (mirrored); `Side = Both` calls it twice about a center
  reference.
- **Corridor-modeling guidance:** for wide or oblique / non-parallel banks, use `Side = Left` and
  `Side = Right` on **two baselines each following its own bank alignment** (keeps sections normal to
  the bank, avoids skew). For reasonably parallel, not-too-wide wadis, `Side = Both` on a single
  centerline baseline.

## 5. Inputs & targets

| Input | Purpose |
|---|---|
| Baseline profile | Levee crest elevation (subassembly origin) |
| Target surface (natural ground / OGL) | Sampled for regression walk; used for daylighting |
| `Side` | Left / Right / Both |
| Center/thalweg offset reference (for `Both`) | Mirror axis; parameter or offset target |

## 6. Two segment types

1. **Levee segment** — crest (width + optional crown), landward face (slope + 200 mm gravel armor +
   small toe trench, all optional), wadi face (slope + 525 mm riprap armor + geotextile filter), wadi
   **toe = separate scour key + separate launching apron**, optional short bed apron toward thalweg.
2. **Wadi-bed-treatment segment** (one per qualifying break) — approach length landward of the break,
   full-slope armor + filter, and its own separate key + separate apron.

**Parameter hierarchy:** **Global** defaults → split into **Levee** and **Wadi-treatment** blocks →
**per-break overrides** (slots) that win over globals.

## 7. Parameter reference (grouped & numbered)

The Properties panel will be large, so parameters use a **numbered group + numbered step**
name-prefix convention (`GG.s Name`) to force ordering and show the user "where they are."
**All values below are parameters — none are fixed.** (Names indicative; finalized in implementation.)

- **01 Side & Targets** — `Side`, `Center/Thalweg Offset`, target-surface assignment, daylight behavior.
- **02 Levee Body** — `Crest Width`, `Crest Crown`, `Wadi-Face Slope (H:V)`, `Landward-Face Slope (H:V)`,
  crest offset/origin handling.
- **03 Levee Wadi-Face Protection** — `Armor Thickness` (e.g. 525 mm, normal to face), `Armor D50`
  (display/BOQ), `Filter/Geotextile` on/off + code (350 g/m²).
- **04 Levee Landward-Face Protection** — `Enable`, `Gravel Armor Thickness` (e.g. 200 mm), `Gravel D`,
  `Landward Toe Trench` on/off + dims.
- **05 Levee Toe (Key + Apron, separate)** — `Key Enable`, `Key Depth`, `Key Width`, `Key Side Slope`;
  `Apron Enable`, `Apron Length`, `Apron Thickness`, `Apron Slope`; `Bed Apron toward thalweg`
  length/thickness.
- **06 Detection** — `Sample Interval (s)`, `Window Length (W)`, `Min Slope Change` (trigger),
  `Min Outward Slope` (downslope-face gate), `Min Segment Length (L)`, `Min Elevation Drop (Δz)`,
  `Max Breaks` (cap), `Detection Start Offset` (from levee toe), `Detection Stop` (thalweg / section end).
- **07 Wadi-Treatment Global** — `Approach Length`, `Armor Thickness`, `Filter` on/off + code,
  `Key` params, `Apron` params (defaults applied to every detected break).
- **08 Continuity / Merge** — `Merge Gap` (e.g. 5 m): segments within this distance run continuous.
- **09 Overrides** — `Override Count` (0–10); slots `09.1 … 09.10`, each: `Enable`, `Offset Range
  (from–to)`, + overridable values (armor thickness, key depth, apron length, …). Keyed by **offset
  range** (stable across stations), not break index.
- **10 Excavation & Stripping** — `Strip Depth` (topsoil), `Foundation Strip/Bench` params under levee,
  `Excavation Side Slopes`, excavation limit behavior.
- **11 Expropriation / ROW** — `ROW Offset` beyond outermost works, expropriation extent markers.
- **12 Codes & Output** — point/link/shape codes per material and per limit line (see §11).

## 8. Processing pipeline (per section, every rebuild)

1. Read crest elevation from baseline; establish origin and `Side`.
2. Build levee body: crest (with crown) + wadi face + landward face; daylight faces to target surface
   or to defined toe geometry.
3. Build levee **wadi-face armor + filter**; build **landward armor + toe trench** (if enabled).
4. Build levee **toe**: scour **key** (cutoff trench) + **launching apron** as *separate* closed shapes;
   optional bed apron toward thalweg.
5. **Detection walk:** from `Detection Start Offset`, sample the target surface at `Sample Interval`
   to `Detection Stop`; sliding-window least-squares fit (`Window Length`) → local slopes; find
   candidate breaks (`Min Slope Change`); **confirm** when `Min Outward Slope` **and**
   `Min Segment Length` **and** `Min Elevation Drop` all pass; cap at `Max Breaks`.
6. For each confirmed break (ordered): build a protected segment = **approach** (`Approach Length`
   landward) + **full-slope armor + filter** + separate **key** + separate **apron**, using global
   wadi-treatment values or the matching offset-range override.
7. **Merge pass:** if a segment's approach-start falls within `Merge Gap` of the previous segment's
   downslope end, armor runs **continuous** (shapes abut, no gap); each part keeps its own
   thickness/material.
8. Build **stripping, excavation, and expropriation** limit lines/points (§11).
9. Emit all points/links/shapes with codes; mirror per `Side`; for `Both`, run the bank routine twice
   about the center reference.

## 9. Detection algorithm detail

- **Sampling:** elevations of the target surface at fixed offset interval `s` from `Detection Start`
  to `Detection Stop`.
- **Local slope:** least-squares line over a sliding window of length `W` (≈ `W/s` samples), giving a
  slope at each step; the window inherently smooths survey noise.
- **Candidate break:** where local slope changes by more than `Min Slope Change`.
- **Confirmation gates (all must hold — composite AND):**
  - `Min Outward Slope` — the downslope (thalweg-side) segment is genuinely steep (only arm faces
    worth arming).
  - `Min Segment Length` — each adjacent regression run is at least this long (noise rejection).
  - `Min Elevation Drop` — the vertical fall across the break is at least `Δz`.
- **Cap:** at most `Max Breaks` per section.
- **Merge:** see pipeline step 7; ensures closely-spaced protections form one continuous armor run.

Each gate is an independent, tunable parameter and can be neutralized (set to 0 / extreme) so the
engineer can dial the trigger to a specific wadi's morphology.

## 10. Geometry, shapes & BOQ codes

- Armor thickness measured **normal** to the face (inner offset link).
- Filter as a coded link or thin shape beneath armor.
- Key and apron as **distinct closed shapes**.
- Coded **points / links / shapes** drive material **shape styles** and **BOQ quantities**, plus
  feature lines. Material classes (indicative): wadi riprap (525 mm, D50 350), landward gravel
  (200 mm, D 20/80), geotextile (350 g/m²), selected earth fill, key fill, apron fill.
- **Quantities:** rock/fill materials by **volume** (shape area × corridor station spacing);
  **geotextile by area** (covered link length × station spacing → m²); plus **cut volume**,
  excavation volume, and stripping volume (see §11).

## 11. Excavation, stripping & expropriation outputs

- **Stripping limit** — line offset below OGL by `Strip Depth` across the works footprint, plus
  foundation stripping/benching under the levee → **stripping volume**.
- **Excavation / cut limit** — coded cut-extent points/links (cut case and toe/key trenches) →
  **excavation volume** and overall **cut volume** (corridor cut between design and OGL).
- **Expropriation / ROW** — outermost works-extent markers offset by `ROW Offset` → **area to be
  expropriated**.

All of these are emitted as coded geometry so corridor surfaces / feature lines / material shapes can
report **area and volume** — cut, fill, excavation, stripping volumes, and geotextile area.

## 12. Error handling & robustness

- Missing/!found surface target → parametric daylight fallback + warning marker.
- No qualifying breaks → levee + toe only (valid output).
- Guards: out-of-section samples, vertical/overhang faces, negative widths, zero-length segments.
- **Station-to-station continuity (known limitation):** if the qualifying-break *count* jumps between
  stations, the corridor surface can become ragged. Mitigations in v1: the noise-rejection gates,
  override slots, and adequate sample-line / assembly-insertion frequency. Plan-view smoothing is a
  v2 item.

## 13. Override mechanism & panel-visibility limitation

- Overrides are keyed by **offset range**, not break index (indices shift between stations).
- The Civil 3D Properties palette **cannot truly hide/reveal parameters at runtime** (both SAC and
  .NET parameter lists are static). Practical equivalent:
  - `Override Count (0–10)` — only the first N slots are processed.
  - Each slot leads with `Enable` and is named to read clearly as active/inactive.
  - `Override Count = 0` → override system dormant.

## 14. Testing strategy

- **Core logic is pure C#** (regression, break detection, gates, merge) — **isolated and unit-tested
  (TDD)** against synthetic cross-sections, no Civil 3D required.
- **Integration test** in C3D 2026 on a sample corridor; benchmark against the supplied cut & fill
  typical sections.

## 15. Deliverables

- `WadiLeveeScour.dll` — the custom .NET subassembly (C3D 2026).
- **`README.md`** — help/instruction: every parameter group, the detection logic, placement workflow
  (Side modes + baselines), override usage, and the cut/fill examples.
- Sample `.dwg` corridor demonstrating cut and fill cases.

## 16. Known limitations / v2 roadmap

- Computed scour (Lacey/regime or discharge/velocity).
- Automatic thalweg detection.
- Dynamic override count.
- Plan-view (station-to-station) continuity smoothing.

## 17. Assumptions to validate during implementation

- Exact `CorridorState` / target-surface sampling API names for C3D 2026.
- The .NET custom-subassembly registration/packaging workflow for 2026.
- Properties-palette grouping behavior for the `GG.s` name-prefix convention.
