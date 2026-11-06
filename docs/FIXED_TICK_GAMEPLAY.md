# Fixed-tick gameplay change

Baseline: `d5520a8`, Unity 6000.6.8f1. All 39 unchanged PlayMode tests passed in
the isolated checkout before production edits. XML/logs are retained as
`TestResults/Before-FixedTick-PlayMode.xml` and `Logs/Before-FixedTick-PlayMode.log`.
The earlier frame-rate investigation supplies the before-profile evidence.

## Implementation and acceptance

1. Give one scene-owned coordinator a clear fixed-tick order: clock, surfaces,
   moving hazards, player, breakable countdowns, oxygen and projectile emitters.
   Leave Unity in charge of normal physics. Keep the authored 0.02-second step.
2. Sample human input in Update, retain a brief observed press through the next
   physics tick, and preserve held-jump and pointer guards. Expose a tick-command
   boundary for reproducible tests; never pretend frame-polled input knows the
   timestamp of an event it has not observed.
3. Own attached-wheel pose with a kinematic Rigidbody2D and full kinematic contacts;
   restore the original body mode on launch. Preserve start/jump/forced-detach
   speeds, wheel angular speeds, conveyor speed and same-wheel suppression.
4. Interpolate the player presentation and follow it in LateUpdate. Keep collision
   and launch decisions on the physics pose, independent of that interpolation.
5. Move gameplay timers and attack animation evaluation onto fixed time. Preserve
   scene fields, script GUIDs and animation-event entry points.
6. Update the validator to schedule/observe simulation ticks and use the same
   runtime tick path for controlled and automatic physics. Old certificates are
   stale; old inputs may only propose freshly verified routes.
7. Test pulse/held input and pause clearing, attachment/contact/forced detach,
   rotation and launch velocities, flight and timers across 30/60/120 FPS plus
   uneven render schedules. Run the existing suites and builds. Revalidate all
   published levels and retain honest inconclusive/content-failure results.

No level geometry, force tuning, new input package or mobile rendering cap is
part of this change. Device movement feel and suspend/resume still require a real
device acceptance run. Record measured outcomes here after verification.

## Design boundaries

The coordinator is a small typed list of existing components, with an explicit
order in one method. It is not a generic simulation framework. Components keep
their authored references and expose short `Advance` methods. Unity still owns
collision solving and its usual fixed loop. The old validator-only physics clock
is removed, so tests and human play execute the same movement implementation.

Initial passive calibration ends at the first attachment plus two fixed ticks,
including an authoritative checkpoint. This avoids waiting indefinitely on a
stationary player without weakening full-route certification: candidates still
require fresh complete repetitions and attachment-relative replay. Explicit old
route proposals are calibrated in full and never trusted by their old hashes.

Observed input can wait up to the next 20 ms tick. Player interpolation also
introduces its usual one-tick presentation lag. Physical events still arrive
through frame-polled legacy input, so lower rendering rates can add input latency.
Tests establish equal simulation outcomes for equal observed tick commands; they
do not establish identical physical-tap latency on every phone. Numerical movement
parameters stay fixed, but launch/contact timing is intentionally quantized to
20 ms and existing routes must be recertified. A newly found faster route is not
evidence that the game's movement speed increased.

Unity references read for the legacy Rigidbody2D implementation:
[fixed updates](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/monobehaviour/fixedupdate),
[full kinematic contacts](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/rigidbody2d/usefullkinematiccontacts),
[MovePosition ownership](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/rigidbody2d/moveposition).
