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

## Bots — animation

`Content/Bots`. Bots are Synty humanoids animated with Mixamo clips (Rifle 8-Way and Pistol
locomotion packs). The animation layer is view-only: ECS moves the bot, the Animator only shows it.

### Assets

- **Clips** — `Content/Bots/Animations/{Rifle,Pistol}/*.fbx`, one clip per file named after it.
  Import rules live in `Editor/Bots/BotAnimationImporter` (an `AssetPostprocessor`): Humanoid,
  locomotion looped, deaths / jumps / turns one-shot. Rotation and height are baked into the pose;
  horizontal travel stays root motion that is **never applied** (`applyRootMotion = false`) — walk
  cycles play in place, and the movement speed belongs to ECS alone. Deaths bake their stumble so
  the body falls where the animation puts it.
- **Controllers** — `Content/Bots/Animators/Bot_Rifle` and `Bot_Pistol`, generated by
  `Editor/Bots/BotAnimatorBuilder` from `BotAnimationSet` (**WheelShooter → Bots → Rebuild
  Animators**). Never hand-edited; a rebuild keeps the GUID.
- **Prefabs** — `Content/Bots/Prefabs/Bot_Rifle` and `Bot_Pistol`: Synty robot + controller +
  weapon prop on the right hand, generated by `Editor/Bots/BotPrefabBuilder` (**WheelShooter →
  Bots → Rebuild Prefabs**). Visual only: no colliders, no MonoBehaviours. The gameplay view prefab
  nests one. The weapon socket is solved from the aiming pose (barrel from the right hand to the
  left), not hand-tuned.

### Parameters (the contract — `Features/Bots/Constants/BotAnimatorParams`)

| Parameter | Type | Meaning |
|---|---|---|
| `MoveX`, `MoveZ` | float | planar velocity in the bot's local frame, m/s (x right, z forward); set with ~0.1 s damping |
| `Grounded` | bool, default true | false → jump up, hang; true → land |
| `Crouch` | bool | crouch locomotion (rifle: 8-way crouch walk; pistol: kneel only) |
| `Aim` | float 0..1 | standing idle → aiming idle (rifle) |
| `Dead` + `DeathVariant` | bool + int 0..5 | the picked death; `Dead = false` returns to locomotion (respawn) |

The locomotion blend trees are 2D freeform-directional; each clip sits at its measured average
velocity, so feet match any speed ECS moves at:

| Set | Walk | Run | Sprint | Backward walk / run |
|---|---|---|---|---|
| Rifle (8 directions) | 1.85 m/s | 4.5 m/s | 6.6–6.9 m/s | same as forward |
| Pistol (4 directions) | 2.5 m/s (strafe 2.2–2.5) | 4.7 m/s, forward / back only | — | 1.4 / 3.4 m/s |

The pistol pack has no airborne loop and no deaths, so its controller borrows the rifle's
(retargeted). Not used yet: turn-in-place clips, pistol arcs and kneel transitions, upper-body aim
by pitch.

## Navigation

`Features/Navigation`. What an agent needs to know about the arena, built from the map's `MapLayout`
whenever `MapStorage.Version` moves (`NavGridBuildSystem`, same tick) and served by `NavStorage`.
Pure C# (`Utils/NavGrid`, `Utils/NavPathfinder`), tested headless.

| Question | Rule |
|---|---|
| Walk | walkable cells (`Open`, `Interior`); a step crosses only `None` or `Door` edges; a diagonal only when both ways round it are open |
| See | blocked by `Wall` / `Border` edges and by **high** cover; windows, doorways and low cover are see-through |
| Path | 8-connected A* (octile heuristic, ties on cell index → deterministic), then string-pulling with the body's radius so a path never shaves a corner |
| Off-grid goal | snapped to the nearest walkable cell (≤ 3 cells), same for a start pushed into cover |

Ground level only: roofs and stairs are not in the grid yet (Docs/09). A query allocates nothing
once the pathfinder is warm; a path belongs to whoever asked (`NavStorage` keeps no agent state).

## Combat

`Features/Combat`. Health, teams, hits and death — for every body, player and bots alike.

| Tuning (`CombatConfig`) | Default |
|---|---|
| Max health | 100 |
| Friendly fire | off (free-for-all bodies are always fair game) |

- **Teams.** `Team { Index }` uses the map's numbering (0, 1, …, `FreeForAll = −1`); a body without
  one is free-for-all. One rule, `CombatRules.AreHostile`: free-for-all on either side, or
  different teams.
- **Hits.** The weapon puts a `ShotRequest` on the shooter; `HitscanSystem` consumes it the next
  tick (read-then-remove): the static world through `IShotOccluder` (phys-r, `ExcludeMoving`), every
  living body analytically as the capsule `CharacterConfig` describes on its feet — so who got hit
  is decided the same headless as in play. A hit becomes a `Damage` entity.
- **Damage** entities (any number per body per tick) are consumed by `DamageApplySystem`: health
  down, `DamageTaken { Hits↑, LastSource, LastOrigin }` on the victim, `Dead { Killer, Direction }`
  at zero. The only place health goes down.
- **Death** keeps the entity: a dead body still falls and can be teleported, but
  `DeadIntentSystem` clears its `CharacterIntent` every tick and it can't be hit.
  `ReviveRequest` (consumed by `ReviveSystem`) refills health and clears `Dead`; whoever respawns
  the body adds it together with a `CharacterTeleportRequest`.
- **Order.** Combat is listed after the drivers (player, bots) and before the body and the weapon:
  last tick's shots land, then the dead are silenced before the motor reads their intent.

## Bots — AI

`Features/Bots`. **A bot is the player's body with a brain instead of a person.** It writes the same
`CharacterIntent` and `Look` the player's input does, so it moves with the same motor, fires the
same weapon through the same hitscan, and has no rule the player doesn't. It misses because its
look wobbles, not because a dice roll says so.

### The brain: utility AI in ECS

Every think (0.2 s, staggered) each behaviour **bids** a score; the best bid becomes the running
action. A behaviour is four pieces and nothing else changes when one is added:

| Piece | Example (Attack) |
|---|---|
| `BotAction` value | `BotAction.Attack` |
| state component, present while it runs | `BotAttack` |
| score system — `brain.Offer(action, score)` on think ticks | `BotAttackScoreSystem` |
| executor — acts every tick on entities with the state component | `BotAttackSystem` |

…plus one case each in `BotActionSet.Enter/Exit`. The running action gets a commitment bonus
(+0.1) so near-equal bids don't flicker.

| Behaviour | Bid | Does |
|---|---|---|
| Wander | 0.1, always | walk to a random point ≥ 8 m away, idle 0.4–2 s, repeat; looks where it walks |
| Attack | 0.8 × range curve (1 at preferred range, ≥ 0.6) while an enemy is visible | aims at the chest, fires once reaction time has passed and the look is on the aim point; closes in beyond 1.4 × preferred range, backs off inside 0.5 ×, strafes side to side otherwise |
| Chase | 0.5 fading to 0 over the memory, while the enemy is lost | sprints to the last known position looking that way; on arrival with nothing there the chase is exhausted and bids 0 |

### Systems (FixedUpdate, in order)

spawn → respawn → **senses** → think → bids (wander, attack, chase) → decide → executors (wander,
attack, chase) → **path** → **steer** → **aim**. Update: view hydration, animator.

- **Senses** (`BotSensesSystem`, scan every 0.15 s): nearest hostile living body within view
  distance, inside the field of view (or within the awareness radius whatever the facing), with
  ground line of sight over the nav grid; the current target is preferred (distance × 0.7). A
  visible target's position updates every tick. Lost targets are remembered for `Memory`
  seconds. A hit from an unseen enemy makes the shooter the target at its position (→ chase,
  turn round).
- **Path** (`BotPathSystem`): re-pathed when the destination moves > 1 m, the grid changes, the
  follower flags it stuck, and every 1.5 s. No path → `BotMove.Failed`, the action picks again.
- **Steer** (`BotSteerSystem`): next corner → local move input relative to the look's yaw (so
  looking at an enemy while walking sideways is just strafing input), slows into the last corner,
  arrives within 0.6 m. Progress watchdog: < 0.3 m in 1 s re-paths; three in a row fail the move.
- **Aim** (`BotAimSystem`): turns `Look` towards the aim point (or the next corner while walking),
  rate-limited. **Aim error is the accuracy model:** a target error re-rolled every 0.25–0.6 s,
  `AimError` degrees on acquisition shrinking to `SettledAimError` × that over `AimSettleTime` of
  tracking. `OnTarget` (look within `FireTolerance` of the wobbling aim) gates the trigger.
- **Spawn** (`SpawnBots { Count, Teams, ProfileId }` message → `BotSpawnRequest`, waits for spawn
  points): a full body plus the AI's components, placed by a teleport; teams alternate 0/1 or
  free-for-all. Spawn and respawn pick the spawn point of the bot's side farthest from every
  living enemy. Respawn after 4 s: teleport + `ReviveRequest`, brain reset.
- **Determinism:** every roll comes from the bot's own `BotRandom` (seeded from the map seed and
  the bot's index); the death clip is a hash, so the view never consumes the stream.

### Profiles (`BotsConfig`, one row per skill level; a bot stores the name's hash)

| Tuning | Regular |
|---|---|
| View distance / field of view / awareness radius | 45 m / 120° / 4 m |
| Memory | 6 s |
| Reaction time | 0.35 s |
| Aim error → settled share / settle time | 3° → 0.3 / 1.2 s |
| Turn speed / sharpness | 300°/s / 10 |
| Fire tolerance | 2.5° |
| Preferred range | 14 m |
| Strafe interval / distance | 0.8–2 s / 2–4 m |

### View

`BotsConfig.ViewPrefab`: root with the body's `CharacterView` (head) and `BotView`;
the animated `Bot_Rifle` nested as `BotView.Model`. `BotViewSpawnSystem` builds it through
`BotViewFactory` (`InstantiateInjected`) and adds
`CharacterViewLink` + `BotViewLink`. `BotAnimatorSystem` places the model at the interpolated
feet turned by the look and drives the animation contract above.

### Entry points

- `Providers/BotsSpawnOnStart` in a scene publishes `SpawnBots` on start.
