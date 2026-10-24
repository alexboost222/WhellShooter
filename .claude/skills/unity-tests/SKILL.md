---
name: unity-tests
description: Run the Unity test suite (EditMode and/or PlayMode) in the open editor through the official Unity CLI (`unity command …`, Pipeline package) and report results — recompiles first, clears the console, runs the tests, then reads back only the real failures. Use whenever asked to run tests, re-run tests after a code change, check whether something compiles, or verify Engine/ECS work in the editor.
---

# Running Unity tests via the Unity CLI

Drives the open editor's Test Runner through the official Unity CLI (`unity`,
`~/.unity/bin/unity`) and the project's `com.unity.pipeline` package. Unity MCP
is not used. Player builds go through the editor (see CLAUDE.md) — this is
the way tests get run.

## Preconditions

    unity status

must list this project (`/Users/stas/personal/WheelShooter`) as `ready`.
Run every command from the repo root, or pass `--project-path`, so the CLI
targets this project's editor and not a worktree's.

- **No instance listed / connection refused** — the editor is closed or booted
  into **Safe Mode** (compile errors → the Pipeline package never loads).
  `unity pipeline list` confirms. Fix the compile errors and restart Unity;
  don't fall back to hand-editing assets.
- A timeout or a dropped connection is reported to the user, not retried in a
  loop.

## Procedure

### 1. If C# changed, recompile first

Tests run against the last successful compile, so a broken build silently
yields stale results:

    unity recompile --format json

Non-zero exit / errors in the report mean stop — fix them and recompile again.

### 2. Clear the console

    unity command console clear

Do this **before** the run, not after. It's what makes step 4 meaningful —
otherwise fresh failures are buried in unrelated backlog.

### 3. Run the tests

**Scope the run to what the change can actually break.** Full-suite runs are
a real cost, so an unfiltered sweep is the exception, not the default:

- Working on one area → filter by name prefix (case-insensitive partial
  match): `--filter Game.Tests.Shooting`. Iterating on a single failure → re-run
  that one test by its full name.
- One assembly → `--filter Engine.Tests --filter_type assembly` (also
  `category`). The filter is a single pattern; for two areas run two calls.
- Add PlayMode only when the change touches boot, scenes, prefabs or authored
  assets (see Modes below) — not reflexively.
- Reserve the whole suite for before a commit, or when a narrow run passed but
  you suspect wider fallout.

Say which filter you used when reporting, so a narrow pass never reads as full
coverage.

The call is synchronous. **Raise the CLI's own `--timeout`** (default 30 s,
it kills the wait — not the run) above the expected duration:

    unity command test run --mode EditMode --filter Game.Tests.Shooting \
      --result-only --timeout 600 > <scratchpad>/tests.json

Don't dump the result into context: it lists every passing test. Extract the
summary and failures with `jq`:

    jq '.Summary, .Duration, [.Results[] | select(.Status=="Failed")
        | {FullName, Message}]' <scratchpad>/tests.json

Pull a `StackTrace` only for the failure you're actually investigating.

**PlayMode must run async** — entering play mode reloads the domain and drops
a synchronous request (it comes back as `Total: 0` with an error). Run
`unity command test run --mode PlayMode --async_tests true --result-only` and
poll `unity command test status --result-only` until `status` is `completed`
(its keys are lowercase: `.summary`, `.results`). `--async_tests true` also
suits a long EditMode run. `unity command test cancel`
releases a wedged Test Runner. `unity command test list --mode EditMode
--result-only` lists tests without running them (`jq` on `.Tests[].Assembly`
for counts).

### 4. Read back what the run logged

    unity command console get --level warn --tail 30 --result-only

`console status` gives counts plus `groundTruth.compilationFailed` without
pulling entries.

## Modes

`--mode EditMode` / `--mode PlayMode` (omitted = both).

**EditMode** is the bulk of it: `Engine.Tests` and the `Game.Tests.*`
assemblies (see CLAUDE.md for the layout).

**PlayMode** (`Game.Tests.PlayMode`) covers only what needs a real play loop —
boot, scene loads, MonoBehaviour callbacks. **Run both modes for any change
that touches boot, scenes, prefabs or authored assets**, since those are
precisely what EditMode cannot see.

## Reading results honestly

`success: true` with `Summary.Total: 0` is **not** a passing suite — it means
nothing ran (usually a filter typo). Always report `Passed`/`Failed` out of
`Total`, plus `Skipped`.

### Console noise that is not a problem

A clean run still leaves these behind. Do not report them as failures:

- **`"Saving results to: .../TestResults.xml"` typed as `Exception`.** The Test
  Framework logs its own results path at exception severity. Benign.
- `Executing IPrebuildSetup` / `IPostBuildCleanup for:
  Unity.PerformanceTesting.Editor.TestRunBuilder` warnings.
- Stack traces and `[ERROR]` lines logged by *passing* tests that
  deliberately exercise failure paths and assert on the fallout.

Judge pass/fail from the run's `Summary`, not from console severity.

## Reporting

State the counts and the duration, list genuine failures with their full test
names, and say explicitly which modes and filter ran. If PlayMode was skipped,
say so — don't let it read as coverage that exists.
