# THROUGHPUT

A deterministic factory optimization game. C# / .NET authoritative simulation core, Godot 4 presentation layer (coming in a later milestone).

> Same setup + same actions = same result. Every time.

This is a game about **pure efficiency**. No randomness, no breakdowns, no worker needs, no warehouse management. A machine stops only because the production system caused it to stop. Every delay has a measurable cause, and every run can be replayed and hash-verified.

## Project Structure

```
Throughput/
├── src/
│   ├── Throughput.Sim/          Engine-independent deterministic simulation core
│   ├── Throughput.Scenarios/    Scenario definitions (garage-01 and variants)
│   ├── Throughput.Headless/     CLI runner, reports, run comparison
│   └── Throughput.Game/         Godot 4 presentation (future milestone)
├── tests/
│   └── Throughput.Sim.Tests/    Determinism, accounting, replay, regression tests
└── README.md
```

**Hard architecture boundary:** `Throughput.Sim` never references Godot. The entire simulation runs headless. Godot only renders; it mutates the simulation exclusively through `SimCommand`s.

## Requirements

- .NET SDK 10 (`dotnet` on PATH, or `~/.dotnet/dotnet`)
- Godot 4.7+ (`.NET` / mono build) to run the game

## Build & Test

```bash
dotnet build Throughput.slnx
dotnet test tests/Throughput.Sim.Tests
```

## Run the Game (Godot)

Requires the **Godot .NET (mono) build** — the plain `godot` binary has no C# support and will fail with `No loader found for resource ... (expected type: Script)`.

From the repo root:

```bash
./run.sh            # uses /Applications/Godot_mono.app
```

or explicitly:

```bash
/Applications/Godot_mono.app/Contents/MacOS/Godot --path src/Throughput.Game
```

(If `godot` on your PATH points at the non-mono `/Applications/Godot.app`, either repoint the symlink to `Godot_mono.app` or use `./run.sh`.)

The game renders the factory (machines colored by state, moving workers, moving items, growing queue bars) and drives the same deterministic `Throughput.Sim` core. The final state hash matches the headless run exactly.

Controls:

| Key | Action |
|---|---|
| `Space` | Pause / resume |
| `1` `2` `3` | 1× / 4× / 16× |
| `4` | MAX (processes events as fast as possible) |
| `R` | Restart the shift |
| Click | Inspect a machine or worker (live state accounting) |

## Run a Scenario Headless

```bash
dotnet run --project src/Throughput.Headless -- garage-01
dotnet run --project src/Throughput.Headless -- garage-01-mill-near
```

Scenarios: `garage-01`, `garage-01-default`, `garage-01-mill-near`, `garage-01-worker-priority-a`, `garage-01-worker-priority-b`.

Verify determinism (runs twice, hashes must match):

```bash
dotnet run --project src/Throughput.Headless -- garage-01 --check-determinism
```

Store a baseline, then compare any later run against it:

```bash
dotnet run --project src/Throughput.Headless -- garage-01 --save-summary baseline.json
dotnet run --project src/Throughput.Headless -- garage-01-mill-near --compare baseline.json
```

Save and replay a run:

```bash
dotnet run --project src/Throughput.Headless -- garage-01-mill-near --save-replay mill-near.json
dotnet run --project src/Throughput.Headless -- garage-01 --commands mill-near.json
```

A replay is just `scenario + version + ruleset + ordered commands`. Replaying it must reproduce the identical final state hash.

## Scenario: garage-01

One eight-hour shift (08:00 — 16:00). Raw material is infinite and pull-based (produced on request, never hoarded). Finished gearboxes leave the system immediately when shipped. All queues are unbounded.

```
RAW SOURCE ──▶ SAW 01 ──▶ QUEUE A ──▶ MILL 01 ──▶ QC 01 ──▶ ASSEMBLY 01 ──▶ DOCK
```

| Machine | Process | Cycle | Feeds |
|---|---|---|---|
| SAW_01 | RAW_STOCK → CUT_BLANK | 40 s | QUEUE_A |
| MILL_01 | CUT_BLANK → MACHINED_PART | 60 s | QUEUE_B |
| QC_01 | MACHINED_PART → INSPECTED_PART | 35 s | QUEUE_C |
| ASSEMBLY_01 | INSPECTED_PART → GEARBOX | 100 s | DOCK |

Two workers (1.0 m/s) move every item and load every machine. Workers do not babysit machines — processing runs unattended.

## Simulation Model

- **Discrete-event simulation.** The world jumps between meaningful events; nothing is simulated per-frame. Event ordering is `(timestamp, sequence)`.
- **Integer time.** 1 tick = 1 ms. `SimTime(long Milliseconds)`. No floating-point time anywhere in the authoritative core.
- **Integer distances.** Grid positions in tiles (1 tile = 1 m). Travel time = Manhattan distance × 1000 mm / worker speed. Same route → same travel time.
- **Items are explicit entities** with a full activity trace (`Waiting`, `Queueing`, `Transporting`, `Processing`, `Shipped`) and per-activity time accounting. Lead time is fully accounted: processing + queueing + transport + waiting = total, with 0 ms unaccounted.
- **Exact state accounting.** Every machine and worker accumulates time per state, and those sums exactly equal elapsed shift time. An invariant validator runs after every simulation and fails loudly on any violation.

## Worker Task Rules (deterministic, no arbitrary ties)

1. When a task becomes available, the **nearest idle worker** takes it (tie: longest idle, then lowest worker ID).
2. When a worker becomes idle, it picks the best pending task by: **worker priority list** → **oldest task** → **shortest travel distance** → **lowest task ID**.
3. Machine load and transport tasks are queued FIFO. Dictionary enumeration order never influences behavior.

## State Hashing

The final state is canonicalized (machines/workers/items/queues sorted by ID) and hashed with SHA-256. Identical inputs produce the identical hash. This is enforced by tests.

## Milestones

- [x] **M1 — Deterministic core:** scenario loads, time, events, machines, workers, queues, items, shipping, metrics, shift end. Headless runner is bit-for-bit reproducible.
- [x] **M2 — Full accounting:** machine state, worker state, and item lead-time are 100% accounted and validated.
- [x] **M3 — Replay + hashing:** runs serialize to replay files, replay reproduces identical hashes, CI-able determinism tests.
- [x] **M4 — Godot factory:** factory renders, machines display state, workers and items move, queues visibly grow, speed controls (Pause, 1×, 4×, 16×, MAX).
- [x] **M5 — Inspectors:** click a machine / worker for live state accounting, or an item for the product profiler (created/shipped, lead time, per-station timeline, biggest delay) with a focus-on-floor path trace.
- [ ] **M6 — Editable layout:** move machines, grid snap, no overlap, travel distances update, results change.
- [ ] **M7 — Run comparison:** stored baseline, rerun, metric deltas (partially available headless via `--compare`).

## Explicit Non-Goals (for now)

No breakdowns, maintenance, worker fatigue/needs, skill variance, random anything, supplier/warehouse simulation, finite buffers, weather, pollution, power grid, vehicles, pathfinding, tech trees, multiplayer, modding.
