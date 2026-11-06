# Gameplay characterization baseline

These PlayMode checks characterize stable runtime behavior of legacy gameplay components before their planned refactor. They use small runtime-created objects and locate production types across loaded assemblies because the test assembly does not reference the production assembly directly.

## Covered behavior

- `PlayerDetatchClearsAttachmentAndLaunchesAlongItsUpAxis`: `Player.detatch()` clears `playerAttached` and assigns a launch velocity of 5 units along the worm's local up axis.
- `PlayerStartsWithDownwardVelocityOfThree`: player startup sets its Rigidbody2D velocity to three units downward.
- `WheelRotatesAndMovingSpikesStayOnTheirConfiguredPath`: wheels rotate during play, and moving spikes interpolate along the configured start/end segment.
- `CameraChangeZoneReplacesFollowTargetAndAlternatesAxis`: entering a `CameraChange` trigger replaces the camera follow component, assigns the player/background/center references, and alternates from horizontal to vertical following on the first change.
- `ExplosionEmitsSevenBodyPartsAndOneRetryUi`: `Explode.OnExplode()` instantiates seven body part rigidbodies and creates the retry UI once.
- `OxygenDepletionExplodesPlayerAndCreatesRetryUi`: depleted oxygen routes through `Explode`, destroying the player and creating the same retry UI and seven parts.

`SceneSmokeTests` separately covers that build scenes load. This baseline avoids save-system callbacks because they write persistent state. The player jump branch is input-driven through legacy `Input`; the deterministic player checks characterize startup and the public forced-detach method. Explosion and oxygen checks use small procedural body part fixtures.

No Unity editor or test runner was launched while preparing this baseline. Run `GameplayBaselineTests` and the existing scene smoke test against the unmodified scripts before refactoring, then retain these characterization expectations after refactoring.

## Recorded result

On November 6, 2026, the pinned Unity 6000.6.8f1 runner passed all seven tests: six focused characterization tests and the existing smoke test iterating the 24 enabled scenes. Original gameplay code had not been modified. Reports: ignored TestResults/Baseline-PlayMode.xml and Logs/Baseline-PlayMode.log. The oxygen fixture includes an actual Player component; camera setup accounts for Unity's deferred component destruction.

The after-refactor camera fixture may add typed CameraRegion metadata in place of the original name-based lookup. This changes fixture wiring only; target, center, axis-toggle and framing expectations stay the same. Save-system and player-input cases become safely injectable tests in the new suites rather than writing to a developer's legacy save.

