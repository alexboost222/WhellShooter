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
  Animators**). Never hand-edited; a rebuild keeps the GUID. The base layer has IK Pass on (for `WeaponHold`).
- **Prefabs** — `Content/Bots/Prefabs/Bot_Rifle` and `Bot_Pistol`: Synty robot + controller +
  weapon prop on the right hand, generated by `Editor/Bots/BotPrefabBuilder` (**WheelShooter →
  Bots → Rebuild Prefabs**). Visual only: no colliders, and the one MonoBehaviour is `WeaponHold`
  below. The gameplay view prefab nests one.

### Holding the weapon

`Providers/WeaponHold`, on the Animator's GameObject, poses the weapon every frame; the weapon is
under `Model`, **not** under a hand bone. Mixamo clips retargeted onto the Synty rig don't keep a
rifle rigid in the right hand: between clips the right-wrist → left-wrist line swings by 15–25°
(up to 55°) in the right hand's frame, so a gun bolted to the right hand is crooked in most clips,
and IK on the left hand alone can't fix that — it moves the hand, not the gun. So:

- **Rifle (two-handed):** the weapon is laid along this frame's animated right-wrist → left-wrist
  line (what the clip means the gun to be), kept upright to the body, the right wrist on its grip;
  the left hand is IK'd (position) onto its place on the weapon.
- **Pistol (one-handed hold):** rides the right hand rigidly; the left hand stays the clip's (it
  wraps the right).
- **Dead:** fades (~0.25 s) to the rigid right-hand hold with the left hand free, so death clips
  play untouched; grabs again on respawn.

The numbers are solved by the prefab builder, not hand-tuned, averaged over 8 frames of every
clip in which the bot holds the weapon, in the right hand's frame:

- the weapon's pistol grip (found on its mesh, just behind and below the trigger) sits in the palm;
- the grip runs along the knuckle line (index → outermost finger; Synty hands have two), which
  fixes the roll;
- the barrel points from the right palm to the left hand's average position (rifle), or along the
  back of the right hand (pistol);
- the wrists' average places on the weapon are what the rifle is laid between; the averaged pose
  itself is the rigid hold.

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

## Feature order

`Meta/Features.cs`: drivers of bodies first (`PlayerFeature`, later bots), then
`CharacterFeature`, `WeaponsFeature`, then views (`ViewmodelFeature`), `EntitiesDestroyFeature`
last. Drivers first so what they ask for this tick/frame is what the body does in the same one.

## Character

`Features/CharacterFeature`. A **body** — the player's or a bot's — is one contract, so neither
side has an edge the other cannot have:

| Component | Written by | What |
|---|---|---|
| `Character` | spawner | tag: this entity is a body |
| `CharacterIntent` | the driver (input, bot brain) | `Move` (local strafe/forward, ≤ 1), `Sprint`, `Fire`, `Aim`; `JumpPressed` / `FirePressed` are **latches** — set by the driver, cleared by the fixed-tick consumer, so a tap shorter than a tick still counts |
| `Look` | the driver, directly | yaw/pitch, radians, Unity convention (+pitch looks down); the authority, not a delta |
| `CharacterMotion` | the motor | feet position, previous position (interpolation), velocity, grounded, sprinting; `Jumps` / `Landings` are **counters**, never cleared, so a view reacts to the difference whatever number of ticks ran between two frames |
| `CharacterTeleportRequest` | anyone (respawn) | consumed before the motor: moves, stops, levels the view, snaps interpolation |
| `CharacterViewLink` | the driver's view hydration | link to the prefab root's `CharacterView` (controller + `Head`) |

- **Movement is steered by yaw only.** Looking up and walking forward never lifts the body.
- **Sprint** needs the button and forward input (> 0.5) and is dropped by aiming or firing.
- **The head is placed every frame in world space**: feet interpolated with
  `TimeService.InterpolationAlpha`, plus eye height, rotated by `Look`. Inheriting it from the
  root would inherit the root's tick-rate stepping.
- **Motor: `CharacterController`, a placeholder.** It is the only thing that knows one exists;
  the phys-r character (Docs/05) replaces it behind the same components.

| Tuning (`CharacterConfig`) | Default | Why |
|---|---|---|
| Capsule height / radius | 1.8 / 0.35 m | set on the controller at hydration; hitscan tests the same numbers |
| Eye height | 1.6 m | camera and shot origin |
| Walk / sprint / aim speed | 4.5 / 7 / 2.8 m/s | sprint ≈ 1.5× walk; aiming slows to a deliberate pace |
| Ground / air acceleration | 45 / 8 m/s² | ~0.1 s to full speed on foot; a jump commits you |
| Jump height | 1.1 m | clears a 1 m block |
| Gravity | 22 m/s² | 9.81 feels floaty in a shooter |

## Player

`Features/PlayerFeature`: the local player as a *driver* of a body.

- **Spawn**: the first `PlayerSpawnPoint` in the scene gets a body; a scene without one has no
  player. The view arrives by hydration (has `Player`, lacks `CharacterViewLink`).
- **Input** (`PlayerInputService` + `PlayerInputSystem`): the `Player` map of
  `Settings/InputSystem_Actions` — Move, Look, Jump, Sprint, Attack, **Aim** (right mouse / left
  trigger). Read every frame into `CharacterIntent` and `PlayerLookInput`; reading is separate from
  deciding so the deciding half is testable headless.
- **Look** is spent every frame, never on the tick. Sensitivity 0.08°/pixel, pitch ±85°.
- Desktop cursor: captured at boot, Escape frees it, a click takes it back; look is ignored while
  free. Touch controls are a separate task.

## Weapons

`Features/WeaponsFeature`, fixed tick, for every body.

- `Weapon { ConfigId, Cooldown, Shots, Aiming }`; a body without one gets
  `WeaponsConfig.DefaultWeapon`.
- **Firing**: automatic fires while held, semi-automatic once per press. At most one shot per tick
  (3000 rpm ceiling at 50 Hz). A running cooldown carries its overshoot so off-grid rates average
  out; idle time is never credit.
- **A shot is `Shots++` plus a `ShotRequest { Origin = eye, Direction = Look.Forward, WeaponId }`**,
  consumed (read-then-remove) by Combat's hitscan. Weapons never clears it.
- Sprinting neither fires nor aims (the motor already drops a sprint when the trigger is held).

| Rifle (`WeaponsConfig`) | Value |
|---|---|
| Fire rate | 600 rpm, automatic |
| Damage / range | 20 / 120 m (read by Combat) |

## Viewmodel (procedural first-person animation)

`Features/ViewmodelFeature`, Update phase, the local player only. **No clips, no Animator**: every
motion is a spring or an oscillator driven by simulation state. View only — it reads the world and
writes three things: the weapon socket's local pose, the camera kick's local rotation, the FOV.

Rig on the player prefab (`ViewmodelRig`): `Head → Kick (camera punch) → Camera, Socket (weapon
pose) → weapon model`. The socket hangs off the kick, so a camera punch carries the weapon.

Systems, in order — the order is the layering:

| System | Does |
|---|---|
| Sweep / Spawn | (re)build the weapon model when the rig is new, died, or the weapon changed |
| Blend | aim blend (over the weapon's `AimTime`), sprint blend, bob weight and phase, breath phase |
| Motion | turn **rate** (°/s, frame-rate independent) and body velocity → motion spring targets; jump/land kicks from the counters |
| Recoil | one kick per shot since last frame; the random part comes from the shot index (`Random.CreateFromIndex`) — no global random |
| Spring | steps every spring once (exact closed form, `Engine/Motion/Springs`) |
| Pose | composes the layers below |
| Apply | writes transforms and FOV |

```
base     hip → aim → sprint pose (smoothstep blends)
motion   (sway + movement lag + landing spring + bob) × aim steadiness
breath   × (1 − bob weight) × aim breath scale
recoil   spring (kick already scaled by aim when added)
camera   punch spring; FOV hip → aim, + a little at full sprint
```

- **Kicks are authored as distances and angles** and converted to a spring velocity (× 2πf), so a
  number reads as "how far it jumps" whatever the stiffness.
- **Bob advances with distance, not time** (π per footstep): the weapon keeps the feet's rhythm at
  any speed and freezes when standing.
- **Counters, not events** (`Weapon.Shots`, `CharacterMotion.Jumps/Landings`): the view remembers
  what it last saw, so 0 or 3 ticks between frames is never a missed or doubled kick. Spawn starts
  with the counters as already seen.
- Recoil is **visual only**; the aim does not move.

| Shared feel (`ViewmodelConfig`) | Default |
|---|---|
| Hip FOV / sprint FOV bonus | 70° / +4° |
| Springs: motion / recoil / camera | 5 Hz ζ0.55 / 9 Hz ζ0.45 / 6 Hz ζ0.6 |
| Look sway | 0.02° per °/s (max 6°), roll 0.012, position 0.08 mm per °/s (max 3 cm) |
| Movement lag | strafe roll 1.1°/(m/s), trail 4 mm/(m/s), fall lift 3 mm/(m/s) max 4 cm |
| Bob | full at 4.5 m/s; step 0.85 m walking / 1.25 m sprinting; 11 × 8 mm, 0.6/0.5/1.0°; ×2.2 at sprint |
| Breathing | 0.28 Hz, 2.5 mm, 0.35° |
| Landing (at 9 m/s impact) | weapon −6 cm, 5° nose-down; camera 2.5° |
| Jump take-off | weapon −2.5 cm, 2.5° nose-up |
| Aiming | motion × 0.15, breathing × 0.4 |
| Sprint blend | 0.22 s |

| Rifle viewmodel (`WeaponsConfig`) | Value |
|---|---|
| Hip / aim / sprint position | (0.16, −0.2, 0.42) / (0, −0.158, 0.3) / (0.1, −0.25, 0.36) m |
| Sprint rotation | (18, −32, 12)° |
| Aim time / aim FOV | 0.18 s / 50° |
| Kick per shot | 4.5 cm back, 0.6 cm up, 4° muzzle up, ±1.2° yaw, ±2.5° roll; camera 0.8° up ±0.35° |
| Recoil while aimed | × 0.45 |

Tuning works live: the configs are assets, and edits made in play mode stay.

**Not yet:** first-person arms (the Synty FPS arms are a bare skeleton — they need a held pose and
hand IK to the weapon), muzzle flash / shell ejection, weapon switching animation (the swap
rebuilds the model instantly), crouch, near-wall weapon pull-back, a viewmodel camera/FOV
separate from the world FOV.

Test scene: `Scenes/Playground_Viewmodel`.
