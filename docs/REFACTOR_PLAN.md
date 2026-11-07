# Unity architecture and behaviour refactor plan

Implemented follow-up: modern monetization is now a separate integration layered
on the refactored architecture. See `MONETIZATION.md` for current behavior and
external release dependencies. The original refactor's ads exclusion below is
historical scope, not a claim that the later SDK integration is absent.

Planning baseline: merged modernization at `b0ff2d0`; target editor remains **6000.6.8f1**. This document specifies implementation and acceptance, not completed verification. Baseline evidence belongs in `docs/BASELINE_BEHAVIORS.md`; record results separately from proposed expectations.

## 1. Baseline and scope gates

Before production edits, run and record characterization checks against existing code. Capture each script/prefab/scene GUID, scene references, public serialized fields, UnityEvent method strings, animation event names, enabled build scenes, physics settings, and representative screenshots. Preserve that fixture through the refactor. Existing `SceneSmokeTests` starts 24 scenes for one second; it does not establish gameplay correctness.

Inspect actual instantiated scene components in the pinned editor when extracting catalog metadata: YAML prefab overrides alone cannot resolve inherited values. Export a reviewed table containing scene path, stable ID, world, legacy index, finish count, effective `currentLevel`, `starTime`, `nextLevel`, and missing/duplicate exits. Do this before changing those scripts. Validate disabled experimental scenes for serialization integrity without enabling them as game content.

Observed behaviours and authored defects:

- `Finish.prefab` contains both `SaveGame` and `Finish`. Two independent trigger callbacks respectively save/time the run and destroy the player/create result UI. Callback order is not an authority for correctness.
- `SaveGame` rounds `Time.timeSinceLevelLoad` to two decimal places; star comparison is strictly `< starTime`. `GameController` defaults to level 1 and 31 star slots, retains earned stars, and advances maximum progression. `FinishedTime` PlayerPrefs carries transient results.
- Level 1 overrides threshold to 10 seconds; Level 2 to 15. Levels 3–19 appear to inherit threshold 0. Treat zero as no timed star challenge; invent no thresholds.
- Levels 11–19 reference `Finish.prefab` without `currentLevel`/`nextLevel` overrides: inherited values are 0 and Level2. Level20 has no reference to that prefab. Confirm its complete scene content before deciding how its authored exit is migrated.
- `StorySelect` offers an empty third-world destination; Continue constructs `Level` plus an integer and can request missing content. Death/results Back hard-codes World1.
- Player starts with downward velocity 3; attached jump velocity is local up times 10; public `detatch()` uses up times 5. Space and touch are held signals, not button-down signals. Wheel attachment copies holder position/rotation then rotates player -90 degrees; scale reflects wheel direction. Conveyor velocity is local right times authored speed. Same-wheel collision suppression is intentional baseline behaviour.
- Wheel rotation and attachment run in `Update`; moving those directly to `FixedUpdate` would change timing and needs independent measured approval. Preserve timing in this refactor.
- Names drive conveyor/destructible/camera detection and `CountdownExplode` finds `Player 1`. Replace those dependencies with typed references. Keep legacy component classes and event entry points.
- World menus already use Canvas buttons with `LevelSelectScript.Selectlevel(string)`. Extend their design rather than replacing them with another independent menu technology. Other menus/HUD use rotated `OnGUI`.

Intentional corrections are separate from parity assertions: one terminal result per run; correct world/level identity and next routes; safe last-level navigation; no missing third world; no runtime binary deserialization; robust corrupt-save handling; readable responsive UI. Preserve all other measured behaviour. Baseline failures documenting these defects must remain labelled defects; do not make a deliberately broken expectation an acceptance requirement.

Level20's missing exit is a content gap until inspection proves otherwise. Do not invent exit geometry in this refactor. Test final-level end policy via the public session/host API and mark natural Level20 completion blocked when no authored exit exists. No automated transition test proves all levels are solvable.

## 2. Architecture and exact shared contracts

Use ordinary cohesive objects for progression, session, oxygen and attachment rules. Unity components receive callbacks, read input, apply transforms/rigidbodies, and present state. Avoid service locators inside pure domain objects, inheritance frameworks, a generic event bus, or an interface for every concrete class.

New isolated Core contracts may be prepared while baseline checks run. Parent must declare **BASELINE PASS** before the root runtime asmdef, bootstrap or legacy production edits. Implementation agents author editor-only migration/builders and do not run Unity; parent serially generates and reviews persisted editable prefab/scene output after scripts compile.

Core agent owns `Assets/Scripts/Core/Worms.Core.asmdef` with `noEngineReferences: true`, namespace `Worms.Core`, and `Assets/Scripts/Worms.Runtime.asmdef` referencing Core. Runtime contains the existing global MonoBehaviour classes plus new Unity adapters. Editor code stays outside runtime compilation; test asmdefs explicitly reference the assemblies tested. Gameplay agent exclusively owns `Core/LevelSession.cs`; Core must not create a competing session implementation. Contracts must compile and be published before dependent agents start.

Shared API (field spelling and constructor ordering are contractual):

```csharp
namespace Worms.Core
{
    [Serializable] public sealed class LevelDefinition
    {
        public string Id, WorldId, SceneName, DisplayName;
        public int LegacyIndex;
        public float StarTimeSeconds;
    }
    public interface ILevelCatalog
    {
        IReadOnlyList<LevelDefinition> Levels { get; }
        LevelDefinition GetLevel(string id);
        LevelDefinition FindByScene(string sceneName);
        LevelDefinition GetNext(string id);
    }
    [Serializable] public sealed class SaveData
    {
        public int Version = 1;
        public List<LevelProgress> Levels = new List<LevelProgress>();
    }
    [Serializable] public sealed class LevelProgress
    {
        public string LevelId;
        public bool Completed, StarEarned;
        public float BestTimeSeconds;
    }
    public interface ISaveStorage
    {
        SaveData Load();
        void Save(SaveData data);
    }
    public enum SessionState { Playing, Completed, Dead }
}
```

`ILevelCatalog` missing lookup/end returns null; ordered `Levels` contains only published playable levels. `ISaveStorage.Load()` returns null for absent data and throws for invalid/unreadable data; SaveService catches expected storage/format exceptions and reports them. Storage owns serialization and file operations, Core owns validation and progression. Use explicit namespaces/imports and normal C# supported by the pinned editor.

| Type and owner | Required public API |
| --- | --- |
| `SaveService`, Core | Constructor `(ISaveStorage storage, ILevelCatalog catalog)`; read-only `SaveData Data`, `string LastError`; `bool TrySave()` |
| `ProgressionService`, Core | Constructor `(ILevelCatalog catalog, SaveService saves)`; `bool IsUnlocked(string id)`, `bool HasStar(string id)`, `string GetContinueLevelId()`, `CompletionResult RecordCompletion(string id, float elapsedSeconds)` |
| `CompletionResult`, Core | Read-only `string LevelId`, `float ElapsedSeconds`, `bool StarEarned`, `string NextLevelId`, `bool SaveSucceeded`; constructor `(string levelId, float elapsedSeconds, bool starEarned, string nextLevelId, bool saveSucceeded)` |
| `LevelSession`, Gameplay | Constructor `(LevelDefinition level, ProgressionService progression)`; read-only `SessionState State`, `string LevelId`, `float ElapsedSeconds`, `CompletionResult Completion`; `event Action<LevelSession> Changed`; `void Tick(float deltaSeconds)`, `bool TryComplete()`, `bool TryDie()` |
| `LevelCatalog : ScriptableObject`, Core runtime adapter | Implements `ILevelCatalog`; designer-editable serialized ordered entries; stable IDs `level-01`…`level-20`, world IDs `world-1` and `world-2` |
| `GameServices`, Core runtime adapter | `EnsureInitialized()` plus static read-only `Catalog` and `Progression`; initializes on demand for direct scene entry; explicit test reset/configuration seam |
| `LevelSessionHost : MonoBehaviour`, Gameplay runtime adapter | Static `Current`, `GetOrCreate()`; instance read-only `Session`; returns no gameplay session for noncatalog scenes; fresh instance per gameplay scene load |
| `SceneNavigator`, UI runtime adapter | Lazy `Instance`; `Continue()`, `OpenWorld(string worldId)`, `OpenLevel(string id)`, `Restart()`, `BackToWorld()`, `MainMenu()`, `Quit()`; owns scene loading and load-in-progress guard |

`GameServices` has no reference to session, navigation or UI. UI may obtain the host lazily after checking active scene in catalog. Reset statics when entering Play Mode with domain reload disabled. Session subscribes/presents state without holding scene objects. Do not introduce runtime/editor assembly cycles.

## 3. Three implementation lanes and ownership

| Lane | Owns | Must leave to another owner |
| --- | --- | --- |
| Core/persistence/catalog | Core contracts, both runtime asmdefs, SaveData/SaveService/ProgressionService; runtime JSON storage, catalog asset and generation/validation editor tool, GameServices, legacy GameController compatibility facade; EditMode persistence/catalog tests | LevelSession implementation, legacy gameplay callbacks, UI/navigation, scene edits |
| Gameplay/session/movement | Core LevelSession; runtime host, input/motor/attachment/oxygen objects; Player, Wheel, Conveyor, CountdownExplode, Explode, SaveGame, Finish, FinishZone, CameraFollow; typed camera/attachment/hazard adapters; gameplay EditMode and PlayMode tests | Catalog/storage/contracts, UI scripts/prefabs/scenes and SceneNavigator |
| UI/navigation | SceneNavigator; legacy LevelFinish, DisplayRestartText, LevelSelect, LevelSelectScript, StorySelect, StoryScript, ClickToContinue adapters; Canvas/safe-area components; editable prefabs and idempotent editor migration tool; UI tests | Gameplay state changes, persistence, shared contracts |

Oxygen data/death remains Gameplay-owned; UI consumes its read-only fraction/event and owns presentation only. Agree that small view API before either lane changes Oxygen. UI can suppress legacy OnGUI from its migration/tool or a presentation adapter; Gameplay performs final Oxygen OnGUI removal to avoid concurrent edits.

Parent/integration owns existing scene/prefab mutations and runs all editor tools serially after reviewing their output. Agents may author migration code and new prefab assets but must not simultaneously resave shared scenes/prefabs. Parent owns check runner changes, consolidated acceptance script, docs and final builds. New tests are separated by owner files; baseline characterization tests remain independently owned. Announce any shared file handoff explicitly. No concurrent Unity processes against this project.

## 4. Implementation phases

### Phase A — record the before state

Add characterization tests and an asset contract manifest before production changes. Back up/isolate the real persistent directory and restore it in test teardown, including PlayerPrefs FinishedTime; never erase a developer's progress. Prefer injected storage immediately when new APIs exist. Baseline binary-save tests may use generated trusted fixtures under an isolated test directory; no test requires arbitrary external binary input. Record legacy failure evidence and desktop screenshots at 720×1280 and a narrow viewport. Parent captures the baseline test report/commit before implementation begins.

### Phase B — publish contracts and persistence/catalog foundation

Publish the shared contract files and asmdefs first. Construct one reviewed catalog asset from the effective legacy scene metadata. Catalog is authoritative after migration: runtime save identity/threshold/route never falls back to mutable `currentLevel`, scene name arithmetic or Finish.nextLevel. Legacy fields remain for serialized compatibility and a migration audit. First level is initially available; each completion unlocks its ordered successor; earlier completions never relock levels; stars are monotonic; best time only improves. Completing final level yields null NextLevelId and Continue resolves a valid published level rather than Level21. Unknown IDs never initiate loads or write new progress.

Store version 1 JSON at a documented new path such as `progress-v1.json`. Use temporary write, flush/dispose, replace/backup strategy appropriate to platform; preserve last good data on write failure and expose error state. Validate version, null/duplicate records, known IDs, finite nonnegative times, bounded file size, and progress consistency. Never interpret a future schema as v1 or overwrite an unsupported save automatically. Missing data starts new progress; corrupted JSON is retained/quarantined with a visible recoverable status; failed save retains in-memory completion and exposes retry without generating another terminal transition.

**Explicit legacy policy:** retain `playerInfo.dat` untouched; detect and report that old progress is not imported. This refactor begins fresh v1 JSON progress. Remove BinaryFormatter usage from the game; do not deserialize old files automatically or claim backward compatibility. A future separate, explicitly requested safe offline conversion workflow can map old indices to stable IDs. Document reset policy in UI/help and migration notes. No implicit deletion, overwrite or unsafe converter is part of this scope.

`GameController` can forward Load/LoadArray/Save to the new services for preserved callers, with a bounded LegacyIndex mapping. It must not retain a second persistence implementation. Catalog validation checks unique IDs/scenes/legacy indices, complete enabled gameplay coverage, valid world routes, nonnegative finite thresholds and no nonexistent destinations. Exclude Level0/testGUI/sdfgsdfg from published progression unless separately authorized.

### Phase C — session and gameplay objects

One scene-scoped LevelSession owns elapsed gameplay time and terminal transition. While Playing, Tick accepts finite nonnegative delta; host pauses ticking during application pause. Completion freezes rounded legacy-compatible elapsed seconds, records progression once, then emits one Changed event. Death emits once without progression. First accepted terminal callback wins; repeated triggers, deadly collision plus oxygen exhaustion, and multiple colliders produce no extra saves, body parts or panels. UI never infers death from destroyed-player absence. SaveGame and Finish both forward to this same session; only the accepted transition runs effects. Remove FinishedTime reads/writes and ensure results receive Completion directly.

Keep Player as serialized scene adapter; extract PlayerInput (Space/touch and UI pointer exclusion), PlayerMotor (cached body and velocities), PlayerAttachment (wheel/conveyor state, holder and countdown interaction). Use typed Wheel/Conveyor/CountdownExplode/CameraRegion components and serialized holder/center references. A one-time editor migration locates old named children and writes references; runtime game rules do not search names. Preserve `Player.detatch()`, `CountdownExplode.count()/playerLeft()`, `Oxygen.addO2()`, `Explode.OnExplode()`, animation Attack/OnShoot methods and exact field types used by scenes. Cache components at initialization and guard optional presentation references.

Separate oxygen state from meter drawing: initial authored air/max/burn rate, decrement with supplied delta, refill to the legacy value 10 unless authored behaviour is deliberately revised, one depletion notification. Retain legacy depletion-frame timing until characterized. Attachment records the actual player instead of globally searching Player 1; handle wheel destruction and detach cleanup once. Camera regions reconfigure an existing CameraFollow; do not destroy/add a camera component per crossing. Preserve smoothing, axis-toggle sequence, background z and screen-ratio framing until screenshots/tests substantiate any change.

### Phase D — editable Canvas UI and central navigation

Build named editable Canvas prefabs: main/world menus, story panel, gameplay oxygen HUD, completion panel and death/retry panel. Use CanvasScaler, anchored layouts, safe-area root, adequate touch targets, EventSystem and designer-editable text/images/button bindings. Runtime presenters bind model state to preauthored views; avoid a procedural script that builds the entire UI hierarchy on every load. Prefab-generation editor tooling is acceptable when its serialized output is committed and editable.

Migration tool must be idempotent and scoped: update affected scenes/prefabs; preserve art, transforms, GUIDs and existing menu button UnityEvents; remove duplicate input/EventSystems and disable migrated OnGUI routes. Keep Selectlevel(string) forwarding legacy scene arguments through catalog/navigation. Preserve splash tap, story typewriter/full-reveal/second-advance, touch and keyboard jump, retry anywhere outside menu controls, Continue/Back/Main Menu/Quit. Replace frame-count story debounce with a documented time/release guard so a held touch cannot skip both stages; test as an intentional fix.

SceneNavigator maps world IDs to actual World1/World2 scenes; rejects locked/unknown destinations; coalesces repeated button presses; uses catalog next/continue routes; Back derives current world. Final completion offers world/menu navigation without loading absent content. Empty World3 is hidden/disabled with no clickable broken route. Keep quit editor-safe. Pointer presses used by menus must not also launch/retry gameplay.

### Phase E — folder cleanup and integration

Use Core/, Gameplay/, Persistence/, Catalog/, UI/, Navigation/ and Editor/ directories. Move existing scripts only after behaviour tests pass, with their original .meta files. Preserve script filename/class correspondence, global legacy classes, serialized field names/types and event methods; use explicit serialized migrations/FormerlySerializedAs only when necessary. Do not blanket namespace or rename old MonoBehaviours. Verify asset manifest before/after, missing scripts and UnityEvents, then run repository check, EditMode, PlayMode, Windows and Android ARM64 build in that order. Run editor migration twice and require the second run to produce no diff. Review scope to exclude ads integration, new content, editor upgrade and physics redesign.

## 5. Nonredundant automated test matrix

Write assertions for externally observable decisions and adapter behaviour, not private-method structure. Domain rule permutations belong in EditMode; PlayMode verifies actual Unity integration; built-player/device scripts exercise platform/UI/input. Every test creates isolated state and releases objects/subscriptions/files. Do not treat teleported finish probes as evidence a level is naturally traversable.

| ID | Before characterization | After acceptance and primary layer |
| --- | --- | --- |
| SAVE-01 | Missing binary file -> level1, 31 false slots | EditMode: missing JSON, round-trip all fields, stable-ID reorder; in-memory ISaveStorage, no real user path |
| SAVE-02 | Star remains true; lower replay does not reduce progression | EditMode Progression: first unlock, replay, monotonic stars, improved best, exact/under/over rounded threshold; disabled threshold0 |
| SAVE-03 | Record lack of corrupt-file recovery | EditMode SaveService fake failures + temporary-file JsonFileSaveStorage tests: malformed, empty, null, duplicate/unknown IDs, future version, nonfinite times, interrupted write/read-only failure, backup recovery and retry |
| SAVE-04 | Document .dat format only | Integration temp directory: legacy .dat unchanged byte-for-byte, fresh JSON created, reset notice surfaced; no BinaryFormatter API references in active source |
| CAT-01 | Export all effective scene finish metadata and broken routes | Editor integration: catalog uniqueness, all 20 playable scenes, build inclusion, world membership, valid route, no duplicate/missing exits; intentional content defects explicitly resolved or reported |
| SES-01 | Finish triggers separate save/destroy/results | EditMode LevelSession: Tick, rounding, complete twice/dead twice, both terminal orders, no ticking after terminal, save failure; one store call and one Changed terminal event |
| SES-02 | Trigger real legacy exit once and record result timing | PlayMode actual prefab/physics trigger: SaveGame and Finish share result, exactly one panel/save; repeated collider enter, hazard/depletion same frame, restart fresh session, no stale PlayerPrefs channel |
| MOV-01 | Initial down3, regular up10, forced up5, held input | PlayMode preserved characterization: actual Rigidbody velocities/directions and no jump while unattached; input source fake for reproducibility; physical keyboard/touch separately |
| MOV-02 | Wheel clockwise/counterclockwise, holder rotation and scale; same-wheel suppression | PlayMode fixture: wheel attach/detach/reattach distinct wheel, conveyor authored speed/direction, destroyed holder cleanup, countdown starts/continues/cancels player ownership correctly; renamed objects still work |
| CAM-01 | Record axis toggle, center smoothing and background position | PlayMode typed region crossings: same expected framing and no component accumulation; optional background safe; desktop screenshot comparison for aspect ratios |
| AIR-01 | Record burn, refill10, depletion-frame and bodypart output | EditMode oxygen rules plus PlayMode single depletion -> Dead -> one seven-part burst; repeat OnExplode guarded; actual Deadly trigger/collision and alien attack paths |
| UI-01 | Record every legacy screen/button route and clipping | PlayMode instantiate committed prefabs, bind states, invoke actual Button events: correct labels, locks/stars, result seconds, world routes, last-level handling, invalid routes blocked, rapid double press coalesced |
| UI-02 | First story touch reveals, later touch advances; held-touch bug recorded | PlayMode story presenter clock/input: reveal guard and one subsequent advance; all actual scene UnityEvents reference existing component/method |
| UI-03 | Rotated fixed layout screenshots | PlayMode layout bounds for supplied safe rect at 16:9, 19.5:9 and 4:3; built desktop + physical phone checks validate rendered readability and hit targets |
| ASSET-01 | GUID/serialized field/event manifest and scene/prefab missing-script scan | Repository/editor integration: every original GUID retained after moves, expected serialized values/required refs remain, animation events resolve, all 24 scenes start; migration twice leaves identical assets |

Existing every-scene smoke is retained once; do not duplicate one-second scene-start tests for each refactor class. Reuse baseline movement assertions after changes. For unit-only cases use fake ILevelCatalog/ISaveStorage and direct Tick, not PlayMode waits. Test save-file atomicity in a temporary folder, not with fake storage alone.

## 6. Built-player and device scripts

Parent creates `docs/PLAYTEST_SCRIPT.md` and a result sheet listing run ID, commit, editor/build platform, OS/device/model/resolution/safe area, scenario ID, steps, expected result, actual result, screenshots/logs and pass/fail/blocked. Run from isolated fresh save, then seeded/replayed progress; restore tester data after the run. Failed or unavailable physical-device cases remain explicitly blocked/unverified.

| Scenario | Reproducible action and required result |
| --- | --- |
| E2E-01 Fresh launch | Start built player -> tap splash once -> world menu -> Continue. Only published destinations appear; Level1 begins with full authored oxygen, correct framing and no stale results. |
| E2E-02 Story/input | Open story scene fixture if no published route uses it; tap once to reveal all, hold/release, tap again to advance once. Desktop mouse and phone touch both work. Menu touches never jump the player. |
| E2E-03 Traversal | On every Level1–20, play the level from entry using keyboard/touch without teleporting; attach each present wheel/conveyor, jump, cross all camera zones, use any refill, reach actual exit. Record route, natural completion, time/star, next destination. Missing exit or impossible route is a content failure, not a passing smoke test. |
| E2E-04 Death/retry | In representative levels exercise spike/Deadly contact, alien/projectile and oxygen exhaustion, plus breakable-wheel forced detach. One explosion/panel appears; Retry creates fresh player/time/oxygen; no new progression on death. Tap menu controls to verify no accidental retry. |
| E2E-05 Progress/replay | Complete Level1 below its threshold, replay slowly, complete Level2, quit/relaunch. Locks, star retention, best time and Continue survive. Back from Level11 returns World2; Level10 Continue goes Level11. |
| E2E-06 Boundaries | With an isolated fixture unlock final level, finish it, select Continue/Back/Main Menu. No Level21/empty world load; one coherent terminal panel and valid navigation. Rapid double taps do not stack panels or loads. |
| E2E-07 Save recovery | Seed malformed JSON, future-version JSON and a .dat-only directory in separate isolated runs. Show recoverable status; retain original files; no crash/unsafe binary read; save retry works when temporary write restriction is removed. |
| E2E-08 Layout | Desktop 720×1280, 1280×720, 800×600 and phone notched safe area: every menu/HUD/result fits, labels readable, hit targets work; no legacy 90-degree GUI double rendering. Test supported orientation(s), resize and safe-area change. Document final orientation policy explicitly. |
| E2E-09 Phone lifecycle | Install ARM64 IL2CPP APK on physical Android phone; test real held touch, multitouch, navigation, suspend/resume while Playing and result panel, force-stop/relaunch progress. No oxygen/time drain while suspended, no duplicate subscription, stable session after resume. Save persists across ordinary app upgrade. |
| E2E-10 Platform build | Launch Windows player from entry and directly inspect all route families; install/launch Android APK and inspect logcat for errors. iOS device run only with Mac/Xcode and signing available; otherwise mark unverified. |

Automated build checks prove compilation/package creation. Human playthrough proves natural reachability and movement feel; neither substitutes for the other. A diagnostic deterministic finish probe may cover every authored exit fast, but report it distinctly from E2E-03.

## 7. Completion criteria and handoff

Done means committed reviewable implementation, baseline and after results linked by scenario IDs, functional test suites passing, no lost GUID/UnityEvent/animation binding, reviewed catalog/content corrections, responsive committed UI prefabs, Windows/Android builds and available player runs complete. Report physical-device limitations honestly; no device attached is not a physical-device pass. Update README/MIGRATION for JSON legacy reset, test commands and changed UI/scene workflow. No release publication is implied.

Expose `EditMode` in `scripts/check-unity.ps1`, keep one editor process, save machine-readable test XML plus logs outside source. Parent verifies reports actually contain executed tests, not merely a zero editor exit. Review a concise failure list before approval; do not silently turn failing scenarios into ignored tests. Keep content issues, platform environment blockers and code regressions distinguishable in the final review.
