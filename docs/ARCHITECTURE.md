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

Movement retains initial/jump/forced-detach speeds of 3/10/5, authored wheel angular
speeds and conveyor speed. A scene-owned `GameSimulation` orders gameplay at the
authored 50 Hz rate: session clock, wheels, moving hazards, player, countdowns,
oxygen and projectile emitters, then Unity runs normal physics. Components register
on enable and unregister on disable; dynamically created participants use the same
path. There is no global participant registry or alternate test simulation.

`Player.Update` samples the existing pointer-guarded input into `JumpInputBuffer`.
The next tick consumes an observed press once; held input still launches when
attached. A released airborne tap expires at the next tick. Pause/focus loss and
disable clear pending input. Tests/replay can supply `IPlayerTickCommands` at that
handoff. This does not invent timestamps for device events the legacy input system
has not observed.

Wheel attachment owns a kinematic Rigidbody2D pose, zero velocity and full kinematic
contacts. Launch restores the original flight body mode/contact option. Decisions
use Rigidbody position/rotation; interpolation smooths the displayed player and
the camera follows in LateUpdate. Attack animators evaluate in fixed time. Public
scene fields, original script GUIDs and animation-event methods remain compatible.
See [FIXED_TICK_GAMEPLAY.md](FIXED_TICK_GAMEPLAY.md) for characterization and limits.

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

## Level validation tooling

`Worms.LevelValidation` is a test-only assembly, excluded from shipping builds.
It searches normal input sequences against real scenes and certifies full routes
with repeatable fixed-tick inputs and ordinary automatic-physics replays. Search
rules/contracts are ordinary C# objects; scene inspection and playback are Unity
adapters. `Player.TraversalChanged` and `GameSimulation.Stepped` expose read-only
observations. Commands/events use simulation ticks; replay certificates are version
2. Old frame-based inputs are proposals that require fresh certification.
See [LEVEL_VALIDATION.md](LEVEL_VALIDATION.md) for the clock, proof boundaries and
command/artifact contract. This tooling never requires an AI service at runtime.
