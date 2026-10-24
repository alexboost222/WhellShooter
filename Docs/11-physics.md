# 11 — Physics

The simulation's physics is **phys-r** (`Packages/com.physr.engine`): a deterministic fixed-point
(Q40.24) Rust engine with rigid bodies, an FPS character controller, raycasts, lag-compensated
rewinds and snapshots. **Unity's PhysX is not stepped and not queried by game code.**

Why: on the shooter's real workload phys-r is several times cheaper than PhysX, and it gives us
bit-identical simulation across ARM/x86 — the precondition for rollback netcode and replays. The
editor benchmark (`Assets/Tests/Benchmarks/Physics`, run with
`--filter Benchmarks.Physics`) measured, per 50–60 Hz tick:

| Scene | PhysX | phys-r |
|---|---:|---:|
| 16 characters on a 5k-box level | 208 µs | 8 µs |
| 64 characters | 612 µs | 29 µs |
| FPS tick: 16 characters + 64 props | 283 µs | 49 µs |
| 256 single raycasts across the level | 216 µs | 208 µs |
| 500 awake dynamic props (sleep off) | **480 µs** | 1150 µs |

The one place PhysX wins is piles of awake rigid bodies: phys-r's solver is single-threaded. The
design budget follows from that — **props are a garnish, not the game**: few, small, allowed to
sleep. Re-run the benchmark on a device before raising that budget.

## Layers

```
Engine/Physics        PhysicsWorld (owns the native PhysWorld), PhysicsStep (the Runner's physics
                      hook), PhysicsMath (float3 <-> FP at the boundary)
Features/Bodies       the ECS bridge: request components in, link + mirror components out
Features/<gameplay>   ask for bodies with request components, read the mirrors, query PhysicsWorld
```

- **`PhysicsWorld`** is an engine service: Bootstrap creates it, registers it in the container
  and disposes it on shutdown (native memory — domain reload is off, so a leaked world survives
  play mode). Its tick is Unity's fixed step (`ProjectSettings/TimeManager`, 0.02 s → 50 Hz),
  read once at boot; the Runner still keeps no clock.
- **`PhysicsStep`** is the Runner's `PhysicsHook`: after every FixedUpdate step it runs a
  *before* group, `PhysicsWorld.Step()`, then an *after* group. It throws if the Runner's delta
  ever differs from the world's tick — a changed fixed step needs a restart, not a silent drift.
  The hook can be installed once, before `Runner.Initialize`.
- **`BodiesFeature`** (`Features/Bodies`) owns both groups and installs the hook. Because all its work runs inside
  the hook, **after every gameplay system and after `EntitiesDestroyFeature`**, its place in
  `Features.cs` doesn't affect timing; it sits first among gameplay features by convention.
  (Not `Features/Physics`: a `Features.Physics` namespace would shadow `UnityEngine.Physics`
  for every file under `Features.*`.)

## The tick

```
FixedUpdate steps (gameplay)        write PhysicsCharacterInput, create *CreateRequest, tag Destroy
EntitiesDestroyFeature              disposes tagged entities
PhysicsHook = PhysicsStep
  before: PhysicsReleaseSystem      native objects of disposed / unlinked entities are destroyed
          PhysicsCreateSystem       requests → native bodies / characters; request removed
          CharacterTeleportSystem   PhysicsTeleportRequest → snap
          CharacterInputSystem      PhysicsCharacterInput → native input
  PhysicsWorld.Step()
  after:  CharacterReadbackSystem   native state → PhysicsCharacterState
          BodyReadbackSystem        native pose → BodyPose (dynamic / kinematic bodies)
AfterPhysics steps                  other features turn the result into gameplay state
                                    (CharacterMotionSystem: PhysicsCharacterState → CharacterMotion)
```

So a request made in tick *N* exists in the world for tick *N*'s step, input written in tick *N*
moves the character in tick *N*, and `AfterPhysics` systems — and views that frame — already see
tick *N*'s result. `RunnerPhase.AfterPhysics` is the generic place for that; nothing in it should
move bodies, which waits for the next step anyway.

## Components (`Features/Bodies/Components`)

| Component | Who writes | Meaning |
|---|---|---|
| `BodyCreateRequest` | anyone | kind (static / dynamic / kinematic), shape (box / sphere / capsule), size, pose, mass, layer |
| `CharacterCreateRequest` | anyone | feet position; capsule, step, speeds, accel, gravity, jump (speed or height) — 0 = phys-r default |
| `PhysicsBody` | Bodies | link: the native body handle. Present ⇔ the body exists |
| `PhysicsCharacter` | Bodies | link: the native character handle and its body handle |
| `PhysicsCharacterInput` | the character motor | this tick's move (local x/z, −1..1), yaw (radians about +Y, 0 = +Z), speed cap, jump / sprint / crouch |
| `PhysicsTeleportRequest` | the character motor | snap the character's feet somewhere (respawn) |
| `PhysicsCharacterState` | Bodies | after the step: feet (and previous), velocity, grounded, crouched, landed / jumped this step, height, landing speed |
| `BodyPose` | Bodies | after the step: position / rotation (and previous) of a non-static body |

- **Handles are ids, not references** (CLAUDE.md): a `ulong` the native world validates; a stale
  one is rejected by phys-r, never dereferenced.
- **Removing an entity is enough.** Tag it `Destroy` (or remove the link component); the next
  `PhysicsReleaseSystem` destroys the native object. Physics keeps its own (entity, handle) list
  and checks `IsDisposed` — nothing holds an `Entity` across ticks unchecked.
- **Moving a character is input, not position.** These are the *physics-level* components. The
  gameplay contract — `CharacterIntent`, `Look`, `CharacterMotion` — belongs to `CharacterFeature`,
  whose motor turns intent into `PhysicsCharacterInput` and `PhysicsCharacterState` into
  `CharacterMotion`. There is exactly one `CharacterMotion`, and Bodies doesn't define it.

## Queries

**A body is queryable from the step after it was created** — phys-r adds new bodies to its
query structures during the step. Bodies creates them right before the step, so for systems
this is invisible: a request made in tick *N* is hit by queries from tick *N+1* on.

Systems inject `PhysicsWorld` and call `Raycast` (float3 in, `PhysicsRayHit` out: distance,
point, normal, body handle). Layers are phys-r's 32 bits (`PhysicsLayers` constants in
`Features/Bodies`); `QueryFlags.ExcludeMoving` gives "static world only", which is what a shot
occluder wants. For the raw API (batched rays, rewinds, sweeps, overlaps) use
`PhysicsWorld.Native`.

## Units and determinism

- The boundary is **`float3` / `quaternion` in ECS, `FP` in phys-r**. `PhysicsMath` converts;
  float → FP goes through `FP.FromFloat` (native, exact on every platform).
- phys-r is bit-identical; our float gameplay around it is not. With a dedicated server
  (CLAUDE.md) the server is authoritative, so that's enough to start; client prediction and
  reconciliation want quantized inputs (`FP.FromRatio`) and replaying the character from a phys-r
  snapshot — design that in `05-networking`, not here.
- The dedicated server needs the native library for its platform too (Linux x86_64:
  `build-unity-plugins.sh linux`).

## Rules

- **No `UnityEngine.Physics`, `Rigidbody`, `CharacterController` or `Collider` in game code.**
  Views carry no colliders; level geometry becomes static bodies from data — the arena's come from
  `MapLayout` via `Features/Map` (`MapCollision`, Docs/08) — never from baked scene colliders. `Physics.simulationMode` stays `Script` and nothing calls
  `Physics.Simulate`, so a stray collider is inert.
- Nothing touches the native world outside the physics groups, except **read-only queries**
  from systems (raycasts between steps are fine — the world isn't mid-step).
- Bodies are created only by `PhysicsCreateSystem`, in entity order — deterministic given the
  same requests.

## Native plugins

| Platform | File | Built by |
|---|---|---|
| macOS (Editor, standalone) | `Plugins/macOS/libphysr.dylib` (universal) | `scripts/build-unity-plugins.sh macos` in phys-r |
| Android | `Plugins/Android/<abi>/libphysr.so` | `… android` (NDK + `cargo-ndk`) — **not built yet** |
| iOS | `Plugins/iOS/libphysr.a` | `… ios` — **not built yet** |
| Linux (dedicated server) | `Plugins/Linux/x86_64/libphysr.so` | `… linux` — **not built yet** |

The package is embedded (`Packages/com.physr.engine`, phys-r `9e10ac3`); binaries are in LFS.
To update: rebuild in a phys-r checkout and copy `unity/com.physr.engine` over the embedded one.

## Open

- **Render interpolation.** Views read `CharacterMotion` / `BodyPose`, which carry the previous
  pose too, but nothing yet supplies the alpha between fixed ticks (the Runner keeps no clock;
  it belongs with `TimeService`).
- Knockback / kinematic targets / contact and sensor events: add with the first
  feature that needs them.
- Snapshots and rollback: phys-r supports them; we don't use them until netcode.
