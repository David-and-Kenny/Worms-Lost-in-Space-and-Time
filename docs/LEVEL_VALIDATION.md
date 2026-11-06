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

# Reproduce a saved route without searching.
./scripts/validate-levels.ps1 -Operation replay -ReplayFile '/absolute/path/level-01.replay.json'

# After a source change, treat old inputs as a proposal and verify them afresh.
./scripts/validate-levels.ps1 -Operation search -ReplayFile '/absolute/path/old/level-01.replay.json'

# Replay the explicitly maintained regression certificates in one editor run.
./scripts/validate-levels.ps1 -Operation batch -ReplayDirectory ./ValidationRoutes

# Recertify just those recorded routes after a deliberate source change.
./scripts/validate-levels.ps1 -Operation batch -Level certified -ProposalDirectory ./ValidationRoutes
```

`-Level` accepts a catalog ID, scene name or project-relative `Assets/.../*.unity`
path. Custom scenes need not be published in Build Settings. `-SearchSeconds`,
`-MaximumTrials` and `-TrialSeconds` override the default budgets of 300 wall
seconds, 10,000 trials and 120 simulated seconds. The request additionally caps
routes at 64 jumps and retains 64 frontier states. Inspect request/report JSON
when comparing budgets; a deeper search is not guaranteed to find a route.
`-FramesPerSecond`, `-Seed`, `-MaximumJumps` and `-BeamWidth` expose the remaining
profile options. `-ProposalDirectory` supplies optional recorded-input proposals
for a catalog search; this never bypasses fresh certification. Strict replay
uses the certificate's recorded seed/rate and does not perform a new search.

Output goes into a unique ignored `TestResults/LevelValidation/` directory, or an
explicit `-OutputDirectory`. The command returns **0** for successful inspection
or all verified routes, **2** for non-passing validation outcomes, and **1** for
execution/infrastructure failure. Inspect success means valid structure only.
A process watchdog handles editor/test-runner stalls outside the trial budget.
No summary from an older invocation is accepted as the current result.

## What a certificate proves

1. The scene has one active player, real finish trigger(s), usable typed surfaces
   and required references. Inspection also records active tagged hazards.
2. Ten identical controlled trials reproduce terminal outcomes and traversal
   checkpoints. Position/velocity tolerance is 0.001 units, angle 0.1 degrees and
   oxygen 0.01 units; event frames/identities must match exactly.
   Raw attachment collision poses remain diagnostic data: authoritative
   attachment poses are sampled after following has run for two updates.
   Immediate launches still verify their actual launch pose; transitions are
   always checked at their original frame and surface identity.
3. Search begins at the real spawn. It injects one-frame jump presses through
   `IPlayerInput`; normal gravity, collision, hazards, oxygen, forced detach and
   finish callbacks execute. It cannot teleport or call completion APIs.
4. A candidate reaches the actual finish and reproduces in three fresh controlled
   runs. Completion must write exactly once into disposable storage.
5. The attachment-relative input policy also completes three fresh runs with
   Unity's ordinary automatic physics at the reference rate.

Legitimate shortcuts and zero-jump/direct exits count. No ordered checkpoint list
is imposed. The known Level 20 missing exit is a structural failure, not a search
timeout. The validator never writes to a developer's actual save directory.

## Clock and search design

The existing player/wheel/hazard code runs in `Update`. Search retains those
behaviours and uses captured game-time frames plus a **test-only** integer phase
clock: 50 physics steps per simulated second, before gameplay `Update`, at the
requested render rate. This removes the automatic physics accumulator inherited
between scene reloads. The absolute-time moving-spike adapter accepts an optional
elapsed-time source for this controlled mode; human play keeps its existing clock.
Animation, coroutines and the rest of the real Unity player loop continue running.

Normal-physics verification disables both overrides to check the route against
the original scheduler. The tool does not claim bit-identical physics across
platforms or editor versions. Timing certificates describe their recorded profile;
device input, rendering, performance and movement feel still need device tests.

Search is bounded best-first exploration of input prefixes. At each reached
attachment it tries passive continuation and wait/jump actions, starting with a
three-frame grid and then testing every remaining frame in the available horizon.
Immediate attachment-frame jumps are included. Actual geometry only prioritizes
frontiers by distance to an exit; it cannot certify or disprove a jump.

Every candidate reloads the entire scene and replays its prefix. There are no
partial physics snapshots that forget projectiles, timers or animation state.
Different contact positions/times on a platform are retained as distinct routes.
Beam limits, route depth and the finite input grid make this an incomplete search.
For rotating surfaces, launch-direction estimates rank coarse candidates toward
other surfaces/exits and interleave nearby one-frame refinements. Every frame
remains available to the search; estimates never exclude a possible route.
Candidates that fail normal-physics verification are rejected and exploration
continues within the remaining budget. A strict saved-route replay fails directly
rather than silently replacing the requested route with a newly discovered one.

After certification, the remaining budget samples single-jump timing offsets
within ±250ms and executes the remaining route using attachment-relative waits.
Disconnected success windows are reported separately. Offsets outside that range
are unknown, including when a success reaches the sampled boundary. The sampled
success ratio concerns those variations, not human retention or combined random
input errors. Separate 30/60/120 Hz checks use normal automatic physics. An
incomplete analysis does not erase a separately certified reachable route.

## Results and artifacts

| Status | Meaning |
| --- | --- |
| `VerifiedReachable` | A real finish route passed controlled and normal-physics verification |
| `InvalidContent` | Required authored content/references are absent or invalid |
| `Inconclusive` | Search/frontier/time budget ended without a certified route |
| `ReplayFailed` | A saved/candidate route failed; an alternative may still exist |
| `RunnerUnstable` | Identical controlled trials diverged; no viability verdict issued |
| `Stale` | Certificate is missing, malformed or belongs to different sources/editor |
| `ExecutionError` | The runner or game produced an execution error |

`summary.md` is the readable overview; `summary.json` contains the versioned batch
result. Each level has a `.report.json`, and a successfully certified route also
has a `.replay.json`. Logs and the command's NUnit XML establish whether execution
itself completed. In reused output folders, use the current summary and `HasReplay`
flag; unrelated older files are not evidence for that run.

Requests, manifests, reports and replays are version 1. Replay commands contain
absolute reference frames and attachment-relative delays, stable surface IDs and
attachment ordinals. Reports contain events, sampled trajectories, budgets,
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
Unity scene inspection, input and simulation adapters. Runtime changes are limited
to read-only traversal events and an optional moving-hazard clock boundary.
Existing script GUIDs and serialized event entry points remain unchanged.
Wheel rotation now explicitly precedes default-priority attachment-following
(`DefaultExecutionOrder(-100)`). Existing levels exposed a one-frame phase
difference across reloads when those Updates had unspecified ordering. Forces,
speeds and the Update/physics timestep split are retained; a PlayMode regression
checks that the player and holder remain phase-locked.

The remaining render/physics split is a separate gameplay concern; see
[FRAME_RATE_INVESTIGATION.md](FRAME_RATE_INVESTIGATION.md). A route certified at
60 Hz is not evidence that a shipping Android build, which currently has no
explicit rendering-rate policy, follows that route at its default frame rate.

Fixtures cover a known route, sealed exit, moving hazard, breakable wheel, narrow
exit and direct finish. The explicit `FixtureAuthoring.Create` command creates
missing fixtures and refreshes their manifests; it does not overwrite existing
fixture scenes. Do not run it as a production-level migration. Rule tests exercise
budgets, stale/malformed certificates, search enumeration and repeatability; actual
PlayMode tests exercise discovery, real completion/death and automatic-physics
verification. See `TESTING.md` for the standard test commands.
