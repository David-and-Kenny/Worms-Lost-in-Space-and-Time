# Frame-rate investigation

Read-only investigation requested on November 6, 2026 while developing the level
validator. This document proposes a separate gameplay change; that refactor has
not been implemented by the validation utility.

## Finding

**There is evidence of frame-sensitive jump execution, not evidence that a 120 Hz
phone runs the entire game twice as fast.** Display refresh, rendered FPS and
physics frequency are different quantities.

- Wheel rotation uses `speed * Time.deltaTime`. A wheel at 90 degrees/second moves
  3 degrees/frame at 30 FPS, 1.5 at 60, or 0.75 at 120: approximately the same angle
  per simulated second. This follows Unity's [deltaTime pattern](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/time/deltatime).
- Player falling/launch velocities are 3/10 units per second, with forced detach
  at 5. [Rigidbody2D velocity is measured per second](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/rigidbody2d/linearvelocity).
- Session time, oxygen and countdowns use elapsed time. No simple global
  fixed-distance-per-rendered-frame speed bug was found in these paths.
- The authored physics step is 0.02 seconds: 50 physics steps per simulated second.

There is no shipping runtime `Application.targetFrameRate` policy. Unity documents
that Android/iOS with the default `-1` target render at **30 FPS independently of
native display refresh**; mobile ignores `vSyncCount`. A 120 Hz screen therefore
does not automatically run this game at 120 FPS. Confirm actual behavior on a
device rather than inferring it from the display specification.
[Unity frame-rate reference](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/application/targetframerate)

## Underlying timing problem

Physics resolves movement/contacts at 50 Hz, but wheel/collider movement,
attachment-following and jump velocity assignment currently run in render-time
`Update`. Unity can execute zero, one or several physics steps between rendered
frames. Consequently, physics sees different intermediate poses and jump commands
arrive at different phases of its schedule. [Unity FixedUpdate reference](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/monobehaviour/fixedupdate)

`PlayerAttachment.Follow` also writes the Transform of an active dynamic
Rigidbody2D. Attachment puts the player origin at a contact point and does not
explicitly clear residual velocity. Physics correction and transform-following
can compete. Exact penetration and its gameplay consequences still need direct
measurement; they are not established for every scene by code inspection alone.

Input adds quantization: a frame is about 33 ms at 30 FPS and 8 ms at 120 FPS.
Rounding the same intended delay to those different boundaries can alter a
precision launch angle even when average wheel speed remains unchanged.

Recorded Level 1 and Level 3 timing policies completed at 60 Hz and failed at
30/120 Hz. This establishes sensitivity of those policies, not that either level
has no successful route at other rates. A 60 Hz certificate does not establish
Android readiness when the shipping application may render at 30 FPS.

Level 7 also exposed invocation sensitivity at the same 60 Hz reference rate:
its controlled route passed automatic-physics verification in the isolated
checkout, but failed that verification during fresh certification in the main
checkout. The tool rejected it and the remaining search budget expired. Its route
is excluded from the maintained regression set. This supports investigating
physics phase and attachment ownership; it does not isolate a single cause or
establish that every normal run fails.

The validator established a separate reload-order issue and now explicitly runs
wheel rotation before default-priority player following. That removes one phase
ambiguity; it does not eliminate the Update/physics split.

## Level 5 measurement issue

The original diagnostics matched all 51 paired post-Update trajectory samples
(frames 0–150 at three-frame intervals) exactly in position, rotation and velocity.
The terminal death at frame 152 also matched. Only the raw `Attached` collision
callback at frame 121 differed: about 0.00229 units in X and 0.00213 in Y, exceeding
the observer's 0.001-unit tolerance. Samples at frames 123 and onward matched.

That is evidence of a transient contact observation, not a global-speed error.
Frames 121/122 after following were not present in that original trace, and the
anchor itself was not recorded. The validator should preserve raw contact data
for diagnostics and compare a consistently phased authoritative attachment
checkpoint, retaining strict launch/terminal tolerances.
The utility now implements that distinction: raw attachment contacts retain
diagnostic data, logical transitions retain their original frames, and stable
attachment poses are checked two updates later. Actual launch/terminal poses keep
their strict tolerances. Level 5 subsequently passed calibration and proceeded
to a bounded route search, rather than failing on that transient contact sample.

## Proposed gameplay fix

Implement and review this separately from the validator:

1. Measure wheel angle, launch pose/velocity, physics tick and consumed-input tick
   at fixed simulated times across 30/60/120 rendered FPS; preserve current force,
   contact and camera baselines.
2. Give a fixed-tick coordinator one explicit simulation order for wheel/hazard
   motion, attachment, jump commands and timers. Retain the 50 Hz physics rate
   initially; do not tie simulation frequency to display refresh.
3. Define ownership of the attached body. Evaluate kinematic attachment with
   appropriate collision/trigger handling and a transition to dynamic on launch;
   preserve hazards, finish callbacks, support contacts and same-wheel suppression.
   Do not mechanically replace dynamic Transform writes with `MovePosition`:
   Unity describes it as intended for kinematic bodies.
   [MovePosition](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/rigidbody2d/moveposition),
   [kinematic contacts](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/rigidbody2d/usefullkinematiccontacts)
4. Buffer observed held-state transitions/commands and consume each once at a
   defined simulation tick. Preserve held jump and menu pointer guards. Legacy
   frame-polled input cannot reconstruct the time of an unobserved physical event;
   true device-event timestamps need an input-backend decision and separate tests.
5. Interpolate sprite/camera presentation between simulation poses. Render-time
   presentation must not write the next authoritative physics state.
   [Rigidbody2D interpolation](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/rigidbody2d/interpolation)
6. Establish an explicit, device-tested mobile rendering policy, initially
   considering a 60 FPS preference and measured fallback behavior. A rendering cap
   improves consistency/latency but does not replace simulation ownership fixes.

## Acceptance

- Identical timestamped commands produce the same attachment/launch/terminal
  sequence at the same simulation ticks under different render schedules.
- Fixed-duration rotation and unobstructed flight cover equal angle/distance.
- Captured brief input survives the render-to-physics handoff without duplication;
  held input and pointer exclusion preserve their behavior.
- Attached bodies do not accumulate penetration corrections or residual drift.
- Hazards, projectiles, breakable surfaces, oxygen and finish callbacks remain
  correct, with one terminal result and one completion write.
- Existing characterization tests pass, or intended timing changes are measured
  and reviewed. Recertify routes; never merely replace certificate hashes.
- Android tests record actual rendered FPS and display refresh separately,
  including dropped frames and suspend/resume. No physical-device result is
  claimed by this investigation.
