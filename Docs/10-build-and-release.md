# 10 — Build and release

Mobile first: iOS and Android are the targets; the editor and desktop builds are for development.

## Player settings (both platforms)

| Setting | Value | Why |
|---|---|---|
| Scripting backend | IL2CPP, Release config | several times faster than Mono; Master config for store builds |
| Architecture | ARM64 only (Android); arm64 (iOS) | every supported device; one native phys-r per platform |
| Graphics API | Metal (iOS); Vulkan, GLES3 fallback (Android) | the GPU Resident Drawer needs Metal / Vulkan; GLES3 devices fall back to ordinary draws |
| Managed stripping | Minimal | DI fills `[Inject]` fields by reflection — stronger stripping removes them |
| Incremental GC | on | spreads collections over frames |
| Optimized frame pacing (Android) | on | even frame times on Swappy-capable devices |
| Accelerometer | off | unused; it was being polled at 60 Hz |
| Frame rate | 60 (`UnityRunnerDriver.MobileFrameRate`) | mobile players default to 30 |
| Quality level | Mobile on iOS / Android (`Mobile_RPAsset`), PC on desktop | |

`companyName`, `productName` and the bundle id are still the template's
(`com.Unity-Technologies.com.unity.template.urp-blank`). They are contracts once shipped — set them
deliberately, once, before the first store build.

## Rendering budget

Measured in the editor (PC asset, so the device is lighter), default arena, start of a match:

| | Before | Now |
|---|---:|---:|
| Draw calls | 6 530 | ~500 |
| Triangles (incl. shadows) | 1 030 k | 230 k |
| Shadow casters | 2 900 | ~250–310 |
| Main thread (editor, M5 Pro) | 8.6 ms | ~3 ms |
| Simulation (all features, phys-r) | 0.2 ms | 0.15 ms |

What got it there, and the rules that keep it there:

- **GPU Resident Drawer** (`InstancedDrawing`, both URP assets) with GPU occlusion culling: the
  arena is a few hundred distinct meshes repeated ~1 800 times, and the drawer instances them.
  Requires **Forward+** (both renderers) and **BatchRendererGroup variants: Keep All** (Graphics
  settings). **Don't static-batch** anything it should draw — it skips static-batched renderers.
- **Shadows only from what matters**: grass, flowers, bushes, floors and ceilings don't cast
  (`MapBuilder`). Mobile: 1 cascade, 1024², 35 m, hard shadows, no additional-light shadows.
- **Mobile asset**: no HDR, render scale 0.8, no MSAA.
- **No geometry nobody can see**: nothing low outside the border wall; ground detail at 0.8.
- **No per-frame garbage** in game code: labels are written on change, no lambdas in per-tick
  paths. What remains is tens of bytes per frame.

Re-measure on a device after anything that adds renderers — the editor's own UI costs ~3 ms a
frame and hides small regressions.

## Native plugins

phys-r: `Packages/com.physr.engine/Plugins/` per platform (Docs/11). A platform without its
binary fails at the first physics call; all of macOS, Windows, iOS and Android arm64 are in.

## Building

Builds run through the editor (CLAUDE.md) into `Builds/`. **This machine has the iOS module for
6000.3.14f1 but not Android**: an Android build needs the Android module (SDK, NDK, OpenJDK)
installed for this editor first.
