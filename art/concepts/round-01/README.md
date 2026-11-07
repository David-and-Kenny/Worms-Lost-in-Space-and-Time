# Round 01: cartoon steampunk-space asset concepts

Status: owner likes this direction; production integration is pending. Generated using the built-in Imagegen
tool. These are concept images outside Unity; no game assets, character artwork,
scenes, physics, or bindings have been changed.

## Brief

Explore two level assets first: the cog platform and a straight pipe. Preserve
the existing character and use its bold black linework, simplified silhouettes,
and broad cartoon shading as the style anchor. Use the collected references for
steampunk cogs and space pipework. No pixel art or realistic rendering.

The warm brass/copper palette, dark recesses, and small teal accents are proposed
for this round, not approved as the final game-wide palette. Title screens,
buttons, and backgrounds remain future work after style review.

## Outputs

- [Cog platform](cog-platform-v01.png), compared with
  `Assets/Artwork/sprite-sheets/cog1.png`.
- [Pressure pipe](pressure-pipe-v01.png), compared with
  `Assets/Artwork/sprite-sheets/pipe-med.png`.
- [Original versus redesign comparison](original-vs-redesign.png): original on
  the left, concept on the right. Assets fitted to comparable widths; original
  aspect ratios are preserved. This is an art comparison, not an in-game scale test.

Both source assets live under `Worms Lost in Space and Time(3.0)`.
All eight steampunk-space reference images informed the round: three gear
references and the cog workshop informed the cog; both spacecraft, the cabin,
and the boiler room informed the pipe. The finished character is a direct style
reference for both. The sketch character was inspected as supplementary context.
Each generation used five direct image inputs, listed in the prompt record.

## Review and production follow-up

Evaluate line weight, simplified metal shading, palette, amount of detail, and
whether these feel like the same world as the unchanged character.

The pipe concept is proportionally thicker than the original. The gear has
illustrated geometry rather than verified radial symmetry. Before integration,
adapt proportions and pivots to the existing assets, verify silhouette and
rotation, evaluate at actual phone size, and preserve existing GUIDs and
bindings. These concepts have not been tested in Unity or on a device.

Owner feedback: "I like this direction." The next requested experiment is a
level background in the same cartoon steampunk-space style; see
[round 02](../round-02/README.md). Approval of the direction does not establish
these images as final production assets.

## Exact generation prompts

### Cog platform

Reference inputs in order:

1. `art/inspirations/characters/main-character-large.png`
2. `art/inspirations/steampunk-space/brass-ring-gear.jpg`
3. `art/inspirations/steampunk-space/rusted-interlocking-cogs.jpg`
4. `art/inspirations/steampunk-space/layered-clockwork-mechanism.jpg`
5. `art/inspirations/steampunk-space/cog-conveyor-workshop.jpg`

```text
Use case: stylized-concept. Create one original review-stage 2D mobile game asset redesign for Worms Lost in Space and Time, about jumping between rotating cogs in space. All images are reference inputs, not edit targets. Image 1 is the PRIMARY STYLE ANCHOR: the unchanged main character. Match its bold hand-inked black contours, chunky simplified shapes, broad flat/cel-shaded color areas, restrained graphic highlights, and approachable anime/cartoon quality. Do not draw or redesign the character. Translate the other reference images' steampunk machinery into that same simple hand-drawn 2D language. No pixel art, photorealism, 3D render, painterly realism, noisy weathering, tiny ornamental detail, text, lettering, logos or watermark. Prioritize a readable silhouette at phone size. Isolate exactly one asset on genuinely transparent alpha background, no baked checkerboard, no scene backdrop, no external cast shadow. Orthographic gameplay view, no perspective camera. Keep the entire object visible with generous clear padding. Palette proposal: warm muted brass, burnt copper, charcoal recesses, small off-white glints, restrained teal space-engineering accents. No more than two shadow tones per metal. Not an integrated production asset. Images 2-4 provide gear teeth, open spokes, nested hubs, concentric mechanical layers, and aged brass construction. Image 5 provides the gear workshop industrial mood. Do not reproduce the reference backgrounds or realistic textures. SUBJECT: one large circular cog platform seen perfectly face-on. Radially centered round silhouette, 24 broad evenly spaced blunt gear teeth, six sturdy open spokes, simple layered bolted central axle hub. Brass-gold front face, burnt-copper inner collar, dark charcoal recesses, modest off-white glints like the existing character's helmet; one subtle cool teal axle inset to suggest space machinery. Strong near-black outlines, no more than two shadow tones per metal. Show substantial construction with a very narrow graphic bevel only; keep tooth circle geometrically regular and centered for future rotation. A few large bolts and clean plates rather than many miniature mechanisms. Open spaces between spokes are transparent. Entire wheel visible and unclipped.
```

### Pressure pipe

Reference inputs in order:

1. `art/inspirations/characters/main-character-large.png`
2. `art/inspirations/steampunk-space/spherical-steampunk-space-station.jpg`
3. `art/inspirations/steampunk-space/brass-pipe-spacecraft.jpg`
4. `art/inspirations/steampunk-space/pipe-lined-space-cabin.jpg`
5. `art/inspirations/steampunk-space/copper-pipe-boiler-room.jpg`

```text
Use case: stylized-concept. Create one original review-stage 2D mobile game asset redesign for Worms Lost in Space and Time, about jumping between rotating cogs in space. All images are reference inputs, not edit targets. Image 1 is the PRIMARY STYLE ANCHOR: the unchanged main character. Match its bold hand-inked black contours, chunky simplified shapes, broad flat/cel-shaded color areas, restrained graphic highlights, and approachable anime/cartoon quality. Do not draw or redesign the character. Translate the other reference images' steampunk machinery into that same simple hand-drawn 2D language. No pixel art, photorealism, 3D render, painterly realism, noisy weathering, tiny ornamental detail, text, lettering, logos or watermark. Prioritize a readable silhouette at phone size. Isolate exactly one asset on genuinely transparent alpha background, no baked checkerboard, no scene backdrop, no external cast shadow. Orthographic gameplay view, no perspective camera. Keep the entire object visible with generous clear padding. Palette proposal: warm muted brass, burnt copper, charcoal recesses, small off-white glints, restrained teal space-engineering accents. No more than two shadow tones per metal. Not an integrated production asset. Image 2 gives spherical space-station panels and teal portholes; Image 3 gives pipe-like brass spacecraft construction; Image 4 gives round hatches and connected cabin service pipes; Image 5 gives copper elbows, flanges, and pressure hardware. Simplify all these motifs into the character's cartoon rendering. SUBJECT: one horizontal straight pressure-pipe level asset, wide landscape composition, complete unbroken readable silhouette. Rounded copper main tube, two broad brass bolted end collars, one simple dark gunmetal middle reinforcement band. A small inset circular teal inspection window near the middle evokes space-station portholes; a short parallel narrow service line attached close to the main tube suggests space pipework without busy silhouette. Use bold black contours and broad stepped cel shading rather than shiny gradients. Burnt-orange/copper tube, warm muted gold fittings, charcoal shadows, small restrained off-white graphic highlights. Only a few oversized readable fasteners. Tube seen straight from the side, ends aligned horizontally, no dramatic perspective or disconnected pieces. Keep the top main pipe edge clear and uncluttered; no big valve or gauge projecting upward. Entire object fits comfortably on canvas with padding.
```
