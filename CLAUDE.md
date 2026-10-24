# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with
code in this repository.

## What this is

**WheelShooter** — a fast-paced first-person co-op shooter demo with a
dedicated server and one or more clients (Unity 6000.3.14f1, URP, Input System).
The repo is at the very start: the Engine core and Bootstrap exist (see *What
exists*), no gameplay yet. Everything below is the architecture carried over
from `~/personal/dacha-simulator` (`dev-2`), where it is designed and proven. When a rule here is unclear, the matching
`dacha-simulator/Docs/` page explains why it exists — read it, but do not copy
dacha's game-specific parts (Steam, persistence details) unless WheelShooter
actually needs them. Netcode and co-op sessions **are** needed (dedicated
server + clients) — use dacha's as the reference.

**Game design:** [`Docs/08-gameplay.md`](Docs/08-gameplay.md) — co-op FPS
demo, dedicated server, procedural runs between safe zones.

## Docs first

Architecture is designed docs-first in **`Docs/`**. Each area
gets its doc **before** its first implementation, mirroring dacha's numbering
where it applies:

| Doc | Area |
|---|---|
| `01-project-structure` | folders, asmdefs, git, dependency direction |
| `02-engine` | DI, message bus, UI framework (MVVM / `Bindable<T>`), module list |
| `03-ecs` | Morpeh conventions: features, systems, patterns |
| `04-persistence` | saves, prefs |
| `06-meta-runner` | bootstrap, GameState flow, Runner, ticks & time |
| `08-gameplay` | **what the game does**: feature roster, tuning numbers |
| `09-roadmap-and-open-design` | **not law** — unsettled design, open questions |
| `10-build-and-release` | Android/iOS builds, signing, store upload |

Once a doc exists it is law: if implementation genuinely fights it, say so and
amend the doc in the same change — never silently diverge.

## Stack

URP 17.3, Input System 1.19, Test Framework 1.6, UGUI (+TMP), and:

- **`com.scellecs.morpeh` 2024.1.1** (OpenUPM) — exactly this; the `2025.x`
  API is incompatible with every convention below (no `SystemsGroup`,
  `IDataComponent`).
- `com.unity.mathematics` 1.3.3, `com.cysharp.unitask` 2.5.11 (OpenUPM).
- `com.unity.pipeline` — the official Unity CLI bridge used for editor automation.

Project settings carried over from dacha: Enter Play Mode Options with domain
and scene reload **disabled**, physics `SimulationMode.Script` (the Runner
calls `Physics.Simulate`).

## Layout

```
Assets/
  Content/<Feature>/   # models, materials, prefabs, VFX — feature-first; Shared/ for cross-feature
  Scenes/
  Scripts/
    Engine/            # game-agnostic: DI, Messages, Runtime (Runner), Configs, Ecs, Ui, Prefs
    Features/<Name>/   # one self-contained ECS feature per folder (shape below)
    UI/                # screens only (Engine/Ui holds the framework)
    Meta/              # bootstrap, ordered feature list, scene/game flow
    Development/       # dev tools, cheats — strippable from release
    Editor/            # editor tooling (UI Toolkit for anything new)
  Settings/            # URP assets, volume profiles, input actions
  Resources/
    Configs/           # config ScriptableObjects — one asset per config type
    Bootstrap/         # persistent prefabs the composition root instantiates
  Tests/EditMode/  Tests/PlayMode/
```

Feature folder — **nothing at its root except `XxxFeature.cs`**, one type per
file, sub-folders map to sub-namespaces:

```
Features/Shooting/
  ShootingFeature.cs   # installer: AddInitializer(storages) + AddSystem(phase, system)
  Components/          # pure-data structs — state, requests, events
  Systems/             # systems + the XxxStorage / XxxService beside them (IInitializer)
  Configs/  Providers/  Enums/  Utils/  Constants/   # only when needed
```

asmdefs: `Engine` (references only Unity + third-party), `Game` (Features, UI,
Meta, Development), `Game.Editor`, plus test assemblies (`Engine.Tests` must not
reference `Game`; game-side suites get their own asmdef). `rootNamespace`
supplies only what the folder path doesn't (`Engine` asmdef → empty).

## Hard rules (the ones that are easy to violate accidentally)

- **ECS-first.** If it's doable in ECS, it's done in ECS. State is components
  (game flow included — `GameState` singleton + request components); logic is
  systems. UI/services only feed or observe the world.
- **Dependency direction:** `Editor → Development → Meta → UI → Features →
  Engine`. `Engine/` references nothing of ours. Litmus: *would this file
  compile unchanged in a different game?* → Engine. Engine owns the mechanism
  (Runner, Feature base, Container), Meta owns the manifest (which features, in
  what order).
- **A feature never calls another feature's systems** — it reacts to its
  components. Cross-feature intent is a request component, consumed
  (read-then-remove) by the owning feature.
- **One ordered feature list in Meta** — no attribute auto-registration. Order
  is the contract: a feature may only inject what earlier features registered.
  `EntitiesDestroyFeature` is last.
- **Nothing disposes entities mid-tick** — tag `Destroy`; the last feature
  disposes them.
- **The Runner owns the loop.** Morpeh is driven manually, phases
  `FixedUpdate` (simulation + manual `Physics.Simulate`, `SimulationMode.Script`)
  / `Update` (views) / `LateUpdate` (only when an Animator fights the view).
  Morpeh commits after each system. Game code never touches the Unity loop.
  The fixed step is Unity's own (`ProjectSettings/TimeManager`); the Runner
  keeps no clock.
- **No `Time.*` / `Random.*` statics in systems** — inject `TimeService` /
  the seeded random service. (Neither exists yet: `TimeService` was
  deliberately left out of the Engine port — design it before the first system
  that needs time.)
- **`Unity.Mathematics` inside ECS** (`float3`, `quaternion`, `math.*`);
  `Vector3`/`Quaternion` only at the Provider/view boundary.
- **Components store ids** (config ids, entity ids) — never SO references,
  never `Entity` refs held across ticks unchecked. Object references only in
  local-only link components (`GameObjectLink`), and a link present means a
  live object.
- **DI:** one scope, `[Inject]` fields, fail-fast at boot with every miss
  listed, no auto-wiring, registering a contract twice throws. Resolvers in
  order: instance → config → stash → message pipe.
- **MonoBehaviours are views/providers only** — no logic. They may carry
  `[Inject]` fields, filled via `container.InjectHierarchy(go)` / `InjectScene`.
  Spawn prefabs with providers via **`container.InstantiateInjected(prefab)`**,
  never `Instantiate` + inject (Awake would run with null fields). They are
  never resolved *from* the container.
- **UI never mutates gameplay state.** Views bind ViewModels (`Bindable<T>`,
  exposed as `IReadOnlyBindable<T>`); ViewModels read storages/components
  (`OnTick`) and write only by publishing bus messages. Systems drain
  `SubscriberQueue<T>`. Messages are plain structs with ids, no object refs.
- **Stable names are contracts:** saved component names, message type names,
  `Pref` keys — and once the game ships, **`companyName`, `productName` and the
  bundle id** (they build `persistentDataPath` and the store identity). Renaming
  is a save break. (They are still template defaults — set them deliberately,
  once, before the first build.)
- **Code style** (dacha's system layout): member order injected stashes →
  other injects → `World` / state → `OnAwake` → `Dispose` → `OnUpdate` →
  helpers. Stash field = component name, singular (`Stash<Health> _health`).
  The single filter is `_filter`; `With`/`Without`/`Build` one per line.
  Always write `private` explicitly. Braces always. No expression-bodied methods
  (properties are fine). Systems are `sealed`.
- **`Filter.IsEmpty()` is not "no entities"** — use `FirstOrDefault() != null`
  or `GetLengthSlow()`.

## What exists

Ported from dacha (adapted: no netcode, no metrics, no TimeService):
`Engine/DI`, `Engine/Messages`, `Engine/Runtime` (Runner, `Feature`,
`UnityRunnerDriver`, `SceneLoader`), `Engine/Configs` (core: `Config`,
`ConfigCatalog`, `ConfigResolver`, `ConfigIds`), `Engine/Ecs` (`EcsFeature`,
`StashResolver`, `SystemsGroupStep`, `Destroy`), `Engine/Disposables`, and a
minimal `Engine/Diagnostics` (`Log`, `Guard`, `MainThread`).
`Meta/Bootstrap.cs` is the composition root; `Meta/Features.cs` is the ordered
feature list, with `EntitiesDestroyFeature` last.

## Workflow constraints

- **Author scenes, prefabs and configs through the open editor via the Unity
  CLI** (`unity command …`; `unity skill show` is the guide) — `script eval`
  with `SerializedObject`, or the `gameobject`/`component`/`prefab` commands.
  Never hand-write `.unity`/`.prefab`/`.asset` YAML. **Then verify in play
  mode.**
- **Never rename or move asset files/folders from the shell** (`.meta` GUID
  breakage). Creating new source files is fine.
- **We test. Everything testable gets tests** (Unity Test Framework). Engine
  modules get edit-mode unit tests (pure C#); ECS features get integration
  tests against a headless Morpeh world ticked by hand. Play mode only for what
  needs a real play loop (boot, scene loads, MonoBehaviour callbacks). New
  Engine/sim code without tests is incomplete. Run tests through the editor
  (the `unity-tests` skill).
- Domain reload is meant to be disabled: no static state that outlives play
  mode; `Bootstrap.Shutdown` nulls its statics; the runner driver uses
  `DontDestroyOnLoad`, never `HideFlags.DontSave`.
- Player builds run through the Unity editor; they land in `Builds/`.
- `Library/`, `Temp/`, `obj/`, `Logs/`, `Builds/`, `UserSettings/` are
  generated — never search or edit.
- Git: LFS for binaries only; Unity YAML and `.meta` are plain text merged with
  UnityYAMLMerge (see `.gitattributes`). Never run a blanket `git lfs migrate`.
  Default branch `main`.
