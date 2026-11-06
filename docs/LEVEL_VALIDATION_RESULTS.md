# Level validation evidence — November 6, 2026

The utility executes locally without an AI service. Five existing levels have
maintained certificates at the 60 Hz reference profile: **1, 3, 4, 8 and 10**.
They are reviewed files in [`ValidationRoutes/`](../ValidationRoutes).
These are evidence of real finish traversal through injected jump input, with
fresh controlled repetitions and ordinary automatic-physics verification.

## Catalog search

The completed 20-level matrix used a deliberately short **30 wall seconds per
level**, 120 simulated seconds per trial, up to 10,000 trials, 64 jumps, beam width
64, seed 1729 and 60 rendered frames per simulated second. It finished all 20
requests: six verified routes, thirteen inconclusive searches and one content
failure. `Inconclusive` is not evidence that a level is impossible. Level 20 has
no active authored finish; the utility does not manufacture one to obtain a pass.

The reproducible machine-readable snapshot is
[`LEVEL_VALIDATION_CATALOG.csv`](LEVEL_VALIDATION_CATALOG.csv). Local raw evidence
is under `TestResults/LevelValidation/Final-Matrix/`, including the request,
manifest, complete summary, per-level reports, command NUnit XML and editor logs.
The source revision was `b8f7c5c` in the isolated verification checkout (equivalent
to main-branch `9b75a55`). Subsequent fixes preserved catalog gameplay and corrected
scene-path identity resolution and cross-checkout text fingerprint normalization.
The five maintained certificates were then freshly verified on the final source.

Level 7 passed the original matrix and isolated-checkout certification, but its
recorded policy later failed automatic-physics verification in the main checkout.
Fresh certification rejected that candidate and exhausted a 60-second search
budget. It is deliberately excluded from the maintained regression set. This is
additional evidence of scheduler-sensitive gameplay; a controlled-clock pass
alone is insufficient. The current report is
`TestResults/LevelValidation/Certified-Portable/level-07.report.json`.

An earlier longer search exercised the default five-minute budget, including
levels that consumed the full budget. That exploratory run was stopped after 13
levels and is **not** reported as a complete catalog pass. Normal use defaults to
five minutes per level; deeper searches can still be inconclusive.

## Regression evidence

| Check | Result | Scope |
| --- | --- | --- |
| EditMode | 86 passed, zero failed | Existing rules/configuration plus search, certificate, identity and line-ending boundaries |
| PlayMode | 39 passed, zero failed | Existing gameplay/UI, all-scene startup and actual validation fixtures |
| Validation fixtures | Included in the suites above | Static route, sealed exit, moving hazard, breakable wheel, narrow exit, direct exit |
| Runner harness | Seven cases passed | Existing editor/report failure handling with a fake editor |
| Repository hygiene | Passed | Asset metadata/GUIDs and 24 enabled build scenes |
| Strict saved-route batch | Five passed in each of two checkouts | Ten controlled and three ordinary-physics repetitions per route, without search |
| Single-file CLI replay | Passed | Default scene selection from the Level 1 certificate |
| Windows development build | Succeeded | Unity 6000.6.8f1, shipping runtime changes included |
| Android development build | Succeeded | IL2CPP/ARM64; compilation, not a device acceptance test |

The Windows/Android build logs are `Logs/BuildWindows.log` and
`Logs/BuildAndroid.log`; outputs are under the project's ignored `Builds/` folder.
Both builds preceded the final test-only search/replay fixes. Shipping player,
wheel and moving-hazard code did not change after those builds. Full PlayMode
tests and certificate replay cover the subsequent tooling changes.
Current replay artifacts are `TestResults/LevelValidation/Strict-Portable-Batch/`,
`Strict-Single/` and `Cross-Checkout-Replay/` under the same validation directory.

Tests use disposable progress storage. Certificates require a single accepted
completion write; traversal never calls the completion API as a shortcut. Missing
oxygen is reported as unavailable (`-1`), not fabricated as a remaining resource.

## Limits and next work

- Recorded jump policies differ at 30/60/120 rendered FPS. A 60 Hz certificate is
  not a mobile-device or cross-profile guarantee. The cause and proposed separate
  gameplay fix are in [FRAME_RATE_INVESTIGATION.md](FRAME_RATE_INVESTIGATION.md).
- Search is bounded and incomplete. Calibration itself can consume much of a
  short budget in a level with a long passive lifetime. Increase the budget or
  supply a recorded-input proposal; do not relabel unresolved levels as failures.
- Timing windows vary one jump at a time within ±250 ms. They do not establish
  human difficulty, combined error tolerance or success outside the sampled range.
- No physical Android/iOS acceptance run was performed. Editor trajectory menus
  and gizmos compile, but their visual usability still needs manual acceptance.
- GitHub repository hygiene runs automatically. The licensed Unity workflow is
  manual and needs a provisioned trusted runner; local Unity passes are not
  claimed as GitHub-hosted gameplay checks.

See [LEVEL_VALIDATION.md](LEVEL_VALIDATION.md) for token-free search, strict replay,
source-change recertification, artifacts and exit codes.
