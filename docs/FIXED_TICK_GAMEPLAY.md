# Fixed-tick gameplay change

Baseline: `d5520a8`, Unity 6000.6.8f1. All 39 unchanged PlayMode tests passed in
the isolated checkout before production edits. XML/logs are retained as
`TestResults/Before-FixedTick-PlayMode.xml` and `Logs/Before-FixedTick-PlayMode.log`.
The earlier frame-rate investigation supplies the before-profile evidence.

## Implementation and acceptance

1. Give one scene-owned coordinator a clear fixed-tick order: clock, surfaces,
   moving hazards, player, breakable countdowns, oxygen and projectile emitters.
   Leave Unity in charge of normal physics. Keep the authored 0.02-second step.
2. Sample human input in Update, retain a brief observed press through the next
   physics tick, and preserve held-jump and pointer guards. Expose a tick-command
   boundary for reproducible tests; never pretend frame-polled input knows the
   timestamp of an event it has not observed.
3. Own attached-wheel pose with a kinematic Rigidbody2D and full kinematic contacts;
   restore the original body mode on launch. Preserve start/jump/forced-detach
   speeds, wheel angular speeds, conveyor speed and same-wheel suppression.
4. Interpolate the player presentation and follow it in LateUpdate. Keep collision
   and launch decisions on the physics pose, independent of that interpolation.
5. Move gameplay timers and attack animation evaluation onto fixed time. Preserve
   scene fields, script GUIDs and animation-event entry points.
6. Update the validator to schedule/observe simulation ticks and use the same
   runtime tick path for controlled and automatic physics. Old certificates are
   stale; old inputs may only propose freshly verified routes.
7. Test pulse/held input and pause clearing, attachment/contact/forced detach,
   rotation and launch velocities, flight and timers across 30/60/120 FPS plus
   uneven render schedules. Run the existing suites and builds. Revalidate all
   published levels and retain honest inconclusive/content-failure results.

No level geometry, force tuning, new input package or mobile rendering cap is
part of this change. Device movement feel and suspend/resume still require a real
device acceptance run. Record measured outcomes here after verification.

## Design boundaries

The coordinator is a small typed list of existing components, with an explicit
order in one method. It is not a generic simulation framework. Components keep
their authored references and expose short `Advance` methods. Unity still owns
collision solving and its usual fixed loop. The old validator-only physics clock
is removed, so tests and human play execute the same movement implementation.

Initial passive calibration ends at the first attachment plus two fixed ticks,
including an authoritative checkpoint. This avoids waiting indefinitely on a
stationary player without weakening full-route certification: candidates still
require fresh complete repetitions and attachment-relative replay. Explicit old
route proposals are calibrated in full and never trusted by their old hashes.

Observed input can wait up to the next 20 ms tick. Player interpolation also
introduces its usual one-tick presentation lag. Physical events still arrive
through frame-polled legacy input, so lower rendering rates can add input latency.
Tests establish equal simulation outcomes for equal observed tick commands; they
do not establish identical physical-tap latency on every phone. Numerical movement
parameters stay fixed, but launch/contact timing is intentionally quantized to
20 ms and existing routes must be recertified. A newly found faster route is not
evidence that the game's movement speed increased.

Unity references read for the legacy Rigidbody2D implementation:
[fixed updates](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/monobehaviour/fixedupdate),
[full kinematic contacts](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/rigidbody2d/usefullkinematiccontacts),
[MovePosition ownership](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/rigidbody2d/moveposition).

## Executed regression checks

The unchanged 39-test PlayMode baseline passed before editing production code.
Before merging parallel AdMob work, the suites passed **92 EditMode tests and 43 PlayMode tests**, with no failed
tests. The seven validation-runner harness cases and scene validation also pass.
The full Windows development build and Android IL2CPP/ARM64 development build
succeeded with the final runtime implementation. Subsequent changes only affect
test calibration/coverage and documentation, so shipping code is unchanged from
those builds.

| Check | Measured result |
| --- | --- |
| Wheel/launch/flight | Same tick-25 launch, 10-unit velocity and later body position at 30/60/120 FPS and uneven rendering |
| Input handoff | Observed press/release survives to the next tick; one launch; held input, airborne expiry, pause clearing and pointer guards preserved |
| Attachment | Kinematic full contacts retain actual static-hazard death and real finish-prefab completion/save; flight mode restored on launch |
| Countdown/oxygen | Same forced-detach tick, 5-unit launch speed and remaining oxygen across 30/60/120 FPS |
| Actual finish route | Fixed-tick traversal events/poses match at 30/60/120 FPS; one completion write |
| Authored attack animation | Real AlienC animation events emit projectiles on equal ticks at 30/60/120 FPS, retaining 10-unit projectile speed |
| Existing behavior | Initial 3-unit fall, same-wheel suppression, conveyors, camera, death, persistence, navigation and UI tests pass |

The final authored-animation test and full 43-test suite ran in an isolated
verification checkout at `47a5700`; runtime/validator source matches the main
implementation at `81b8bf8` plus the documentation-only merge `8d0a19b`.
Raw evidence is retained locally:

- `TestResults/EditMode.xml` — final 92-test unit/editor suite.
- `TestResults/FixedTick-PlayMode-Final.xml` and
  `Logs/FixedTick-PlayMode-Final.log` — final 43-test suite.
- `TestResults/FixedTick-Full-Build-Validation.json` — the full scene/test/build
  invocation before the final test-only additions.
- `Logs/BuildWindows.log`, `Logs/BuildAndroid.log` and the project's `Builds/`
  folder — successful builds and local development outputs.

No physical-device or subjective movement-feel pass is claimed. Follow the
fixed-tick acceptance section in [PLAYTEST_SCRIPT.md](PLAYTEST_SCRIPT.md) on a
device before treating the change as release-ready.

## Existing-level iteration

The final catalog batch completed **20/20 requests**, with a 60-second wall budget
per level, seed 1729, 60 FPS rendering, a 120-second simulated trial horizon,
10,000-trial cap, 64-jump cap and beam width 64. That scan certified levels 1, 4
and 10; sixteen searches were inconclusive and Level 20 was invalid content.
The deeper five-minute Level 3 run certified a new route; Level 8's five-minute
run and six uniform timing-offset proposal checks remained inconclusive. Level
4's separately completed timing analysis supplies its full difficulty evidence.

The final combined result is **four certified routes, fifteen unresolved levels
and one content blocker**. All four routes completed at 30/60/120 rendered FPS.
Their version 2 certificates passed strict replay in both the implementation and
verification checkouts: ten fixed-tick repetitions plus three attachment-relative
repetitions per route per checkout. The current maintained set is 1, 3, 4 and 10.

**Level 20 cannot complete as authored because it has no active finish trigger.**
No other level has been proved impossible. Level 8 was certified before this
timing change but has not been recertified; treat it as an unresolved regression
risk requiring a manual playtest or further search. Its obsolete certificate is
not retained as a passing regression. No level geometry or content was changed.

The original completed scan is preserved as
[the iteration snapshot](FIXED_TICK_CATALOG_ITERATION.md) and its
[CSV](FIXED_TICK_CATALOG_ITERATION.csv). The current
[completion/difficulty report](FIXED_TICK_LEVEL_REPORT.md) and
[CSV](FIXED_TICK_LEVEL_REPORT.csv) use the integrated-source evidence described
below. The ratings describe the found routes,
using explicit sampled-timing and jump-count rules. All four currently rate High;
that does not establish that every possible route through each level is hard.

Raw completed evidence is under `TestResults/LevelValidation/`:

- `FixedTick-Catalog-Final/` — complete 20-level one-minute iteration.
- `FixedTick-Deep03/`, `FixedTick-Deep08/` — five-minute per-level budgets.
- `FixedTick08-Offset--1/` through the six signed 1/2/3-tick proposal folders —
  unsuccessful bounded input-policy checks, not content changes.
- `FixedTick-Analysis04/` — fresh certification and completed timing analysis.
- `FixedTick-Strict-Replay/`, `FixedTick-CrossCheckout-Replay/` — four-route passes.

An earlier catalog attempt was intentionally stopped to correct passive
calibration budgeting. It is not counted as a completed iteration. Reports from
different source fingerprints cannot be combined by the report tool.

Regenerate the report without Unity or AI tokens:

```powershell
node scripts/report-level-validation.mjs --summary TestResults/LevelValidation/FixedTick-Catalog-Final/summary.json --summary TestResults/LevelValidation/FixedTick-Deep03/summary.json --summary TestResults/LevelValidation/FixedTick-Deep08/summary.json --summary TestResults/LevelValidation/FixedTick-Analysis04/summary.json --output docs/FIXED_TICK_CATALOG_ITERATION.md --csv docs/FIXED_TICK_CATALOG_ITERATION.csv
```

The six report-tool tests pass, including its actual Markdown/CSV command. They
run in repository CI alongside existing hygiene and runner checks.

## Integration with the parallel AdMob change

Master advanced to `f0fdebd` while validation was running. The integration at
`5aef1a3` retains both sets of functionality/documentation and removes the obsolete
Level 8 certificate. Gameplay movement code is unchanged by that integration.
The merged source passed **122 EditMode tests, 44 PlayMode tests, scene validation,
and fresh Windows/Android development builds**. Standard `TestResults/EditMode.xml`,
`PlayMode.xml`, `validation-summary.json` and the build logs now refer to this run.
The six report-tool tests and all eight merged runner-harness cases also pass.

All four routes were freshly certified with completed timing analysis against the
merged source. Their recorded times remain 7.96s, 9.32s, 9.86s and 2.60s, with
successful 30/60/120 FPS profiles. `Integrated-Structure/` inspects all twenty
current scenes, and `Integrated-Certification/` records the four full certifications.
The current report combines those matching fingerprints. Its other fifteen rows
explicitly say structure-only: their longer traversal search results remain in
the earlier iteration snapshot, rather than being relabelled as new-source runs.
Level 20's missing finish is confirmed again on the integrated source.

`Integrated-Strict-Replay/` records strict replay of the refreshed certificates in
the implementation checkout; certification ran in the separate verification
checkout. Physical-device acceptance remains open.

```powershell
node scripts/report-level-validation.mjs --summary TestResults/LevelValidation/Integrated-Structure/summary.json --summary TestResults/LevelValidation/Integrated-Certification/summary.json --output docs/FIXED_TICK_LEVEL_REPORT.md --csv docs/FIXED_TICK_LEVEL_REPORT.csv
```
