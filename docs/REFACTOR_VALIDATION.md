# Architecture refactor validation

Run date: November 6, 2026. Editor: **6000.6.8f1** on Windows.
Baseline: `b0ff2d0`; branch: `codex/architecture-and-behavior-tests`.
The passing characterization baseline and plan were committed as `e0e0c88`
before production implementation. The plan was prepared with SOL medium;
implementation began with Luna and continued with the current model after the
user authorized the fallback when worker usage limits intervened.

## Executed automated checks

| Check | Result | Evidence |
| --- | --- | --- |
| Before refactor | 7/7 PlayMode tests passed | `TestResults/Baseline-PlayMode.xml`; six characterization tests and one 24-scene startup test |
| Repository | Passed: 317 assets/folders and unique GUIDs; 24 enabled scenes | `node scripts/validate-repository.mjs` |
| Original asset identities | All 242 baseline GUIDs retained | `docs/asset-guid-baseline.json`, enforced by repository validation |
| Scene/prefab serialization | Passed | All enabled scenes and prefabs checked for missing scripts, UnityEvent targets/methods and animation event receivers |
| After refactor, EditMode | 66/66 passed; none skipped | `TestResults/EditMode.xml` |
| After refactor, PlayMode | 30/30 passed; none skipped | `TestResults/PlayMode.xml`, including the retained characterization/startup checks |
| Repeat migrations | Passed: all 594 asset files unchanged by repeat | SHA-256 comparison before/after explicit gameplay/UI authoring commands |
| Windows development build | Passed | `Logs/BuildWindows.log`; `Builds/Windows/Worms.exe` inside the Unity project |
| Android development build | Passed, IL2CPP/ARM64, debug signing | `Logs/BuildAndroid.log`; `Builds/Android/Worms.apk` inside the Unity project |

The final full sequence uses `scripts/check-unity.ps1 -Check All`. Raw XML, JSON
summary, logs and builds stay in ignored local folders. These paths describe
local evidence, not artifacts automatically uploaded by GitHub. Test layers and
scenario mappings are documented in [TESTING.md](TESTING.md) and the
[plan](REFACTOR_PLAN.md). The manual Unity workflow needs a licensed self-hosted
runner; only repository hygiene runs automatically on GitHub.

Tests isolate progress using injected in-memory storage or disposable temporary
directories. They cover completion/death ordering, movement/attachment, hazards,
oxygen, navigation/story/pointer guards, save validation/recovery/retry, catalog
configuration and Canvas wiring/layout. The world-menu regression checks original
button events, catalog labels, lock references and non-intercepting badge graphics.
Rule matrices live in unit tests; PlayMode tests exercise representative adapter
paths rather than duplicating those matrices.

## Built-player observations and remaining acceptance

The Windows development player was launched at 720×1280. Main menu and both world
menus were inspected; World 1 level entry, the oxygen HUD, death panel and return
to the main menu were exercised. No runtime exceptions were found in
`Logs/Refactor-Player-final.log`. Rendered checks caught legacy World 2 labels and
badge wiring that startup tests alone missed; the migration and regression test
now correct those authored bindings. The final build was also checked at
1280×720: splash → main menu → World 2 showed the correct Level 11–20 labels,
all locked-level badges, readable controls and no obsolete rotated header. A tap
on the Level 18 lock stayed in the menu. Its log is
`Logs/Refactor-Player-landscape.log`, with no runtime exceptions found. Synthetic
safe-area/layout assertions do not prove device input.

The project retains its inherited mobile orientation settings. Canvas layout
supports changing aspect ratios, but a shipping orientation policy still needs
physical-device acceptance before release.

| Outstanding case | Status |
| --- | --- |
| Natural traversal of Levels 1–19, movement feel, full progression/relaunch | Not yet played through; follow `PLAYTEST_SCRIPT.md` |
| Natural Level 20 completion | Blocked: no authored finish trigger; see `CATALOG_REVIEW.md` |
| Actual Android install, held/multitouch input, notched safe areas, suspend/resume | Blocked: `adb devices` lists no connected device |
| iOS compilation/signing/device checks | Unverified; requires macOS/Xcode and signing |

These are explicit acceptance scripts rather than automated passes. A physics
finish probe validates integration; it does not prove a naturally reachable exit.
Old `.dat` progress remains untouched and is not imported. Existing players begin
fresh JSON progression with a visible notice; a legacy importer is separate work.

## Follow-up review

Review on November 6, 2026 found PR #2 already merged as `5eadcd8`. Review covered
the domain/save/session boundaries, navigation/input, gameplay adapters, serialized
bindings and test tooling. No further production-code changes were made in this
follow-up. The known device/content gaps remain listed above and in `BACKLOG.md`.

The validation runner could leave an earlier successful summary after a failed
invocation, and accepted a skipped-only report as executed tests. It now clears
the old summary before validating the editor path, publishes only after success,
and rejects suites with no passed/failed tests. Seven isolated runner regression
cases pass, including stale XML/summary, missing editor/report, failed/empty and
skipped-only suites. The required GitHub check now runs those cases without Unity.

The real Unity EditMode suite was rerun: **66/66 passed**, none skipped. The
previous **30/30 PlayMode** result, scene checks and Windows/Android builds remain
the production evidence for the unchanged game code/assets; they were not rerun
solely for backlog/documentation and runner-report changes.
