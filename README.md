# Worms — Lost in Space and Time

Working title for a Unity mobile game prototype. Jump between rotating wheels,
avoid hazards, and reach the exit before oxygen runs out.

## Development status

The project is upgraded from **Unity 5.2.0f3** to **Unity 6000.6.8f1 (Unity 6.6)**,
the latest supported release verified on November 6, 2026. All 24 enabled scenes
pass startup smoke checks; Windows and Android ARM64 development builds succeed,
and the Windows player runs. See
[migration notes](docs/MIGRATION.md) for verification details and remaining mobile
and gameplay validation. Use the exact version in `ProjectSettings/ProjectVersion.txt`.

## Get started

1. Install [Git](https://git-scm.com/downloads), [Git LFS](https://git-lfs.com/),
   [Node.js 24 LTS](https://nodejs.org/), and [Unity Hub](https://unity.com/download).
2. Clone the repository:

   ```powershell
   git clone https://github.com/David-and-Kenny/Worms-Lost-in-Space-and-Time.git
   cd Worms-Lost-in-Space-and-Time
   git lfs install --local
   git lfs pull
   ```

3. Sign in to Unity Hub and confirm an active license in **Settings → Licenses**.
   A license is required even for batch-mode checks.
4. Install **6000.6.8f1** from the
   [official release page](https://unity.com/releases/editor/whats-new/6000.6.8f1).
   Select **Android Build Support**, **Android SDK & NDK Tools**, and **OpenJDK**
   for Android development. Everyone should use the same exact editor patch.
5. In Hub choose **Projects → Add → Add project from disk** and select
   **`Worms Lost in Space and Time(3.0)`**, which contains `Assets` and
   `ProjectSettings`. The Git repository root is one directory above it.
6. Open using the pinned editor. First import can take several minutes. Keep generated
   `Packages/packages-lock.json` and migrated project settings in version control.
7. Open `Assets/Scenes/World1/Splash.unity` for the entry flow, or
   `Assets/Scenes/World1/Level1.unity` for a direct gameplay check. Press Play.
   **Space** jumps away from an attached wheel; touch does the same on mobile.
   Click/tap advances splash/story screens. The original UI draws rotated controls;
   check both portrait and landscape Game view sizes. A **720 × 1280** portrait
   view gives the legacy fixed-size menus enough space; narrow windows can clip them.

Close Unity before switching branches or running batch checks against the same
project. Never reopen migrated assets with an older editor.

## Checks and a local development build

From the repository root:

```powershell
node scripts/validate-repository.mjs
$editor = 'C:\Program Files\Unity\Hub\Editor\6000.6.8f1\Editor\Unity.com'
./scripts/setup-git.ps1 -UnityEditor $editor
./scripts/check-unity.ps1 -UnityEditor $editor -Check Scenes
./scripts/check-unity.ps1 -UnityEditor $editor -Check PlayMode
./scripts/check-unity.ps1 -UnityEditor $editor -Check BuildWindows
./scripts/check-unity.ps1 -UnityEditor $editor -Check BuildAndroid
```

The repository check requires no Unity license. It verifies `.meta` pairing,
unique GUIDs, merge markers, build-scene paths, text serialization, and exclusion
of generated output. GitHub runs it on pull requests and pushes to `master`.
The Unity scene check loads every enabled build scene and prefab and checks for
missing scripts. Play Mode checks start every enabled scene for one second and
fail on unexpected errors; results are saved under `TestResults/`.
The Windows command builds `Worms Lost in Space and Time(3.0)/Builds/Windows/Worms.exe`.
The Android command selects IL2CPP/ARM64 and builds a development APK at
`Worms Lost in Space and Time(3.0)/Builds/Android/Worms.apk` using debug signing.
Launch the desktop player at a useful portrait size:

```powershell
& './Worms Lost in Space and Time(3.0)/Builds/Windows/Worms.exe' -screen-fullscreen 0 -screen-width 720 -screen-height 1280
```

Logs go to the ignored root `Logs` folder. These checks do not replace playing
the game or testing on a phone. Licensed Unity CI can be added after migration;
the current GitHub check does not compile or build Unity.

For macOS/Linux, use the equivalent editor executable and command-line options:

```sh
"/path/to/Unity" -batchmode -nographics -quit -accept-apiupdate \
  -projectPath "Worms Lost in Space and Time(3.0)" \
  -executeMethod DevelopmentChecks.ValidateScenes -logFile unity-scenes.log
```

## Mobile setup

For Android, use **File → Build Profiles → Android**, switch platform, and use
the SDK/NDK/JDK installed with the pinned editor. Use a development build and a
USB-debugging-enabled device first. Recheck minimum/target API, ARM64/IL2CPP,
orientation, safe areas, input, and permissions before release; the original
Unity 5 Android settings are historical and are not a store-ready configuration.
Keep signing keys and passwords outside Git. Build output belongs in ignored `Builds/`.

iOS requires the matching iOS module and a Mac with Xcode for final compilation,
signing, and device testing. Windows alone cannot produce a signed iOS app.

Ads are deliberately inactive during migration. The unsupported 2015 SDK and
native libraries are preserved in [Legacy](Legacy/README.md), outside `Assets`.
The scene-attached script keeps its GUID. A maintained SDK and consent flow
must be integrated separately before enabling ads.

## Working together

See [CONTRIBUTING.md](CONTRIBUTING.md) for branches, reviews, scene editing,
Smart Merge, large assets, and check-in expectations.

Commit `Assets/` **with all `.meta` files**, `Packages/` (including the lockfile),
and `ProjectSettings/`. Never commit `Library/`, `Temp/`, `Logs/`, `UserSettings/`,
IDE output, builds, or credentials. The old tracked cache and APK have been
removed from the index; historical copies remain recoverable in Git history.

## References

- [Unity upgrade guidance](https://docs.unity.com/en-us/engine/6000.7/manual/upgrade-guides/upgrade-project)
- [Unity version-control and serialization guidance](https://learn.unity.com/tutorial/unity-tips)
- [Unity Smart Merge](https://docs.unity.com/en-us/engine/6000.6/manual/get-started/project-configuration/version-control/smart-merge)
- [GitHub protected branches](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/about-protected-branches)

No new license is assigned by this setup work. Confirm ownership and third-party
asset licenses before redistributing the project or releasing a build.
