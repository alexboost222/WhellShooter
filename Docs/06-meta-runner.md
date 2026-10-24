# 06 — Meta & Runner

*Partial: only the sections that exist in code are written so far. Bootstrap, GameState flow and
the Runner's phases are described in the code's own remarks (`Meta/Bootstrap.cs`,
`Engine/Runtime/Runner.cs`) until this page catches up.*

## Ticks & time

**The Runner keeps no clock.** It hands each phase its delta. Systems integrate with the delta
`OnUpdate` gives them and with nothing else.

**`TimeService` (Engine/Runtime) is the one clock**, injected like anything else — systems never
read `UnityEngine.Time`. Bootstrap registers it and calls `Install(runner)` **before any feature
registers**, which puts one step first in FixedUpdate and one first in Update, so every system in a
phase reads the same numbers, and a headless test drives it exactly as play does.

| Member | Meaning |
|---|---|
| `FixedDeltaTime`, `FixedTicks` | the step the last fixed tick ran with; ticks this session |
| `FrameDeltaTime` | the delta of the frame being drawn |
| `InterpolationAlpha` | where this frame sits between the last two fixed ticks, 0…1; 1 before the first tick |

**Interpolation** is what alpha is for: a body moved on the fixed tick keeps `Position` and
`PreviousPosition`, and a view draws `lerp(previous, current, alpha)`. Unity runs a frame's fixed
ticks before its Update, so the service subtracts each fixed step and adds each frame delta; the
remainder is clamped to one step so a hitch never leaves a debt that pins alpha for seconds.

**Deliberately not here (yet):** elapsed game time and a seeded random service. Nothing needs them —
oscillators accumulate their own phase in components, and per-event randomness is seeded from the
event's own index (`Random.CreateFromIndex(shotIndex)`). Add them when the first system genuinely
needs one, and design the random service for determinism (one seed per match).
