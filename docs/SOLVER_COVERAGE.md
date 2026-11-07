# Solver coverage improvement

Baseline: `dea89b3` (merged fixed-tick gameplay); pinned editor 6000.6.8f1.
The unchanged nine-test validation integration suite passed in the isolated
checkout before production edits. Evidence: `TestResults/Solver-Baseline.xml` and
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
points. Measured results and remaining limitations will be recorded below.

## Reusable portfolio

`scripts/validate-catalog.ps1` runs the full catalog with the balanced profile
(256-state beam, 120-second route horizon), then retries only inconclusive scenes
with a 30-second horizon and a 1,024-state beam. If that also misses, a focused
profile uses a 16-state beam and 60-second horizon to spend more launch angles on
each retained arrival. Each profile has its own default five-minute wall budget;
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

```powershell
./scripts/validate-catalog.ps1 -OutputDirectory TestResults/Catalog/current
# Resume a complete same-source first pass without paying its search cost again.
./scripts/validate-catalog.ps1 -InitialRun TestResults/LevelValidation/Coverage-Catalog -OutputDirectory TestResults/Catalog/continued
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
