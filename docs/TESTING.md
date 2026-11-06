# Test strategy

Use Unity **6000.6.8f1**. Only one editor process may use a checkout at a time.
Tests never use a developer's save directory: domain tests inject storage, file
tests use disposable temporary directories, and scene/UI tests override services.

## Commands

Run from the repository root, with Unity closed:

```powershell
$editor = 'C:\Program Files\Unity\Hub\Editor\6000.6.8f1\Editor\Unity.com'
node scripts/validate-repository.mjs
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

GitHub's required `Repository hygiene` check validates repository structure. It
does not execute Unity and must not be described as a gameplay test. The optional
`Unity validation` workflow runs the full validation command on a trusted,
licensed Windows self-hosted runner labelled `unity-6000.6.8f1`; it is manual so
untrusted fork code cannot run on a developer's machine automatically. Install
Git LFS, Node 24, the pinned editor, and Android tools on that runner first.

For a behavior PR, include the executed suite counts/results and relevant scenario
IDs, not a claim of total coverage. Attach XML/logs or summarize their failures.
Device cases remain unverified until someone records an actual device run.
