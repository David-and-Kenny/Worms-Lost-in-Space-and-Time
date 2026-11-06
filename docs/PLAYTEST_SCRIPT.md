# Built-player and device acceptance script

This script covers behavior that headless tests cannot establish. Record commit,
build, tester, device/OS, resolution, safe area, and actual result for each case.
Use PASS, FAIL, or BLOCKED; unavailable hardware is BLOCKED, never PASS. Save logs
and screenshots in the ignored `TestResults/` directory, and summarize material
failures in the PR. Use a disposable OS/test profile for fresh-save tests; do not
delete a developer's progress to prepare a fixture.

## Start and navigation

1. **E2E-01 — fresh launch:** launch the Windows player or install the Android
   development APK. Tap the splash once. Confirm two published worlds, no empty
   third destination, Continue starts Level 1, and only the first level is unlocked.
2. **E2E-02 — story:** use the story fixture if it has no published menu route.
   Confirm text reveals progressively; first press reveals all; holding that press
   does not advance. Release and press again to advance exactly once. Verify mouse
   and touch separately.
3. **E2E-05 — persisted progression:** complete Level 1 under its authored target,
   replay slowly, complete Level 2, and quit/relaunch. Confirm unlocks, earned stars,
   best time and Continue persist. Death must never unlock a level.
4. **E2E-06 — route boundaries:** test Level 10 → Level 11, Back from World 2
   gameplay, final completion, Main Menu, and rapid repeated button presses. No
   absent Level 21, incorrect World 1 return, duplicate panel, or stacked load.

## Natural traversal

**E2E-03:** play every level from entry to its actual exit without teleporting or
calling a completion test API. For each, record attachments, conveyors, breakable
wheels, camera transitions, hazards, oxygen/refills, collectible result and next
destination. A headless startup pass does not prove that a level is solvable.

| Level | Result | Notes/evidence |
| --- | --- | --- |
| 1 | NOT RUN | |
| 2 | NOT RUN | |
| 3 | NOT RUN | |
| 4 | NOT RUN | |
| 5 | NOT RUN | |
| 6 | NOT RUN | |
| 7 | NOT RUN | |
| 8 | NOT RUN | |
| 9 | NOT RUN | |
| 10 | NOT RUN | |
| 11 | NOT RUN | |
| 12 | NOT RUN | |
| 13 | NOT RUN | |
| 14 | NOT RUN | |
| 15 | NOT RUN | |
| 16 | NOT RUN | |
| 17 | NOT RUN | |
| 18 | NOT RUN | |
| 19 | NOT RUN | |
| 20 | BLOCKED | Catalog audit confirmed no authored Finish trigger; see CATALOG_REVIEW.md. |

A zero collectible threshold means that timed challenge is disabled. Do not count
it as a failed earned-star test or invent a target time during testing.

## Death and restart

**E2E-04:** exercise a deadly collision, a trigger hazard, alien melee, projectile,
oxygen exhaustion, and forced detachment from a breakable wheel. Confirm exactly
one explosion and retry panel. Retry resets player, timer, oxygen and terminal
state. Buttons must not also trigger background retry or a gameplay jump. Compare
wheel attachment and launch feel to the recorded baseline.

## Persistence failures

**E2E-07:** in separate disposable profiles, test absent JSON, malformed JSON, a
future schema, a legacy `.dat` only, and a temporarily unwritable save location.
Confirm a readable status, intact original files, no binary deserialization, and
no crash. After restoring a writable location, retry persistence without completing
the level twice. Legacy `.dat` progress starts fresh JSON progression by design;
the old file must remain byte-for-byte intact.

## Rendering and real input

**E2E-08:** check desktop 720×1280, 1280×720, and 800×600 plus a notched phone.
All text, buttons, oxygen and result UI must fit within the safe area; no 90-degree
legacy overlay should render. Test supported orientation changes, resizing, and
large/small aspect ratios. Check actual hit targets, not only screenshots.

**E2E-09:** on a physical Android phone, test held touch, multitouch, UI pointer
exclusion, suspend/resume while playing and while showing a result, and force-stop
then relaunch. Time/oxygen must not advance during application pause, and ordinary
application upgrades must preserve JSON progress.

**E2E-10:** launch the actual Windows and Android builds, inspect logs/logcat,
and repeat the main route/death/progression checks. iOS requires macOS/Xcode,
signing and an actual device; record BLOCKED when unavailable.
