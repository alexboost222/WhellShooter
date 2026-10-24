# 05 — Netcode

Dedicated server + clients over **LiteNetLib** (UDP, unreliable only). The
server is authoritative and runs the simulation in **ticks**; clients send
their input for every tick and the server consumes it on that exact tick
(input lockstep: same tick numbering everywhere, server decides).

## Tick

- One tick = one Runner fixed phase, `Time.fixedDeltaTime` long (`0.02` s →
  50 Hz). **Not** Unity's `FixedUpdate`: `UnityRunnerDriver.Update` adds the
  frame's unscaled delta to a `TickAccumulator` and runs as many ticks as it
  owes — 0, 1 or more — capped at `MaxTicksPerFrame` (8). The excess of a
  longer frame is dropped, not carried (carrying makes the next frame longer
  still: death spiral). Dropped time shows up to the sync as a client falling
  behind (it jumps) or a server falling behind (its clients hold).
- `NetClock.Tick` (singleton component) is the simulation clock. It only ever
  grows. The server's clock is the truth; a client's clock runs **ahead** of
  the server so its input for tick `T` is already on the server when the
  server simulates `T`.
- The transport is polled at the start of each tick and sends at its end,
  both inside `NetworkFeature` systems. No network callbacks outside
  `INetTransport.Poll`.
- The transport belongs to whoever created it (Bootstrap; `NetNode` in tests):
  `NetSessionStorage` only starts it, because the Runner never disposes
  systems groups.

## Roles

The registered `INetTransport` (`IsServer`) decides the role; `NetworkFeature`
installs the server or the client systems. Meta (`NetComposition`) picks:

1. command line `-server` / `-client`;
2. editor only: the MPPM multiplayer role of this instance
   (`MultiplayerRolesManager.ActiveMultiplayerRoleMask`, `com.unity.dedicated-server`),
   set per instance by the play mode scenario —
   `Assets/Settings/PlayMode/Server_Client.asset`: main editor = Server,
   Player 2 = Client. `ClientAndServer` (no scenario role) falls through;
3. `NetConfig.Mode`.

`NetConfig.Address:Port` is the server's IPv4 bind address (`0.0.0.0` = every
interface, needed for LAN) and the client's connect target; `-address <ip>`
overrides it. A LAN friend runs a player build with
`-client -address <server LAN IP>`.

## Packets

All unreliable, little-endian, first byte = `PacketType`.

| Packet | Dir | Body |
|---|---|---|
| `Input` | C→S, every client step | `frame u32`, `firstTick i32`, `count u8`, `count × PlayerInput` |
| `Sync`  | S→C, every server tick | `serverTick i32`, `echoFrame u32`, `echoAge u8`, `ackTick i32`, `sampleTick i32`, `slack i8`, `loss u8` (0.5 %), `lastMissedTick i32` |

- `frame` is the client's local step counter (grows on holds too). The server
  echoes the newest one with how many ticks it held it → RTT in ticks without
  clock sync. Gaps in `frame` give the server the client→server loss rate.
- `PlayerInput` is 7 bytes (move x/y `sbyte`, look yaw/pitch delta `short`
  ×100, buttons `byte`).

## Input redundancy

The client keeps every input the server has not acknowledged and sends **all
of them** (newest `MaxInputsPerPacket` = 64 if over) in every `Input` packet.
`ackTick` = highest tick through which the server holds a contiguous run of
inputs. A tick's input therefore rides in every packet from the one it was
produced in until the ack returns.

Client inputs are **contiguous from its first tick**: a jump fills the skipped
ticks with copies of the current input, so the server never has a hole by
design.

## Server

Per client entity: `ServerClient` (256-slot ring keyed by tick, with the server
tick each input **first** arrived on), `ServerClientStats`, `ConsumedInput`.

- **Lead cap:** an input for a tick more than `MaxInputLeadMs` (300 ms = 15
  ticks) ahead of the server tick is **dropped and not acked**. The client
  keeps resending it; it is taken once the server is within 15 ticks. Clients
  can therefore never simulate more than 300 ms ahead of what the server
  accepts.
- On server tick `S` the input for `S` is consumed into `ConsumedInput`:
  present → used; missing → **missed**, last input repeated,
  `lastMissedTick = S`. An input arriving for an already-consumed tick is
  **late** (still stored, so the ack advances and the client stops resending).
- Consumption starts at the client's first input tick.
- Logging: every consumed input with `NetConfig.LogEveryInput`; a missed input
  always (warning); per client every `StatsIntervalTicks`: inputs / missed /
  late / rejected / packets / slack range / loss / ack.

## Client tick sync

**Slack** = how many ticks before its deadline an input reached the server.
The server samples it only from packets that bring a **new newest tick** (that
tick's first copy), so the sample measures lead + jitter, not loss. Loss is
covered separately: with slack `s` an input travels in `s + 1` packets before
its deadline, so the client needs `s` with `loss^(s+1) ≤ TargetMissChance`
(1e-6): `NeedSlack` = 1 on a clean link, 2 at 1 %, 5 at 10 %, capped by
`MaxSlack` and the lead cap.

The client never moves its tick backwards. Each step it does one of:

| Step | Tick | When |
|---|---|---|
| advance | `+1` | normally |
| jump | `+1+k` | estimated slack floor < `NeedSlack` → `k = NeedSlack − floor` (never past the lead cap, unless that is the only way not to miss); or the server reported a miss |
| hold | `+0` | full window's min > `NeedSlack + Hysteresis` (holds the excess), or window max > lead cap |

- **Floor estimate** = min(window min, window mean − ½ × last full window's
  max−min). A young window has not seen the jitter's low end; the last full
  window's spread fills that in.
- Only samples for inputs produced **after** the last adjustment count (a
  barrier tick), so one adjustment is never answered twice.
- Until the server has a full loss window (`LossWindow` = 100 packets) it
  reports loss as unknown; the client then aims for `StartSlack` (6) and
  assumes a jitter spread of `2 × StartSlack`.
- Start: wait for `SyncSamples` (10) `Sync` packets, take the worst RTT, start
  at `serverTick + rtt + StartSlack`. Excess is shed by holds; inputs too far
  ahead are rejected by the lead cap and resent.

## Link conditioner

`LinkConditioner` wraps any `INetTransport` and delays/drops **outgoing**
packets: one-way delay `LatencyMs ± JitterMs` (uniform, so packets reorder)
and `LossPercent` drop, seeded. Both ends wrap theirs, so "ping 300" is
`LatencyMs = 150` on each side. It runs on the transport's own clock (sum of
`Poll` deltas) — no `Time.*`. `NetConfig.SimulateBadNetwork` turns it on for
MPPM sessions (editor / dev builds); edit-mode tests use it over
`LoopbackNetwork`.

## Tested envelope

`NetworkFeatureTests`: a server and two clients as three headless worlds,
180 s each, require **zero missed inputs**, every consumed input equal to what
that client sampled for that tick, client ticks never decreasing, and no input
taken more than 15 ticks ahead:

- ideal → ping 50/±5/1 % → 100/±10/3 % → 200/±25/5 % → **300/±50/10 %**
  (plus 5 more seeds of the last);
- latency ramp 0 → 300/±50/10 % → 0 (lead must shrink back);
- client clocks drifting ±1 % (one fast, one slow), with and without loss;
- too much start lead (rejected, then shed).
