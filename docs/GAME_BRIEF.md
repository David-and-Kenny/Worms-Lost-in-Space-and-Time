# Game brief

Working title: **Worms — Lost in Space and Time**. Contributor brief, November 6,
2026. The final name is undecided. This records the agreed direction and current
prototype; it does not authorize unrelated features or release configuration.

## Player experience

A mobile precision platformer about guiding a worm between rotating machinery
and hazards in space. The player observes a platform's rotation, chooses when to
jump, and tries to reach the exit. Rotation determines launch direction. Timing,
route understanding and learning from mistakes drive improvement.
Target players enjoy learning precision challenges. This describes play preference,
not an age-rating or child-audience decision; those remain in the privacy backlog.

Levels should be short, difficult and worth retrying. Frequent failure is expected,
as in Super Meat Boy. A useful failure teaches the player something; a large
attempt count alone does not establish difficulty. Favor readable hazards,
predictable movement, clear goals and a quick return to another attempt.

## Core loop and controls

Enter a level → observe → jump or hold through attachments → avoid hazards →
fail and retry, or finish and progress. Touch and keyboard Space are held signals;
holding can launch automatically on a subsequent attachment. Menus must exclude
gameplay input. Do not add a new gesture or change this behavior incidentally.

The current game uses a shared 50 Hz gameplay tick, versioned local JSON progress,
two worlds and twenty catalog levels. Destructible wheels, conveyors and moving
hazards extend the same jumping mechanic. Timed stars exist only where authored;
do not invent challenge thresholds from solver search effort.
The retention hypothesis is that visible improvement and quick retries encourage
voluntary return play; BL-005 still needs representative playtests to validate it.

## Development priorities

1. Keep levels viable, controls dependable and failures understandable.
2. Preserve movement feel, character identity and existing asset GUIDs.
3. Use the established cartoon steampunk space art as the current visual reference.
4. Keep code readable and testable, scenes editable, and changes reviewable in Git.
5. Use the reusable validator for physical route evidence without AI tokens.

Recorded successful routes demonstrate viability, not optimal times or calibrated
human difficulty. Verify held-input policies separately from exact jump ticks.
Phone input latency, readability and lifecycle behavior still require devices.

## Boundaries and open decisions

Android is the available development target; iOS acceptance needs Apple tooling
and hardware. The final name, audience/skill calibration, duration bands, complete
star targets and additional worlds remain open product decisions. A working
design reference is the current short-route catalog, not a mandatory time limit.

New mechanics, online services, procedural generation and monetization changes
need their own scoped work. Existing ads/purchase policy and release gates remain
in [MONETIZATION.md](MONETIZATION.md). This brief does not enable production ads.

Use [LEVEL_DESIGN_CHECKLIST.md](LEVEL_DESIGN_CHECKLIST.md) for each level and
[BACKLOG.md](../BACKLOG.md) for remaining decisions and acceptance work.
