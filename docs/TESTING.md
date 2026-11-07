# Test strategy

Use Unity **6000.6.8f1**. Only one editor process may use a checkout at a time.
Tests never use a developer's save directory: domain tests inject storage, file
tests use disposable temporary directories, and scene/UI tests override services.

## Commands

Run from the repository root, with Unity closed:

```powershell
$editor = 'C:\Program Files\Unity\Hub\Editor\6000.6.8f1\Editor\Unity.com'
node scripts/validate-repository.mjs
node --test scripts/report-level-validation.test.mjs
./scripts/test-check-unity.ps1
./scripts/test-validate-catalog.ps1
./scripts/check-unity.ps1 -UnityEditor $editor -Check EditMode
./scripts/check-unity.ps1 -UnityEditor $editor -Check PlayMode
./scripts/check-unity.ps1 -UnityEditor $editor -Check Scenes
./scripts/check-unity.ps1 -UnityEditor $editor -Check BuildWindows
./scripts/check-unity.ps1 -UnityEditor $editor -Check BuildAndroid
```

`-Check All` runs the repository check, scene validation, both test suites and both
builds sequentially. Use `-TestFilter FullyQualifiedTestName` to diagnose a failure,
then rerun the affected full suite. The runner rejects an absent report, zero
executed tests, failed tests, or a failed editor exit. XML reports and a JSON run
summary are written under ignored `TestResults/`; editor logs are under `Logs/`.
Each invocation removes any previous summary before running and publishes a new
one only after every requested check passes. Skipped-only suites are rejected.
The lightweight runner regression script verifies exit/report handling and stale
result cleanup in a temporary fixture without launching Unity.

## What belongs in each suite

| Layer | Responsibility | Examples |
| --- | --- | --- |
| EditMode, ordinary objects | Rule combinations and failure boundaries | Versioned saves, validation/recovery, unlocking and collectible rules, final-level policy, terminal-state ordering, story input decisions |
| EditMode, editor/data integration | Authored configuration | Catalog uniqueness/coverage, prefab references, scene/animation/UnityEvent binding integrity |
| PlayMode | Actual Unity component wiring | Rigidbody launch/attachment, triggers, countdown ownership, oxygen/death, completion/result UI, navigation buttons, safe-area layout |
| Built player/device | Platform and human interaction | Natural level reachability, movement feel, real keyboard/touch, readable rendering, suspend/resume, install/relaunch |

Do not repeat the same threshold matrix in PlayMode and unit tests. Test the rule
in isolation, then test one representative path through its Unity adapter. The
single scene smoke test loads all 24 enabled scenes; do not add another suite of
equivalent one-second startup checks. A simulated completion tests integration,
not whether a human can reach an exit.

The detailed coverage contract and scenario IDs are in
[`REFACTOR_PLAN.md`](REFACTOR_PLAN.md), sections 5–6. Manual steps and evidence
requirements are in [`PLAYTEST_SCRIPT.md`](PLAYTEST_SCRIPT.md).

## Before and after evidence

`docs/BASELINE_BEHAVIORS.md` records the seven-test passing pre-refactor baseline.
`TestResults/Baseline-PlayMode.xml` and `Logs/Baseline-PlayMode.log` retain its local
raw evidence. Baseline expectations retain initial/final velocities, wheel/spike
motion, camera targets/axis transitions, and seven-part death output. Typed
fixture wiring may replace name-dependent setup after migration; assertions must
continue to describe the same observable behavior.

Intentional changes have separate expectations: one terminal result per run,
correct catalog routes, a safe final level, recoverable versioned JSON saves,
responsive unrotated UI, and a story press/release guard. Do not encode a known
legacy defect as required parity. Save/progression tests now use injectable storage;
the baseline does not claim unsafe tests against a person's old binary save.

## CI and collaboration

GitHub's required `Repository hygiene` check validates repository structure and
the validation runner's report/exit handling using a fake editor. It
does not execute Unity and must not be described as a gameplay test. The optional
`Unity validation` workflow runs the full validation command on a trusted,
licensed Windows self-hosted runner labelled `unity-6000.6.8f1`; it is manual so
untrusted fork code cannot run on a developer's machine automatically. Install
Git LFS, Node 24, the pinned editor, and Android tools on that runner first.

For a behavior PR, include the executed suite counts/results and relevant scenario
IDs, not a claim of total coverage. Attach XML/logs or summarize their failures.
Device cases remain unverified until someone records an actual device run.
The Node report tests cover execution-demand bands, incomplete evidence, disconnected
timing windows, aggregation/source guards and the actual Markdown/CSV CLI. They
run in repository CI without Unity or an AI service.
The catalog portfolio harness additionally checks fallback selection, matching-source
resume, clean output isolation, certificate selection, strict replay and propagation
of execution errors using a fake backend. It does not claim to run Unity.

## Monetization scenarios

| ID | Scenario and primary layer |
| --- | --- |
| ADS-01 | EditMode: 25 distinct failed runs or 3 completions; duplicate death/retry does not increment twice; both counters reset only on opened ads |
| ADS-02 | EditMode: five-minute opening grace, three-minute shared cooldown, consent/ownership gating, missing ads and failed presentation |
| ADS-03 | EditMode: repeated taps/SDK events navigate once; cancelled/stale callbacks cannot navigate a new screen |
| ADS-04 | EditMode: committed defaults request no ads or purchases; developer preview uses only sample IDs |
| IAP-01 | EditMode: idempotent grants, durable ownership, failed writes, cached offline ownership, authoritative revocation, isolated purchase storage |
| UI-ADS-01 | PlayMode: editable settings panel opens/closes, bindings and unavailable-store behavior; existing safe-area fixture includes its controls |
| DEVICE-ADS-01 | Device: test ads at natural completion/retry transitions, foreground timing, audio/focus restoration, suspend/resume and no closing-touch leakage |
| DEVICE-IAP-01 | Device: store purchase/cancel/defer/restore/reinstall/offline ownership and purchase-confirmation retry |
| RELEASE-ADS-01 | Editor: production build guard rejects incomplete/sample configuration; SDK app IDs must match; child/privacy declarations require external review |

See `MONETIZATION.md` for account configuration and `PLAYTEST_SCRIPT.md` for
device steps. Automated navigation/session fixtures prove wiring, not natural
traversability. Use the existing no-AI level-validation tooling for traversal.

## Level 1 artwork review (ART-01)

The first art pass uses Level 1 scene overrides. Validate all scenes/prefabs after
applying `Level1ArtTools.Apply`; run EditMode and PlayMode for serialized UI and
physics wiring. Replay the maintained Level 1 route as a proposal after its
dependency fingerprint changes, then strictly replay the newly certified route.
Never substitute a screenshot for natural traversal evidence.

`Level1ArtCaptureTests.CaptureAdvancingLevelOne` checks the five cog and seventeen
pipe sprite bounds, the retained character, and advancing frame/time/wheel state
with injected in-memory storage. Screenshot output is opt-in: pass
`-worms-art-capture` with a graphics device (omit `-nographics`). It renders the
runtime camera and existing Canvas UI to `TestResults/Art`; ordinary suites run
the presentation integration checks without rendering. See
`art/concepts/level-01/README.md` for the command and source prompts.
Review portrait readability, distinct foreground/background metal colors, oxygen
frame padding, and visible alignment with unchanged collision boundaries. These
camera renders do not establish device behavior; landscape background coverage
and actual mobile input/lifecycle remain manual player/device checks.

When tagging replaced originals `.legacy`, verify that image bytes and `.meta`
GUIDs are unchanged, and validate all scenes/prefabs because the originals may
still be shared. Run the art authoring command against the legacy source paths
and require the approved scene/textures to stay unchanged. Asset path changes
invalidate route fingerprints: recertify maintained inputs and strictly replay
the newly generated certificates rather than editing their hashes.

## Start and menu artwork (ART-MENU-01)

`MenuArtCaptureTests.ThemedMenusUseReadableAssetsAndRetainControls` loads Splash,
StorySelect, World1 and World2 with injected disposable progress. It checks the
new backdrop, fonts, retained character, pipe-button/lock/star assets and settings panel
open/close behavior. Existing Canvas integration tests cover level labels,
navigation callbacks, safe-area layouts and unavailable-store controls.

Pass `-worms-menu-capture` with a graphics device to render portrait and landscape
screens/settings to ignored `TestResults/Art/Menus`. Inspect title/button text,
pipe frames, badge placement, background coverage and hidden-option layout.
Both world selectors must have thirteen smaller decorative pipe connections,
with raycasting disabled, behind the buttons in either orientation.
Verify `MenuArtTools.Apply` leaves scenes/prefabs unchanged when reapplied and
retains original image bytes/GUIDs plus legacy prefab copies. Run repository,
scene/prefab, EditMode and PlayMode checks. Recertify maintained routes from their
recorded inputs after runtime dependency changes, then strictly replay them.
Actual phone input, notches, store flows and lifecycle remain device checks.
See `art/concepts/menus/README.md` for source prompts and capture instructions.

## Automated level traversal

The reusable [level validator](LEVEL_VALIDATION.md) adds EditMode search/contract
tests and physical PlayMode fixtures. Run `-Check PlayMode -TestFilter Worms.LevelValidation`
for its focused integration suite. Ordinary full-suite execution does not launch
a catalog search: the command entry point only dispatches a search/replay when a
request is explicitly supplied. `scripts/validate-levels.ps1` is the no-AI CLI for
those requests. Search outcomes and saved-route regressions supplement the existing
24-scene smoke test; they do not duplicate it or replace device/feel acceptance.
`SearchCoverageTests` covers complete waiting-tick scheduling, passive/extended
fallbacks, landing diversity, thin obstacles, reachable collider edges and search
fairness. The catalog harness checks all three profile choices, analysis retries,
same-source resume, certificate selection and strict replay; partial, stale,
unstable and execution-failed evidence must fail the command.
Matching-source completed worker runs can supplement a catalog pass; tests require
those verified routes to skip redundant search and reject changed-source evidence.

## Fixed-tick movement acceptance

`JumpInputBufferTests` covers brief observed taps, held input, airborne expiry and
pause/reset clearing without duplicating the pointer-gate rule matrix.
`FrameRateConsistencyTests` runs the actual coordinator with ordinary Unity physics
at 30/60/120 rendered FPS and an uneven render schedule. It compares the launch
tick/velocity, subsequent flight position, moving-hazard pose, oxygen consumption
and forced-detach tick, and checks the rendered-input-to-physics buffer adapter.
`ValidationIntegrationTests` additionally replays a real collision/finish route at
30/60/120 FPS and compares tick-aligned traversal evidence with one completion save.

Existing gameplay adapter tests wait for the responsible fixed step; body-pose
assertions use Rigidbody2D rather than the deliberately interpolated Transform.
This preserves the force, held-input, pointer, same-wheel suppression, countdown,
death, save and camera expectations while allowing the intentional scheduling
change. Physical tap latency, perceived feel and suspend/resume remain device
acceptance cases in `PLAYTEST_SCRIPT.md`. See `FIXED_TICK_GAMEPLAY.md` for results.
