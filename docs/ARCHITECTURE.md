# Architecture and authoring

The game uses ordinary C# models for decisions and small Unity adapters for
physics, input, scene lifetime and rendering. Legacy MonoBehaviour class names,
public serialized fields and callback methods remain compatibility boundaries;
they are not alternate implementations of saving, navigation or session rules.

## Dependencies

```text
Worms.Core                         No Unity references
  LevelDefinition / ILevelCatalog
  SaveData / ISaveStorage / SaveService
  ProgressionService / LevelSession
        ↑
Worms.Runtime                      Unity adapters
  Configuration/LevelCatalog       Editable ScriptableObject
  Persistence/JsonFileSaveStorage  File-format and filesystem boundary
  Gameplay/                       Input, Rigidbody, attachment, hazards, clock
  Navigation/SceneNavigator        The scene-loading boundary
  UI/                             Serialized views and state presenters
        ↑
Editor tools / EditMode tests / PlayMode tests
```

`GameServices` is the composition root. It creates catalog/storage/progression on
demand, including direct editor entry to a level. Domain objects receive their
dependencies explicitly. Tests configure an in-memory catalog/storage or temporary
file directory and reset services afterward. Scene-specific objects must not be
retained by the composition root.

## One result per run

`LevelSession` starts Playing and accepts either Completed or Dead once. The first
accepted terminal transition freezes the run. Both old finish callbacks reach the
same session; repeated collisions cannot save twice. Death never awards progress.
The Unity host supplies elapsed time and pause state. Presenters observe the
result; they do not infer it from a destroyed GameObject or a PlayerPrefs key.

Movement retains the characterized initial speed, forced-detach speed and wheel
timing. Input, cached Rigidbody access and attachment behavior are separated so
they can be tested independently. Typed wheel/conveyor/camera references replace
object-name rules. Changes to physics timing or movement feel need their own
characterization evidence and PR.

## Levels and progression

The committed `Resources/Configuration/LevelCatalog` asset defines stable IDs,
scene names, world membership, display names, legacy indices and collectible target
times. Scene order and object names are not save IDs. Menus, Continue, results and
world navigation use this catalog. A zero target means no timed collectible
challenge. The final level has no successor; no route constructs a Level21 name.

`docs/CATALOG_METADATA.csv`, `.json`, and `CATALOG_REVIEW.md` capture effective
legacy scene values and content defects when the catalog migration tool runs.
The migration corrects code routes; it does not invent missing exits or challenge
times. Once the migration is accepted, edit the catalog deliberately for new
content rather than regenerating it from obsolete compatibility fields.

## Saves

Progress uses version 1 JSON at `Application.persistentDataPath/progress-v1.json`.
Storage writes a temporary file and replaces the previous save with a backup;
read/write failures are surfaced to UI. Future schemas and unreadable data must
not be silently replaced. Unit tests cover progression rules, and temporary-file
tests cover serialization, recovery and filesystem failure paths.

The old `playerInfo.dat` is retained untouched and is not deserialized. This
version starts fresh JSON progress and explains that policy to players with old
data. An old-save conversion would be a separately reviewed migration.

## UI and scene editing

The UI uses committed, editable Canvas prefabs, named serialized references,
anchored layouts and safe-area roots. Runtime presenters bind data and button
actions; they do not construct complete UI hierarchies. Editor builders create
and migrate prefab/scene data and must be idempotent. Preserve existing world
button event bindings and original script `.meta` GUIDs.

Only `SceneNavigator` requests scene loads. It validates published destinations,
unlocks and repeated requests. Retry reloads a known current level; a menu cannot
open an unknown or locked level. Back derives the world from catalog metadata.

For new behavior, start with the scenario/test layer in `TESTING.md`, implement
the smallest cohesive object, then wire its Unity adapter. Keep migrations, game
behavior changes, art/layout work and unrelated cleanup distinguishable in diffs.

## Monetization

`AdBreakPolicy`, `AdBreakController`, `ForegroundClock`, and `RemoveAdsEntitlement` are ordinary Core
objects. Real substitution boundaries cover interstitial presentation, entitlement
storage, purchasing, and deferred navigation. The persistent `MonetizationHost`
owns platform SDK lifetimes and a foreground clock; it does not own progression or
hold scene UI references. `SceneNavigator` validates/coalesces the destination
before its optional transition gate presents an eligible ad. Test constructors
omit the gate or inject a fake. Batch-mode runs never initialize monetization.

Main/world menu prefabs contain an editable settings panel. The old ad component
is inert. Developer ads and production monetization default OFF. See
[MONETIZATION.md](MONETIZATION.md) for timing, child-safe defaults, purchase-cache
boundaries, account handoff and release gates. Gameplay progress/save format is
unchanged; purchase ownership has a separate recoverable cache.

## Level validation tooling

`Worms.LevelValidation` is a test-only assembly, excluded from shipping builds.
It searches normal input sequences against real scenes and certifies full routes
with repeatable controlled trials plus ordinary automatic-physics replays. Search
rules/contracts are ordinary C# objects; scene inspection and playback are Unity
adapters. `Player.TraversalChanged` exposes read-only observations, and moving
spikes accept an optional elapsed-time source without changing human-play defaults.
See [LEVEL_VALIDATION.md](LEVEL_VALIDATION.md) for the clock, proof boundaries and
command/artifact contract. This tooling never requires an AI service at runtime.
