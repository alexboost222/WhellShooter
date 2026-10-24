# 09 — Roadmap and open design

**Not law.** Unsettled design and open questions; once something here is decided it moves to the
doc that owns it.

## Bots

- **Roofs and stairs.** The nav grid is ground level only, so bots never climb to the roofs and
  ground line of sight treats a roof as behind the building's walls. Needs a second grid layer
  (roof cells, stairs as the link) and height-aware sight.
- **Line of sight through physics.** Sight is a 2D trace over the grid — cheap and deterministic,
  but blind to height (shooting over low cover from a roof, crouching behind it). Once map statics
  exist in phys-r, a ray per scan may replace or refine it.
- **Behaviours to add** (each is a `BotAction` + state component + score system + executor):
  take cover when hurt (cells next to high cover with no sight to the enemy), retreat at low
  health, flank (path to a point beside the target rather than at it), push with team-mates,
  investigate sounds (shots heard within a radius), hold a power position.
- **Teams and the player.** In a team game the player's body has no `Team` yet, so every bot treats
  it as free-for-all. Whoever spawns the player (or a match/mode feature) should give it one.
- **Difficulty.** Only the "Regular" profile exists; Easy / Hard rows are a tuning pass.
- **Crowding.** Bots path independently and may stack in doorways; local avoidance (separation
  steering) when it shows up in play.
