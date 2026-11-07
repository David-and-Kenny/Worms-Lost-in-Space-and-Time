# Level design and acceptance checklist

Use one copy of the template below in a level's design note or PR. Keep draft
scenes outside the published catalog until they have a certified route. A solver
timeout is inconclusive, never proof of impossibility. Follow the
[game brief](GAME_BRIEF.md); preserve existing movement unless separately scoped.

## Per-level note

| Field | Author fills in |
| --- | --- |
| Catalog ID / scene | Stable ID and `Assets/.../*.unity` path |
| Purpose | Mechanic taught, practiced or combined; intended player knowledge |
| Intended challenge | Timing decisions, route reading, moving hazards, commitment |
| Entry / exit | Safe initial state, visible finish, allowed shortcuts |
| Retry cost | Expected successful-route duration and time to meaningful feedback |
| Resource/lifetime rules | Oxygen if present, wheel timers, forced detach, moving periods |
| Evidence | Fingerprint, replay, rendering profiles, analysis and report paths |
| Human/device review | Tester, device/build, observations and unresolved issues |

## Authoring checks

- [ ] One active player; valid typed wheel/conveyor, camera and hazard references.
- [ ] Active real finish trigger, with readable visual identification and enough
  space for the player to approach. Final-level navigation must not request Level 21.
- [ ] Preserve GUIDs and existing collision intent. Keep replaced art as `.legacy`.
- [ ] Spawn and camera framing reveal useful information without an accidental death.
- [ ] Hazards and safe surfaces are distinguishable. Explain moving/timed hazards
  through visible behavior rather than hidden exceptions.
- [ ] Judge legitimate shortcuts by the intended mechanic; validation does not
  impose an unrecorded ordered checkpoint list.
- [ ] Successful routes stay short enough to encourage learning through retries.
  Current measured times are a reference, not a fixed design band or optimal times.
- [ ] An avoidable failure communicates its cause. Review unreadable obstacles,
  excessive dead time and expensive recovery separately from execution precision.
- [ ] Keep timed-star targets disabled unless deliberately authored and playtested.

## Automated evidence — no model required

Run from the repository root with the pinned Unity editor available:

```powershell
./scripts/validate-levels.ps1 -Operation inspect -Level level-20
./scripts/validate-levels.ps1 -Operation search -Level level-20 -FramesPerSecond 30 -SearchSeconds 600 -TimingRadiusTicks 2
# For the complete shared catalog and complementary search profiles:
./scripts/validate-catalog.ps1 -OutputDirectory TestResults/Catalog/current
```

- [ ] Structural inspection passes. Search or a recorded-input proposal reaches
  the real finish with one save write in disposable storage.
- [ ] Fresh certification and strict replay pass. Source changes require fresh
  issuance; never modify a fingerprint to make an old certificate pass.
- [ ] Timing analysis and 30/60/120 FPS policies are complete. Record budget limits
  and any unverified hold policy rather than guessing its result.
- [ ] Review exact jump windows alongside verified press groups, prepared presses
  and launches from continued holding. Search attempts and failure ratios do not
  set difficulty. Human learnability and combined input errors remain separate.
- [ ] Copy only freshly verified certificates to `ValidationRoutes`, regenerate
  tracked report data, and run `node scripts/check-validation-artifacts.mjs`.
  This CI check verifies artifact agreement; Unity establishes source freshness
  and physical viability. It is not a replacement for playing on a phone.

## Human and device acceptance

- [ ] Test actual press/hold/release behavior, touch exclusion, camera transitions,
  portrait/landscape readability, pause/resume and final result navigation using
  [PLAYTEST_SCRIPT.md](PLAYTEST_SCRIPT.md).
- [ ] Observe how quickly a player understands and improves at the level. Record
  execution mistakes separately from unclear routes, surprise rules and input issues.
- [ ] Do not label a level harder just because it took more attempts. Do not treat
  a single successful sampled tick as a literally zero-width human input window.
- [ ] Record unresolved findings in the backlog. Missing device or human testing
  stays unverified, even when every automated check passes.
