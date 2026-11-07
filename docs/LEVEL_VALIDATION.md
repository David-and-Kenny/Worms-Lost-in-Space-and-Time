# Level viability utility

This is a reusable C# Unity tool, not an AI agent. Search, replay, timing analysis
and reports require **no model, API key or AI tokens**. Run the same commands from
PowerShell or a licensed CI runner. Unity still needs its normal editor/license
and installed project dependencies. The tool does not design or publish levels.

## Run it

Use PowerShell 7, close any editor using this checkout, and run from the repository
root. The default editor is the team's pinned Windows installation; override it
with `-UnityEditor /path/to/Unity` when needed.

```powershell
# Structure only; does not establish that a level can be completed.
./scripts/validate-levels.ps1 -Operation inspect -Level all

# Discover a route with the default five-minute budget.
./scripts/validate-levels.ps1 -Operation search -Level level-01

# Search the full catalog, independently budgeting each level.
./scripts/validate-levels.ps1 -Operation batch -Level all

# Full catalog, alternate-profile retries, timing analysis, reports and strict replay.
./scripts/validate-catalog.ps1 -OutputDirectory TestResults/Catalog/current

# Reproduce a saved route without searching.
./scripts/validate-levels.ps1 -Operation replay -ReplayFile '/absolute/path/level-01.replay.json'

# After a source change, treat old inputs as a proposal and verify them afresh.
./scripts/validate-levels.ps1 -Operation search -ReplayFile '/absolute/path/old/level-01.replay.json'

# Replay the explicitly maintained regression certificates in one editor run.
./scripts/validate-levels.ps1 -Operation batch -ReplayDirectory ./ValidationRoutes

# Recertify just those recorded routes after a deliberate source change.
./scripts/validate-levels.ps1 -Operation batch -Level certified -ProposalDirectory ./ValidationRoutes
```

The catalog command uses 30 FPS rendering and the same 50 Hz gameplay physics.
It tries balanced, wide and focused search profiles for unresolved scenes, gives
incomplete timing analysis a separate budget, then strictly replays the selected
certificates. See [profile budgets and coverage](SOLVER_COVERAGE.md). It leaves
maintained repository certificates untouched until a developer reviews the run.

`-Level` accepts a catalog ID, scene name or project-relative `Assets/.../*.unity`
path. Custom scenes need not be published in Build Settings. `-SearchSeconds`,
`-MaximumTrials` and `-TrialSeconds` override the default budgets of 300 wall
seconds, 10,000 trials and 120 simulated seconds. The request additionally caps
routes at 64 jumps and retains 256 frontier states. Inspect request/report JSON
when comparing budgets; a deeper search is not guaranteed to find a route.
`-FramesPerSecond`, `-Seed`, `-MaximumJumps` and `-BeamWidth` expose the remaining
profile options. `-ProposalDirectory` supplies optional recorded-input proposals
for a catalog search; this never bypasses fresh certification. Strict replay
uses the certificate's recorded seed/rate and does not perform a new search.

`-TimingRadiusTicks` chooses the initial sensitivity-test radius (default 12).
Use `-TimingRadiusTicks 2` for an efficient catalog iteration: each jump starts
with ±40ms at 20ms resolution and expands up to ±240ms if a tested success reaches
the boundary. Requests for a larger initial radius retain that larger cap.
The report records the maximum radius actually sampled. Each legal wait tick is
tested once; negative/duplicate earliest-tick commands are not counted as distinct
observations. Budget-limited analysis remains explicitly incomplete.

Output goes into a unique ignored `TestResults/LevelValidation/` directory, or an
explicit `-OutputDirectory`. The command returns **0** for successful inspection
or all verified routes, **2** for non-passing validation outcomes, and **1** for
execution/infrastructure failure. Inspect success means valid structure only.
A process watchdog handles editor/test-runner stalls outside the trial budget.
No summary from an older invocation is accepted as the current result.

## What a certificate proves

1. The scene has one active player, real finish trigger(s), usable typed surfaces
   and required references. Inspection also records active tagged hazards.
2. Ten identical fixed-tick input trials reproduce terminal outcomes and traversal
   checkpoints. Launch/checkpoint position and all velocity tolerances are 0.001;
   completed-event X/Y use 0.01, matching the pinned physics contact offset.
   Angle tolerance is 0.1 degrees and oxygen 0.01; event frames/identities must
   match exactly. See [the observed finish-contact case](SOLVER_COVERAGE.md).
   Without a proposal, initial calibration stops at the first authoritative
   attachment checkpoint, or a terminal result; a stationary attached player does
   not consume an entire trial horizon. A proposed full route is calibrated in full.
   Raw attachment collision poses remain diagnostic data: authoritative
   attachment poses are sampled after following has run for two simulation ticks.
   Immediate launches still verify their actual launch pose; transitions are
   always checked at their original frame and surface identity.
3. Search begins at the real spawn. It supplies observed jump commands through
   `IPlayerTickCommands`; normal gravity, collision, hazards, oxygen, forced detach and
   finish callbacks execute. It cannot teleport or call completion APIs.
4. A candidate reaches the actual finish and reproduces in three fresh fixed-tick
   runs. Completion must write exactly once into disposable storage.
5. The attachment-relative input policy also completes three fresh runs at the
   reference rendering rate. All runs use Unity's ordinary automatic physics.

Legitimate shortcuts and zero-jump/direct exits count. No ordered checkpoint list
is imposed. The known Level 20 missing exit is a structural failure, not a search
timeout. The validator never writes to a developer's actual save directory.

## Clock and search design

The runtime now owns gameplay in one 50 Hz fixed-tick coordinator, followed by
Unity's normal physics step. Player input is buffered from Update; validator
commands are scheduled at that observed-input handoff using simulation ticks.
The old test-only physics clock and moving-spike time override have been removed.
Human play, search, certification and replay share the same runtime tick path.

Traversal events and authoritative attachment checkpoints record simulation tick
numbers and Rigidbody2D poses. Presentation interpolation never supplies launch
angles or certificate positions. Checkpoints are sampled after following, on a
fixed tick, rather than on a render-dependent coroutine continuation.

Rendering profiles still exercise real Unity Update/FixedUpdate scheduling and
fixed-time attack animations. The tool does not claim bit-identical physics across
platforms/editor versions or identical latency for physical taps sampled at
different render rates. Device input, rendering and movement feel need device tests.
See [FIXED_TICK_GAMEPLAY.md](FIXED_TICK_GAMEPLAY.md) for the change and evidence.

Search is bounded best-first exploration of input prefixes. At each reached
attachment it tries passive continuation and wait/jump actions, starting with a
three-tick grid and then testing every remaining tick in the available horizon.

Collider-size-aware angle hints are shared among possible destinations before
the fallback grid. Static obstruction costs help rank paths around obstacles;
moving hazards remain real simulation, not static blockers. The larger beam
retains different landing phases, radii and clock phases so a crowded platform
does not monopolize the search. Those cells guide allocation, not state equality.
Platform-wide effort also increases a frontier's scheduling cost: another arrival
on an early wheel cannot reset its effort and indefinitely starve later wheels.
Exit distance remains the main progress hint; obstruction estimates have a small
weight because a center-to-center line can intersect a hazard that an edge-to-edge
jump safely avoids.

Search-only transition horizons stop spending the full 120 seconds on every miss;
extended probes and a full-horizon passive action remain available. An outward
zero-gravity flight beyond the scene's interaction envelope ends that search
probe. None of these cutoffs proves a level impossible. Full certification/replay
uses the full requested horizon and never uses these search cutoffs.
Jump waits also respect the active breakable wheel's observed remaining timer;
passive forced-detach continuations stay available. An unknown lifetime is -1,
while zero remains a real imminent deadline rather than an unlimited wait.
`Coverage` records attempted transitions, reached surfaces, frontier count,
deepest observed prefix and timeout count. Unsuccessful reports retain a diagnostic
trajectory, not a fabricated completion certificate.
Collision attachment can first launch on the following simulation tick. Actual
geometry only prioritizes frontiers by distance to an exit; it cannot certify or
disprove a jump.

Every candidate reloads the entire scene and replays its prefix. There are no
partial physics snapshots that forget projectiles, timers or animation state.
Different contact positions/times on a platform are retained as distinct routes.
Beam limits, route depth and the finite input grid make this an incomplete search.
For rotating surfaces, launch-direction estimates rank coarse candidates toward
other surfaces/exits and interleave nearby one-tick refinements. Every tick
remains available to the search; estimates never exclude a possible route.
Candidates that fail normal-physics verification are rejected and exploration
continues within the remaining budget. A strict saved-route replay fails directly
rather than silently replacing the requested route with a newly discovered one.

After certification, the remaining budget samples single-jump timing offsets
within ±250ms on the 20 ms simulation grid (±240ms sampled) and executes the
remaining route using attachment-relative waits.
Disconnected success windows are reported separately. Offsets outside that range
are unknown, including when a success reaches the sampled boundary. The sampled
success ratio concerns those variations, not human retention or combined random
input errors. Separate 30/60/120 Hz checks use normal automatic physics. An
incomplete analysis does not erase a separately certified reachable route.

## Results and artifacts

| Status | Meaning |
| --- | --- |
| `VerifiedReachable` | A real finish route passed absolute-tick and relative-policy verification |
| `InvalidContent` | Required authored content/references are absent or invalid |
| `Inconclusive` | Search/frontier/time budget ended without a certified route |
| `ReplayFailed` | A saved/candidate route failed; an alternative may still exist |
| `RunnerUnstable` | Identical fixed-tick input trials diverged; no viability verdict issued |
| `Stale` | Certificate is missing, malformed or belongs to different sources/editor |
| `ExecutionError` | The runner or game produced an execution error |

`summary.md` is the readable overview; `summary.json` contains the versioned batch
result. Each level has a `.report.json`, and a successfully certified route also
has a `.replay.json`. Logs and the command's NUnit XML establish whether execution
itself completed. In reused output folders, use the current summary and `HasReplay`
flag; unrelated older files are not evidence for that run.

Requests, manifests and reports remain version 1; new replay certificates are
**version 2**. Their `Frame` fields are 50 Hz simulation tick numbers, independent
of `FramesPerSecond` (the rendering profile). Commands retain attachment-relative
delays, stable surface IDs and attachment ordinals. Version 1 inputs remain readable
as search proposals, using their relative delays, but strict replay rejects them. Reports contain events, sampled trajectories, budgets,
normal-physics profiles, timing windows and explicit `Status` names (Unity also
serializes numeric enum values). `HasReplay` is authoritative because Unity's
serializer can represent a null nested object with default fields.
`Complete` and `RequestedLevels` distinguish a finished batch from a partial
progress report. Oxygen `-1` means that no oxygen component was available.

The source fingerprint includes the level's imported dependencies, gameplay and
validator source, relevant physics/time/build/tag settings, packages and editor
version. Text line endings are normalized. A changed fingerprint invalidates the
certificate: search current content to issue a new one. Never hand-edit a hash to
make a stale route pass. Certificates in `ValidationRoutes/` are reviewed source
artifacts; logs, search traces and bulk outputs remain ignored.
An explicitly supplied search proposal can retain its old hash: the utility
re-executes it, requires new verification and writes the current fingerprint.
If those inputs fail, automatic search can continue within the remaining budget.
Copy only the newly verified `.replay.json` files into `ValidationRoutes/`, review
the change and run strict batch replay before committing. See
[LEVEL_VALIDATION_RESULTS.md](LEVEL_VALIDATION_RESULTS.md) for the initial catalog
matrix, suite evidence and outstanding content/device checks.

### Completion times and timing demand

Generate a shareable report from completed runs without Unity or AI tokens:

```powershell
node scripts/report-level-validation.mjs --summary TestResults/LevelValidation/run/summary.json --output TestResults/level-report.md --csv TestResults/level-report.csv

# Publish compact report data without local trajectories or duplicate checkpoints.
node scripts/report-level-validation.mjs --summary TestResults/Catalog/current/summary.json --output TestResults/preview.md --csv TestResults/preview.csv --evidence docs/LEVEL_VALIDATION_EVIDENCE.json
# Regenerate the shared table from the tracked snapshot, without Unity or a model.
node scripts/report-level-validation.mjs --summary docs/LEVEL_VALIDATION_EVIDENCE.json --output docs/FIXED_TICK_LEVEL_REPORT.md --csv docs/FIXED_TICK_LEVEL_REPORT.csv
```

Repeat `--summary` to combine the catalog scan with deeper per-level runs from the
same source fingerprints. A verified route survives a later inconclusive retry.
Partial runs and conflicting fingerprints are rejected. The report records replay
completion time, jump count, sampled timing tolerance and Low, Moderate or High
timing demand. Attempts, sampled failure ratios and total jump count do not set
the band; tight-jump counts and chains are separate. Rules are printed in the report.
Incomplete timing analysis stays Not rated; these bands are not human playtest
ratings or proof that an easier route does not exist.
The tracked snapshot is report data, not a replay certificate; complete expected
checkpoints remain in `ValidationRoutes/*.replay.json`. The current iteration
certifies Levels 1–19; Level 20 has no authored finish. See
[the full validation evidence](SOLVER_COVERAGE.md).

## Visual replay and development

In Unity, choose **Worms → Level Validation → Replay saved route** and select a
certificate. The test runner plays the real scene with visible input and
Scene-view trajectory/jump gizmos. It performs verification rather than launching
a search. Use **View trajectory report** to inspect a saved `.report.json` with
launch frame/angle labels, and **Clear trajectory** to remove that overlay.
Enable Scene-view Gizmos to see live breadcrumbs. Close/save dirty scenes before
inspection; the tool does not silently save or discard another developer's work.

Tool code lives in the `Worms.LevelValidation` test assembly and is excluded from
ordinary player builds. Ordinary C# models/search/report logic are separated from
Unity scene inspection and playback adapters. Existing script GUIDs and serialized
animation/event entry points remain unchanged. The runtime coordinator, buffered
input and Rigidbody attachment ownership are described in
[ARCHITECTURE.md](ARCHITECTURE.md). The original investigation is preserved in
[FRAME_RATE_INVESTIGATION.md](FRAME_RATE_INVESTIGATION.md).

Fixtures cover a known route, sealed exit, moving hazard, breakable wheel, narrow
exit and direct finish. The explicit `FixtureAuthoring.Create` command creates
missing fixtures and refreshes their manifests; it does not overwrite existing
fixture scenes. Do not run it as a production-level migration. Rule tests exercise
budgets, stale/malformed certificates, search enumeration and repeatability; actual
PlayMode tests exercise discovery, real completion/death and automatic-physics
verification. See `TESTING.md` for the standard test commands.
