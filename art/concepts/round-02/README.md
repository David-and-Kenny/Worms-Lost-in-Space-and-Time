# Round 02: cartoon steampunk-space level background

Status: background experiment awaiting owner review. Generated with the built-in
Imagegen tool. The owner liked the direction of round 01 and requested a level
background in the same style; this is not approval to replace production art.

## Files and baseline

- Latest: [background V04](level-background-v04.png), returning closely to V01's
  broad space-window composition with duller metal tones and no cogs inside the
  window. See [V04 notes and prompt](level-background-v04.md).
- Previous: [background V03](level-background-v03.png) and
  [V03 notes](level-background-v03.md), the rustic frame with a smaller window.
- Previous: [background V02](level-background-v02.png) and
  [V02 notes](level-background-v02.md), the simpler wall experiment.
- [Background concept](level-background-v01.png)
- [Original versus redesign](original-vs-redesign.png): original on the left,
  redesign on the right, fitted into equal-size panels without cropping or
  stretching.
- Original: `Worms Lost in Space and Time(3.0)/Assets/Artwork/level_background.jpg`.
  The existing `Assets/Prefab/Background.prefab` SpriteRenderer references its GUID,
  `49d9fee143aad464ebaf198868b08777`.

The concept stays outside Unity. Existing backgrounds, character artwork,
prefabs, metadata, gameplay, and bindings are unchanged.

## Art direction

Carry the round-01 black cartoon outlines, broad shading, chunky mechanical
shapes, brass/copper construction, and teal accents into an environment.
Frame a quieter space view with cogs, pipe bends, reinforced hull plates, and
portholes. Distant gear silhouettes connect the new composition to the original
blue background. A small planet and sparse stars establish the space setting.

Keep central detail subdued for gameplay overlays. The edge machinery is
relatively bright and will need an in-game contrast check against actual player
and platform positions before integration. This is a single flattened background,
not separated parallax layers or a seamless scrolling tile.

## Review notes

Round 01 feedback: "I like this direction." The character remains the style
anchor and is not redesigned. The owner likes the V01 window into space but says
the background competes with playable cogs, requesting a smaller window and no
random cogs visible through it. V02 explored removing decorative machinery.
The owner then requested more of V01's machinery, with background cogs colored
differently from playable cogs and duller, more rustic pipes and gears. V03
addresses that direction while retaining the smaller window and clear space view.
Earlier versions remain as design history. The art-direction section describes
the original V01 experiment. The owner subsequently pointed back to V01 and
clarified that they wanted that composition with duller tones, rather than the
small-window wall treatment. V04 follows this correction; use the V04 notes for
the current direction.

Check composition, amount of space versus machinery, edge brightness, and whether
the cartoon treatment belongs with the unchanged character and round-01 assets.
Phone cropping, actual level coverage, camera movement, and gameplay readability
have not been tested in Unity or on a device.

## Exact generation prompt

Reference images in order:

1. `art/inspirations/characters/main-character-large.png`
2. `art/concepts/round-01/cog-platform-v01.png`
3. `art/concepts/round-01/pressure-pipe-v01.png`
4. `art/inspirations/steampunk-space/spherical-steampunk-space-station.jpg`
5. `Worms Lost in Space and Time(3.0)/Assets/Artwork/level_background.jpg`

```text
Use case: stylized-concept. Generate ONE original landscape 16:9 2D LEVEL BACKGROUND for the cartoon mobile game Worms Lost in Space and Time. This is a review concept, not a title screen or complete playable level. All five input images are reference inputs, not edit targets. Image 1 is the unchanged main character and primary rendering style anchor: bold hand-inked near-black outlines, simplified chunky forms, clear flat cel-shaded planes and a few graphic highlights, approachable anime/cartoon illustration. Images 2 and 3 are the approved visual DIRECTION for cog and pipe concepts: extend their warm brass/gold and burnt-copper machinery, black outlines, restrained teal accents and simple graphic shading into a cohesive background. Image 4 supplies the space-station theme, stars and planetary atmosphere ONLY; do not copy its realism. Image 5 is the ORIGINAL LEVEL BACKGROUND being reconceived, with cogs and pipework: retain these as major motifs, but make an original steampunk-space environment with richer cartoon construction. COMPOSITION: orthographic side-on background view of the inside edge of a space station's clockwork engine bay, looking outward through a broad open observation aperture into deep navy/teal space. A few big layered brass cogs, bolted copper pipe elbows, connecting rods, and gunmetal hull plates frame the outer edges and lower corners. Some overlapping large gear silhouettes sit in a subdued distant layer. One small softly lit blue-green planet and a few sparse stars visible through the opening; simple broad shapes, no nebula noise. Keep the central roughly 60 percent of the canvas quiet, dark blue-teal and low contrast for later gameplay overlays. Do NOT put a bright planet in the middle. Background machinery should be more muted and less contrasty than the foreground assets in images 2 and 3, yet legibly hand-inked, with broad graphic highlights rather than minute detail. All pipework integrated into the environment, no floating loose props. Friendly adventurous retro-space mood, coherent warm metal / cool space contrast, balanced readable shape hierarchy, no photorealistic metal and no gritty texture. Full-bleed opaque landscape illustration with no border, no labels, no text, no logos, no UI, no buttons, no character, no pixel art, no 3D render, no camera perspective corridor, no dramatic floor or obvious walkable platforms. Do not reproduce a complete gameplay layout, hazards, foreground player cogs or collectible objects. Prioritize simple cartoon forms and gameplay readability over ornate clockwork detail.
```
