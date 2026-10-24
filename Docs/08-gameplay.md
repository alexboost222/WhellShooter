# 08 — Gameplay

What the game does: the feature roster and its tuning numbers. Unsettled ideas go to
`09-roadmap-and-open-design`, not here.

Feature order (`Meta/Features.cs`): drivers of bodies first (`PlayerFeature`, later bots), then
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
