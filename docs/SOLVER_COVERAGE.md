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
