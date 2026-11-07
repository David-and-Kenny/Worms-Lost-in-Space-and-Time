# Solver coverage improvement

Baseline: `dea89b3` (merged fixed-tick gameplay); pinned editor 6000.6.8f1.
The unchanged nine-test validation integration suite passed in the isolated
checkout before solver edits. Evidence: `TestResults/Solver-Baseline.xml` and
`Logs/Solver-Baseline.log`. This change is test tooling; gameplay/level geometry
and the runtime 50 Hz simulation remain unchanged.

## Scope and acceptance

- Replace expensive unbounded-first transition exploration with short probes,
  outward-flight envelope checks and explicit longer fallbacks. Probe timeouts
  remain inconclusive; they do not establish physical impossibility.
- Record actual collider envelopes. Use geometry and static obstruction costs to
  order trials, share early angles among destinations, and retain alternative
  platforms in the frontier beam. Every legal waiting tick remains available.
- Reload and replay complete prefixes through real gameplay. Never restore a
  partial physics snapshot, teleport the player or invoke completion APIs.
- Retain ten-trial calibration, fresh full-route certification, disposable
  progress storage, attachment-relative verification and source fingerprints.
- Add search coverage diagnostics and regression fixtures for the search's
  missing paths, then rerun all twenty published levels against one source.
- Publish actual successful-run times and measured route execution demands.
  Attempts/search effort describe solver coverage, not human difficulty. The
  intended game has short, demanding, learnable levels with inexpensive retries;
  overall difficulty still needs calibrated human playtests.

Search and reporting run locally without a model, API key or AI tokens. The
existing `scripts/validate-levels.ps1` and report CLI remain the developer entry
points. Measured results and remaining limitations are recorded below.

## Completed small follow-ups — November 6, 2026

All five scoped follow-ups are implemented in PR #12:

1. A no-Unity CI integrity guard checks catalog/certificate/report agreement, with
   six positive/mutation tests. It does not replace source-fresh Unity replay.
2. Observed-button hold policies pass through `JumpInputBuffer`, with verified
   press groups and prepared/continued-hold counts separate from exact precision.
   Four rule tests and a two-attachment physical fixture cover this behavior.
3. [GAME_BRIEF.md](GAME_BRIEF.md) records the agreed game direction and open choices.
4. [LEVEL_DESIGN_CHECKLIST.md](LEVEL_DESIGN_CHECKLIST.md) supplies a reusable design
   note and automated/human acceptance gates.
5. Level 20 has a visible exit after its existing platforms. The solver's natural
   input route completes in **3.94 seconds**; the final catalog policy has no Level 21.

The frozen Unity source for this follow-up is `4e4d989`. EditMode **141/141**,
PlayMode **47/47**, and all 24 scene/prefab binding checks passed. The report and
integrity suites contain **16 passing Node tests**. All twenty levels have fresh
certificates and complete timing/held-input analysis at 30/60/120 FPS; strict
replay passed in the primary and independent checkouts. This supersedes the
nineteen-route baseline below. No movement or earlier level geometry changed.

Known routes in this iteration use a 30-second trial horizon, sufficient for all
recorded completion times. Missed timing variations are observations within that
bounded profile, not proofs about every possible longer path. Normal catalog
search profiles retain their documented horizons and budgets. Logs/traces remain
under ignored `TestResults/Catalog/Complete-20-Held` and the follow-up replay runs;
the tracked report snapshot and maintained replay files contain shared evidence.
Phone input/readability/lifecycle and calibrated human difficulty remain open.

## Earlier solver iteration — November 6, 2026

The final Unity source is `77fb024`, integrated with master `b40eacb`, using the
pinned Windows editor. All twenty catalog levels were evaluated: **Levels 1–19
are VerifiedReachable**, with complete timing analysis and successful 30/60/120
FPS policy runs. **Level 20 is InvalidContent: no active authored finish trigger.**
There are no remaining untested or inconclusive catalog levels in this iteration.

Main-checkout strict replay passed **19/19**; an independent checkout also passed
**19/19** against the same source fingerprints. Each strict route ran ten absolute
replays and three attachment-relative replays. Tests passed: **136 EditMode,
46 PlayMode**, 8 report tests, 12 catalog-runner scenarios and 8 existing runner
scenarios. The scene/prefab binding check passed all 24 enabled scenes.

See the [full completion/timing-demand table](FIXED_TICK_LEVEL_REPORT.md), its
[CSV](FIXED_TICK_LEVEL_REPORT.csv), and [compact evidence](LEVEL_VALIDATION_EVIDENCE.json).
The maintained `ValidationRoutes/` contains nineteen freshly generated version 2
certificates. Times range from **2.60 to 23.00 seconds** for these discovered
routes; they are not optimized best times. Human difficulty remains uncalibrated.

Selected discovery runs explain the longer focused budget: Level 9 required
445.81 wall seconds (1,697 total trials), and Level 19 required 588.43 seconds
(2,573 trials). Those in-flight discovery runs predate the final observed-lifetime
change; their recorded inputs were recertified against the final source. These
are search-effort observations, not human attempt counts or controlled benchmarks.

Raw runs are under ignored `TestResults/Catalog/Integrated-UI`,
`TestResults/Catalog/Integrated-Selected/Replay` and
`TestResults/LevelValidation/Integrated-Independent-Strict`. Full traces/logs stay local;
the reviewed snapshot and replay inputs preserve the shared evidence. No shipping
gameplay or level geometry changed. Physical input/feel, rendering and lifecycle
acceptance on phones remain in `PLAYTEST_SCRIPT.md`.
The concurrently merged menu-art change was integrated before this final pass;
all nineteen certificates were reissued rather than carrying over old hashes.

## Reusable portfolio

`scripts/validate-catalog.ps1` runs the full catalog with the balanced profile
(256-state beam, 120-second route horizon), then retries only inconclusive scenes
with a 30-second horizon and a 1,024-state beam. If that also misses, a focused
profile uses a 16-state beam and 60-second horizon to spend more launch angles on
each retained arrival. Balanced and wide profiles have five-minute wall budgets;
the focused profile gets ten minutes and a 50,000-trial cap, informed by Level 9's
successful longer search. The other profiles retain the 10,000-trial cap.
`-SearchSeconds`, `-FallbackSeconds` and `-FocusedSeconds` configure those budgets.
Rendering defaults to 30 FPS to reduce search work; `-FramesPerSecond` changes the
initial/analysis profile. The runtime physics rate remains 50 Hz, and sensitivity
analysis separately checks 30/60/120 FPS. The wide profile spends more search on
short routes and landing diversity. Invalid content,
execution failures and unstable calibration are not relabelled as search misses.

Verified inputs are freshly replayed and copied into the run's `Certificates/`
folder, not published over the repository's maintained routes. Incomplete timing
analysis gets a separate budget. The pipeline emits JSON, Markdown and CSV, and
strictly replays the selected certificate set before returning. An old initial
run can be resumed only after fresh inspection confirms all source fingerprints.
Output folders containing an earlier certificate set are rejected.
`-AdditionalRuns` accepts completed per-level or subset runs from independent
checkouts. Each must match the initial catalog's fingerprints. Verified
supplemental routes skip redundant searches, retain complete timing evidence and
join the same final strict replay. Changed sources and partial runs are rejected.

```powershell
./scripts/validate-catalog.ps1 -OutputDirectory TestResults/Catalog/current
# Resume a complete same-source first pass without paying its search cost again.
./scripts/validate-catalog.ps1 -InitialRun TestResults/LevelValidation/Coverage-Catalog -OutputDirectory TestResults/Catalog/continued
# Combine completed matching-source worker runs before retrying any remaining misses.
./scripts/validate-catalog.ps1 -InitialRun TestResults/Catalog/current/Initial -AdditionalRuns TestResults/Manual-level09,TestResults/Manual-level16 -OutputDirectory TestResults/Catalog/combined
```

The deterministic harness `scripts/test-validate-catalog.ps1` runs in CI without
Unity or a model. The CLI uses exit 2 for unresolved/content-blocked outcomes and
exit 1 for execution/aggregation failure; it never calls a timeout impossible.

## Search scheduling regression

The first iteration spent 3,191 transition probes on Level 8 but reached only
three platforms. Center-line obstruction costs ranked a farther platform behind
the starting area, and fresh arrivals reset their local exploration counter.
The final scheduler blends distance with a small obstruction hint and accounts
for effort across all arrivals on a platform. It retains different landing states
and every waiting tick; the fix changes ordering, not physical reachability.

The same Level 8 then required 93 transition probes to discover its certified
10.42-second route. A regression fixture continuously supplies fresh arrivals on
an early wheel and requires another wheel to receive search effort.

Another regression fixture covers a platform embedded in a ceiling: its center
is blocked, but the approach up to first contact can be clear. Obstruction hints
stop at the landing envelope instead of rejecting that approach. A wall before
contact still receives its penalty. Actual Unity collisions decide the outcome.

## Observed lifetime and finish contact

Breakable-wheel search waits use the active wheel's actual remaining countdown,
combined with oxygen time when present. This avoids spending probes on jump
commands after the wheel has exploded, including after an earlier visit started
its timer. Unknown wait time is -1; zero means detachment/death is imminent.
A two-tick allowance covers handoff boundaries. Both passive actions remain, so
natural forced-detach routes can still reach the next attachment. The real
breakable-wheel fixture checks the observed deadline and its later forced detach;
unit tests cover zero and positive remaining lifetimes.

Level 17 exposed a finish-pose comparison issue: identical routes reached the
same finish tick and earlier traversal checkpoints, but terminal Y differed by
0.00388 units. Waiting four extra ticks before the last jump also completed but
still varied by 0.00295 units at the finish. The pinned Physics2D contact offset
is 0.01. Completed-event X/Y therefore use that contact envelope; earlier launch
and attachment-checkpoint positions retain 0.001, as do all velocity comparisons.
Angle/oxygen limits and exact event frame/identity checks are unchanged. Tests
reject 0.011 terminal drift and 0.004 drift at an earlier decision pose. This is a
validator comparison policy change; no game physics or movement settings changed.
