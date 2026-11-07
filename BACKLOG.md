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
| BL-002 | P0 | In progress | Quality | Verify natural traversal of every level | Levels 1–19 certified; BL-001 for Level 20; human/device acceptance remains |
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
| BL-015 | P2 | Proposed | Tooling | Generate and evaluate batches of candidate levels | BL-013 and BL-014 |
| BL-017 | P2 | Proposed | Monetization | Model ad revenue across usage scenarios | BL-016; sourced assumptions, not forecasts |
| BL-018 | P2 | Blocked | Monetization | Finish live acceptance of the approved ads integration | Code merged in PR #8; account/configuration/device work in BL-023–BL-031 |
| BL-019 | P1 | Proposed | Release | Define Android/iOS publishing requirements | Platform/account/distribution decisions |
| BL-020 | P1 | Proposed | Release | Create and execute a release readiness checklist | BL-019 and release blockers above |
| BL-021 | P2 | Proposed | CI | Provision a trusted Unity validation runner | Licensed Windows host and runner owner |
| BL-022 | P1 | In progress | Gameplay | Make precision jumps consistent across render rates | Levels 1–19 complete at 30/60/120 FPS; physical-device acceptance remains |
| BL-023 | P1 | Ready | Accounts | Establish AdMob and store account access, agreements and payments | Account owner to complete console setup |
| BL-024 | P1 | Blocked | Monetization | Register Android/iOS AdMob apps and interstitial units | BL-023; confirm existing store identifiers |
| BL-025 | P1 | Ready | Web | Publish developer website, privacy policy and app-ads.txt | BL-024 for the authorized seller entry |
| BL-026 | P1 | Blocked | Purchases | Configure Remove Ads products and store testers | BL-023; store agreements and Google Play public licensing key |
| BL-027 | P1 | Ready | Privacy | Review audience, child protections and store disclosures | Final audience/category decisions and BL-019 |
| BL-028 | P1 | Blocked | Configuration | Configure Unity IDs and sandbox monetization | BL-024–BL-027; production stays disabled |
| BL-029 | P1 | Blocked | Android | Accept ads and purchases on physical Android hardware | BL-028; device and licensed store tester |
| BL-030 | P1 | Blocked | iOS | Build and accept ads and purchases on iPhone | BL-028; Apple access, macOS/Xcode and physical iPhone |
| BL-031 | P1 | Blocked | Release | Approve and activate production monetization | BL-023–BL-030; approved release checklist and explicit release decision |
| BL-032 | P1 | Proposed | Maintenance | Evaluate migration to Google's Next-Gen Android ads backend | Verify Unity compatibility and Families SDK status before June 30, 2027 |
| BL-033 | P2 | Blocked | Monetization | Review live ad/purchase health and player impact | BL-031; real production data |

### Monetization launch sequence

The provider decision and initial code are complete in
[PR #8](https://github.com/David-and-Kenny/Worms-Lost-in-Space-and-Time/pull/8).
BL-018 remains open for live acceptance; BL-017 revenue modeling is useful research,
not a prerequisite for the already approved integration. Use
[MONETIZATION.md](docs/MONETIZATION.md) for exact fields, switches and evidence.
Start with BL-023, BL-025 and BL-027, then configure the app/products, test both
platforms and request the release decision. These items track future work; adding
them does not enable ads, approve an audience declaration or authorize publication.

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
The [current report](docs/FIXED_TICK_LEVEL_REPORT.md) records natural-input routes
for Levels 1–19, complete timing analysis and strict replay in two checkouts.
Level 20 is content-blocked; camera/input feel and device acceptance remain manual.

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
The [level-validation utility](docs/LEVEL_VALIDATION.md) implements local search,
certificates, profile retries and replay without AI tokens. Levels 1–19 are
certified; Level 20 lacks a finish. See [coverage evidence](docs/SOLVER_COVERAGE.md).
Physical-device acceptance remains BL-003; human difficulty is not calibrated.

### BL-015 — Generate candidate level batches

Generate reproducible candidates across agreed difficulty bands, run BL-014
validation and export a concise review report. Require human selection/playtests
before publishing candidates. Keep seeds/metadata and exclude discarded generated
build/cache output from source control.

### BL-016 — Assess ads compatibility and placement

**Done — November 6, 2026.** Selected Google AdMob and documented supported Unity/
platform dependencies, privacy requirements, tradeoffs and placement policy in
[MONETIZATION.md](docs/MONETIZATION.md), with dated verification and primary-source
links. Initial code merged in PR #8. Account setup, audience declarations, device
acceptance and production activation are separate work below.

### BL-017 — Estimate monetization scenarios

Build low/base/high scenarios using explicit daily-active users, sessions, ad
opportunities, opt-in/fill/show rates, geography and sourced eCPM assumptions.
Show formulas, dates, gross/net distinctions and sensitivity; identify missing
data. Treat results as conditional estimates and keep test traffic out of revenue.

### BL-018 — Implement approved ads

Initial AdMob/IAP adapters, editable settings, ownership persistence and automated
checks are merged in PR #8. The approved policy makes an ad eligible after 25
failed runs or 3 completions, with a five-minute foreground opening grace and
three-minute shared cooldown. A death plus Retry counts once. Ordinary developer
testing makes no ad requests; explicit preview uses Google sample units only.
Rewarded ads and banners are deferred. Close this parent after BL-023–BL-031 are
accepted with actual Android/iOS evidence; code/build passes alone do not establish
live ad delivery or purchase restoration.

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
For a monetized release, include the configuration, platform acceptance and account
approval evidence tracked by BL-023–BL-030. BL-031 uses this readiness checklist for
the separate activation decision and subsequent rollout verification.

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
The implementation and measured evidence are in
[FIXED_TICK_GAMEPLAY.md](docs/FIXED_TICK_GAMEPLAY.md). Physical input still has
render-sampling latency; the fix makes simulation outcomes consistent for observed
tick commands without claiming that every phone feels identical.
The solver iteration now certifies Levels 1–19, including Level 8, with successful
30/60/120 FPS policy runs. Level 20 lacks a finish. See
[completion times and measured timing demand](docs/FIXED_TICK_LEVEL_REPORT.md).

### BL-023 — Prepare publisher and store accounts

Confirm who owns and can administer the existing Google publisher/AdMob and Play
Console accounts. Establish Apple Developer/App Store Connect access. Complete
required identity verification, paid-app agreements, banking/tax/payment setup and
test access. Record each platform's status and remaining blockers without storing
passwords, service-account credentials or signing material in Git.

### BL-024 — Create platform-specific AdMob inventory

Confirm the real store package/bundle identifiers before changing
`com.KOGaming.worms`. Create or link separate Android and iOS AdMob apps and one
interstitial ad unit per app. Record all four app/ad-unit IDs and link public store
listings when available. Unpublished setup is acceptable initially; readiness
approval is required by BL-031.

### BL-025 — Publish website, policy and authorized seller file

Publish a developer website with support contact, an HTTPS privacy policy matching
actual SDK/data practices, and root-level `app-ads.txt` with AdMob's exact seller
entry. Link the website and policy from the appropriate store listings. Verify
public accessibility and AdMob's successful seller-file crawl; retain the URLs
and verification result as release evidence. Coordinate policy content with BL-027.

### BL-026 — Configure the permanent Remove Ads purchase

Create/activate `remove_ads` as a permanent non-consumable/one-time product in both
stores, with a $2.99 US base price, localized prices and descriptions. Configure
Google Play license testers and Apple sandbox/TestFlight access. Obtain Google's
public Play licensing key for local receipt verification. Confirm product
availability and identifiers; store purchase/restore acceptance belongs to
BL-029/BL-030. Purchases restore within their store account; cross-store transfer
is outside this launch scope.

### BL-027 — Complete audience and privacy review

Record the actual intended age groups and Apple category; the game currently adds
no age/download gate. Review the all-user child-protection/G-rated ad settings,
the exact SDK's Families status, merged Android advertising-ID permissions and any
neutral-age-screen or parental-gate requirements. File any needed code changes
before acceptance. Complete Google Play ads/audience/Data safety and Apple age/
privacy declarations and configure applicable AdMob Privacy & messaging. Keep the
no-ATT/no-cross-app-tracking intent consistent with the built apps. Record review
evidence before enabling Audience And Privacy Reviewed; child flags alone are not
proof of compliance.

### BL-028 — Configure the project for platform testing

Enter both platform app IDs in Worms MonetizationSettings and Google Mobile Ads
settings, with matching values. Enter both interstitial IDs, policy URL, public
Play licensing key and `remove_ads` product ID. Enable Store Purchases only once
test products/accounts are ready. Keep Production Enabled off and Preview Test Ads
off during ordinary development; enable preview explicitly for sample-ad testing.
Verify that development builds cannot select production ad units and incomplete
production configurations are rejected. Rebuild after configuration changes.

### BL-029 — Accept Android monetization on a device

Use an Android ARM64 build, Google sample ads and a licensed sandbox tester.
Execute DEVICE-ADS-01 and DEVICE-IAP-01 from
[PLAYTEST_SCRIPT.md](docs/PLAYTEST_SCRIPT.md): natural completion/retry placement,
death/retry deduplication, grace/cooldown, no-fill/offline handling, focus/audio/
input recovery, purchase/cancellation/deferred approval where supported, restore,
reinstall and offline ownership. Record build/device/OS, results and defects. Do
not click live ads or treat a successful APK build as device acceptance.

### BL-030 — Accept iOS monetization on a device

Build/sign through macOS/Xcode and distribute to sandbox/TestFlight testers.
Execute the same monetization scenarios on a physical iPhone, including Restore
Purchases after reinstall, lifecycle/audio/input behavior and no ATT prompt. Verify
safe-area rendering and the exported SDK privacy/attribution configuration. Record
device/OS/build and results; unavailable Apple hardware or signing/account access
remains a blocker rather than a pass.

### BL-031 — Enable and release approved monetization

Record publisher/app readiness approval, app-ads.txt verification, available store
products, accepted declarations and BL-029/BL-030 evidence. Obtain the separate
release decision required by BL-020. For each approved platform, set Store
Purchases Enabled, Audience And Privacy Reviewed and Production Enabled on; keep
Preview Test Ads off and Unity Development Build off. Build/sign a fresh release
candidate with real matching IDs and submit through the agreed rollout process.
Verify production delivery and purchaser ad suppression after launch without
generating developer test impressions/clicks on live units. Document rollback via
an ads-disabled build; these switches are built into the app, not remote controls.

### BL-032 — Plan the Android ads backend upgrade

Before June 30, 2027, recheck Google's support timeline for the standard 25.x
backend and evaluate the current Next-Gen alternative. Verify Unity/plugin and
Families SDK compatibility, migrate in a focused PR if approved, and rerun native
build, privacy, sample-ad and device acceptance checks. Keep production traffic
off during developer validation and record the selected versions and evidence.

### BL-033 — Review production monetization health

After launch, review Android and iOS separately using existing AdMob/store reports:
ad availability/fill, impressions and revenue per active player, purchase/restore
issues, crashes and available retention/player-feedback signals. Exclude developer
test traffic, compare observations with BL-017 assumptions, and propose frequency
changes with player-impact evidence. Adding another analytics SDK or mediation
provider requires a separate decision.

## Completed foundation

- [x] **BL-014:** reusable token-free viability search, certificates, complementary
  profiles and reporting; all 20 levels evaluated, 19 certified and strictly
  replayed in two checkouts — [evidence](docs/SOLVER_COVERAGE.md).
- [x] Recover and upgrade the Unity project; remove tracked caches/obsolete ad SDK
  and establish a shared Git workflow — [PR #1](https://github.com/David-and-Kenny/Worms-Lost-in-Space-and-Time/pull/1).
- [x] Introduce testable models/adapters, JSON saves, catalog/navigation, Canvas UI
  and regression suites — [PR #2](https://github.com/David-and-Kenny/Worms-Lost-in-Space-and-Time/pull/2).
- [x] **BL-016:** select AdMob and merge the initial ads/Remove Ads code, safe
  developer defaults and automated checks — [PR #8](https://github.com/David-and-Kenny/Worms-Lost-in-Space-and-Time/pull/8).
  Live account/configuration/device/release acceptance remains BL-018 and
  BL-023–BL-031.

These foundations do not close the product/device acceptance items above. Current
evidence and limitations are in [REFACTOR_VALIDATION.md](docs/REFACTOR_VALIDATION.md).
