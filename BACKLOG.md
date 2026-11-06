# Product backlog

Working title: **Worms — Lost in Space and Time**. Last reviewed: **2026-11-06**.
This is the shared product backlog for features, research and known issues. The
initial items come from the product draft and verified repository findings.
Priorities are provisional; feature ideas are not approved design decisions.

## How we track work

- **Priority:** P0 blocks a playable/releasable build; P1 establishes the product
  or development foundation; P2 improves the experience; P3 explores later options.
- **Status:** Proposed → Ready → In progress → Done. Use Blocked with a reason.
  Ready means scope, acceptance criteria and dependencies are agreed.
- Keep IDs stable. Add an owner and issue/PR link when work starts. Split large
  items into linked tasks without deleting the parent acceptance criteria.
- A completed item needs reviewable changes, relevant automated tests or a manual
  test script, and recorded results. Hardware checks remain unverified until run.
- Update this file in the implementing PR. Record product decisions and links;
  keep detailed implementation guidance in the architecture/test documents.

All open items are currently unassigned. Order items within a priority when
planning the next increment; the table is an inventory, not a delivery schedule.

## Open work

| ID | Priority | Status | Area | Outcome | Depends on / blocker |
| --- | --- | --- | --- | --- | --- |
| BL-001 | P0 | Ready | Content | Give Level 20 a reachable exit | Designer must author and playtest the exit |
| BL-002 | P0 | Ready | Quality | Verify natural traversal of every level | BL-001 for Level 20 |
| BL-003 | P0 | Blocked | Mobile | Validate Android controls and app lifecycle | Physical Android device/tester |
| BL-004 | P1 | Proposed | Direction | Write the game vision and description | Product/design decisions |
| BL-005 | P1 | Proposed | Gameplay | Evaluate the core loop and retention | BL-004; representative playtesters |
| BL-006 | P2 | Proposed | Gameplay | Prototype selected new mechanics | BL-005; select one hypothesis first |
| BL-007 | P1 | Proposed | UI | Refine responsive menus, HUD and feedback | BL-003 for device acceptance |
| BL-008 | P1 | Proposed | Art | Audit placeholder art and define visual direction | BL-004 |
| BL-009 | P2 | Proposed | Art | Replace approved placeholder assets | BL-008 |
| BL-010 | P1 | Proposed | State | Review save/session integrity and tampering risks | Explicit offline/online threat model |
| BL-011 | P2 | Proposed | State | Decide legacy-save compatibility policy | Existing `.dat` files are retained, not imported |
| BL-012 | P1 | Proposed | Levels | Define authoring and difficulty rules | BL-004 and BL-002 observations |
| BL-013 | P1 | Proposed | Tooling | Build maintainable level-authoring tools | BL-012 |
| BL-014 | P1 | In progress | Tooling | Validate generated/authored level feasibility | Utility implemented; validation/review in progress |
| BL-015 | P2 | Proposed | Tooling | Generate and evaluate batches of candidate levels | BL-013 and BL-014 |
| BL-016 | P2 | Proposed | Monetization | Assess a current ads integration | BL-004; ads currently inactive |
| BL-017 | P2 | Proposed | Monetization | Model ad revenue across usage scenarios | BL-016; sourced assumptions, not forecasts |
| BL-018 | P2 | Proposed | Monetization | Integrate approved ad placements | BL-016 and BL-017; product approval |
| BL-019 | P1 | Proposed | Release | Define Android/iOS publishing requirements | Platform/account/distribution decisions |
| BL-020 | P1 | Proposed | Release | Create and execute a release readiness checklist | BL-019 and release blockers above |
| BL-021 | P2 | Proposed | CI | Provision a trusted Unity validation runner | Licensed Windows host and runner owner |
| BL-022 | P1 | Proposed | Gameplay | Make precision jumps consistent across render rates | Measured fixed-simulation/input/attachment design; physical-device verification |

## Acceptance criteria

### BL-001 — Complete Level 20

The catalog audit confirms no authored finish trigger in Level 20. Author an exit
that fits the level design; reach it from entry without teleporting. Completion
must show the final result, save once and offer valid world/menu navigation.
Add an authored-content regression check and record the natural playthrough.

### BL-002 — Establish playable level coverage

Follow [PLAYTEST_SCRIPT.md](docs/PLAYTEST_SCRIPT.md) for Levels 1–20. Record route,
hazards, attachment/camera/oxygen behavior, completion time and next destination.
File reproducible defects with level IDs. Startup tests and simulated finish
triggers must not be presented as proof of natural reachability.

### BL-003 — Verify on a physical Android device

Install the ARM64 development APK and test real held touch, multitouch, menu input
exclusion, safe areas, supported orientations, suspend/resume, force-stop/relaunch
and persisted progress. Record device/OS/build and logs. Resolve failures or link
them as release blockers. iOS device coverage belongs to BL-019/BL-020.

### BL-004 — Document product direction

Agree on the target player, core fantasy/loop, session length hypothesis, challenge
and failure philosophy, progression goals, tone and explicit non-goals. Write a
short player-facing description and contributor/AI decision guide. Mark unknowns
and distinguish decisions from hypotheses; leave the final name open until chosen.

### BL-005 — Evaluate engagement and retention

Explain the current loop's strengths/friction using observed playtests. Define
onboarding, repeat-play and retention hypotheses, a small measurement plan and
prioritized experiments. Select success criteria before implementing experiments;
use enjoyable repeat play as the goal rather than assuming compulsive behavior.

### BL-006 — Test new mechanic hypotheses

Choose a mechanic tied to BL-005, document its expected benefit and build a small
reversible prototype. Add rule/adapter tests and a playtest script. Record whether
to keep, revise or remove it based on the agreed criteria.

### BL-007 — Polish responsive UI

Audit every screen for hierarchy, contrast, typography, feedback and touch targets.
Agree on an orientation policy and test representative portrait/landscape sizes
and notched safe areas. Keep Canvas prefabs editable, preserve navigation/input
guards, and update layout/interaction tests plus before/after screenshots.

### BL-008 — Establish art direction

Inventory sprites/backgrounds/UI art, flag placeholders and document ownership or
license uncertainty. Produce a reviewed concept board spanning multiple planned
environments, with palette, shape, contrast and readability guidance. Prioritize
replacements and agree on source/export formats.

### BL-009 — Produce replacement art

Create only assets approved through BL-008. Preserve gameplay silhouettes and
collision intent, keep editable sources with provenance/license notes, and verify
imports, scale, animation and in-game readability on target screens.

### BL-010 — Review app/session state

Assess crash/interrupted-write recovery, corruption, schema changes, retry,
pause/resume and upgrade behavior. Document what a local save can and cannot
protect against, including player editing. Decide whether any future competitive
or paid value needs server authority; do not promise tamper-proof offline saves.
Add focused failure tests and record device lifecycle evidence where available.

### BL-011 — Decide whether to import old progress

Evaluate whether real legacy player data needs conversion. Record a decision to
retain the fresh-JSON policy or design a separately reviewed safe importer.
Preserve original bytes and explain compatibility to players; do not reintroduce
runtime BinaryFormatter deserialization.

### BL-012 — Define level design and difficulty

Document each level's teaching/purpose, required mechanics, difficulty factors,
oxygen budget, hazards, camera transitions and target audience skill. Review
existing star thresholds: Levels 1 and 2 have authored targets; zero targets on
later levels disable the challenge. Choose new targets through playtesting.

### BL-013 — Build a level-authoring framework

Define versioned, inspectable level data and reusable components with stable IDs.
Provide an authoring workflow usable by people and AI, validation diagnostics,
example levels and documentation. Keep level changes reviewable in Git and test
serialization, invalid input and deterministic generation where applicable.

### BL-014 — Evaluate level feasibility

Check required entry/exit objects, references, bounds and structural constraints.
Define a repeatable movement simulation or agent probe with recorded seed/input
and budgets. Report demonstrated routes separately from inconclusive or failed
search; a solver timeout must not be labelled proof that a level is impossible.
Validate results against known playable and deliberately invalid fixtures.
The initial [level-validation utility](docs/LEVEL_VALIDATION.md) implements local
search, certificates and replay without AI tokens; unresolved catalog routes and
physical-device acceptance remain explicit in its reports.

### BL-015 — Generate candidate level batches

Generate reproducible candidates across agreed difficulty bands, run BL-014
validation and export a concise review report. Require human selection/playtests
before publishing candidates. Keep seeds/metadata and exclude discarded generated
build/cache output from source control.

### BL-016 — Assess ads compatibility and placement

Research maintained SDK/mediation choices, current Unity/platform compatibility,
dependencies, consent/privacy needs and player-friendly placement options. Record
dated primary sources, costs/tradeoffs and a recommendation. The obsolete SDK was
removed; this item is discovery, not permission to activate ads.

### BL-017 — Estimate monetization scenarios

Build low/base/high scenarios using explicit daily-active users, sessions, ad
opportunities, opt-in/fill/show rates, geography and sourced eCPM assumptions.
Show formulas, dates, gross/net distinctions and sensitivity; identify missing
data. Treat results as conditional estimates and keep test traffic out of revenue.

### BL-018 — Implement approved ads

After a placement/provider decision, integrate test ads with isolated adapters.
Validate reward-once behavior, no-fill/failure/offline paths, consent and lifecycle,
and ensure ads do not interrupt critical gameplay. Keep keys/configuration handled
appropriately and record Android/iOS device tests before enabling production ads.

### BL-019 — Map the publishing path

Research dated Google Play and Apple requirements using primary sources. Explain
which work happens in Unity/Hub versus store consoles, signing and platform tools.
Cover accounts, identifiers, signing/key ownership, build formats, supported APIs,
store assets/ratings, privacy/data declarations and any in-app platform flows.
Record account/hardware blockers and assign release responsibilities.

### BL-020 — Establish release acceptance

Create a repeatable checklist for functional/device/performance checks, content
completion, art/licenses, save upgrades, privacy, signing, store metadata and staged
distribution/rollback. Produce an installable candidate and record test evidence.
Store submission/public release requires a separate explicit release decision.

### BL-021 — Run licensed Unity checks in CI

Provision an owned, trusted Windows runner for the pinned editor, Node/Git LFS and
Android tools. Run the existing manual workflow successfully and retain useful
failure artifacts. Keep untrusted fork code off the runner and document maintenance
and license handling. Repository hygiene alone is not gameplay validation.

### BL-022 — Separate simulation timing from rendering

Use [the frame-rate investigation](docs/FRAME_RATE_INVESTIGATION.md) as the starting
point. There is evidence that recorded jump policies behave differently at
30/60/120 Hz, not that a faster screen doubles the whole game's speed. Define one
fixed simulation owner, buffered input and attached-body collision behavior, then
choose a measured mobile rendering policy. Preserve force/contact baselines and
recertify routes after the change; measure actual FPS and display refresh separately
on devices. This gameplay refactor is separate from the level-validation utility.

## Completed foundation

- [x] Recover and upgrade the Unity project; remove tracked caches/obsolete ad SDK
  and establish a shared Git workflow — [PR #1](https://github.com/David-and-Kenny/Worms-Lost-in-Space-and-Time/pull/1).
- [x] Introduce testable models/adapters, JSON saves, catalog/navigation, Canvas UI
  and regression suites — [PR #2](https://github.com/David-and-Kenny/Worms-Lost-in-Space-and-Time/pull/2).

These foundations do not close the product/device acceptance items above. Current
evidence and limitations are in [REFACTOR_VALIDATION.md](docs/REFACTOR_VALIDATION.md).
