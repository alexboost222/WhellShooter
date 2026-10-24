# 08 — Gameplay

What the game does. Tuning numbers get added here as they are settled;
unsettled design goes to `09-roadmap-and-open-design`.

## Pitch

A demo of a fast-paced **first-person co-op shooter**. One **dedicated server**,
one or more clients: friends connect to the same server and play a run together.

## Core loop (moment to moment)

- Move around the map in first person.
- Kill bots that spawn periodically in waves and attack the players.
- Collect resources dropped by the bots.

## Mid loop (one run)

- The map is procedurally generated.
- Players travel from safe zone to safe zone (3–5 minutes per leg), killing
  mobs along the way.
- Entering a safe zone closes the passage back.
- In a safe zone players auto-heal and upgrade their character with the
  resources earned from mobs.
- Then either continue to the next safe zone along a harder route, or choose to
  end the run.
- At the end of a run, stats are shown per player and for the run as a whole.

## Meta loop (across runs)

- Players try to beat each other's run-stat records.
- Session after session they improve their skill and get further and further.

## Map (procedural arena)

`Features/Map`. Every match is played on an arena generated from a seed out of the Synty
**PolygonGeneric** kit. Same seed → the same arena, bit for bit — that's what lets a seed be shared,
replayed or sent over the network later.

### Shape of the arena

A walled compound on a square **cell grid**. The cell is **2.5 m**, the module of the PolygonGeneric
`Base` building kit, so walls, floors and stairs snap without scaling.

| Tuning | Default | Why |
|---|---|---|
| Grid | 26 × 26 cells (65 × 65 m) | small-team FPS: crossing the map takes ~10 s on foot |
| Border wall | 2 storeys (6 m) | can't be seen over or jumped from a roof |
| Team spawn zones | 3 rows at each far edge (z), no buildings | each team starts behind its own cover line |
| Buildings | 4–7, 3–5 cells per side, ≥ 2 cells apart | lanes between them are ≥ 5 m wide |
| Building doors | ≥ 2, on different sides | a building is never a dead end |
| Windows | 35 % of plain wall segments | sight lines through, not walk through |
| Roof access | 60 % of buildings: outside stairs + 1.5 m parapet | verticality, a power position that can be contested |
| Cover | ~16 % of open cells, ≥ 1 empty cell between pieces | no cover touching cover: no sealed corridors |
| Low / high cover | 65 % / 35 % | low = shoot over (≤ 1.5 m), high = break line of sight |
| FFA spawns | 12, farthest-point spread | nobody spawns on top of anybody |

### Guarantees (enforced by the generator, covered by tests)

- **Connectivity.** Every walkable cell is reachable from every other on foot. A cover piece that
  would cut the map in two is not placed.
- **Doors stay usable.** No cover or prop on a cell in front of or behind a door.
- **Spawns are walkable**, inside the map, and the team zones hold only their own spawns.
- **Determinism.** The generator uses only its own seeded `Unity.Mathematics.Random`; no
  `UnityEngine.Random`, no time.

### How it is split

- **Generator** (`Utils/MapGenerator`) — pure C#, no Unity objects. Seed + `MapSettings` →
  `MapLayout`: the cell grid, the edge walls, a flat list of `MapPiece` (kind, variant, position,
  yaw) and the spawn points. Tested headless.
- **Generation** (`Systems/MapGenerateSystem`, FixedUpdate) — consumes `MapGenerateRequest`
  (read-then-remove), runs the generator, puts the layout in `MapStorage` and replaces the
  `SpawnPoint` entities (old ones tagged `Destroy`). Anything may ask for a map by publishing
  `GenerateMap { Seed }` or creating the request component.
- **View** (`Systems/MapViewSystem`, Update) — when `MapStorage.Version` changes, throws away the
  old arena and builds the new one from `MapConfig`'s prefab palette (`Utils/MapBuilder`), then
  static-batches it. The view owns GameObjects only; nothing reads gameplay back from them.
- **Palette** — `MapConfig` maps each `MapPieceKind` to a list of prefabs; a piece stores the
  kind and a variant number, never a prefab. The builder places each prefab by the centre of its
  mesh bounds, so kits with different pivots (PolygonGeneric pivots on a corner) need no per-prefab
  offsets — only a per-kind yaw offset where a mesh faces a different way (stairs).
- **Ground** is not a piece: one flat quad under the arena and its outskirts
  (`MapConfig.GroundMaterial`, a triplanar grass) over one box collider. Synty's ground tiles come
  in mismatched sizes and bumps that poke through interior floors; a plane is also the cheapest
  thing to draw on mobile. Interior floors sit `MapGenerator.FloorLift` (2 cm) above it.

### Entry points

- `Providers/MapGenerateOnStart` in a scene publishes `GenerateMap` on start (fixed seed or a
  fresh one).
- Development: `F5` regenerates with a new seed (`Development/MapRegenerateHotkey`); the seed is
  logged.
- Editor: **WheelShooter → Map → Preview In Scene** builds the arena in edit mode without play.
