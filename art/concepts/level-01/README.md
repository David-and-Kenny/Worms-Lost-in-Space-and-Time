# Level 1 first art pass

Status: approved by the owner for use in Level 1. The main character remains
unchanged. Artwork was generated through built-in Imagegen.

## Inventory and scope

Before the art pass, the instantiated scene used five cog objects across three unique designs:
`cog2`, `cog3`, and `cog5`; seventeen repeated `pipe-large3` hazard sprites;
a `bg_red.jpg` scene override; the player; and the existing Canvas oxygen,
completion, and death screens. There are no visible pickup sprites or exit art.
The invisible finish trigger is retained without inventing a new goal mechanic.
The player's existing artwork and body-part effects are unchanged.

| Retained source | Active production asset under Assets/Artwork/Level1 |
| --- | --- |
| cog2.legacy.png | cog-spokes.png |
| cog3.legacy.png | cog-double-ring.png |
| cog5.legacy.png | cog-cutouts.png |
| pipe-large3.legacy.png | hazard-pipe.png |
| bg_red.legacy.jpg scene override | background.png, based on round-02 V04 |
| Plain UI surfaces | ui-plate.png plus cream text and teal oxygen fill |

Source generated PNGs are beside this README. Selected background source:
[round 02 V04](../round-02/level-background-v04.md). Production assets are imported
through `Assets/Editor/Level1ArtTools.cs`; scene-level overrides isolate this
review pass from other levels and shared prefabs. Original art remains in place.

## Legacy preservation

Replaced originals stay in their existing folders with `.legacy` immediately
before the image extension, retaining the file type Unity imports. The five
sources above are renamed through `Level1ArtTools.MarkOriginalArtLegacy`, which
verifies unchanged image bytes and GUIDs. Their `.meta` files move with them;
shared prefabs and other levels continue to reference the original GUIDs.
The historical GUID-baseline manifest keeps its original recorded paths.

The owner requested retaining legacy artwork until all assets are finalized.
Do not delete or overwrite it during subsequent art passes. The new Level 1
textures remain active. The old plain UI surfaces were serialized Image settings,
not separate image files; their shared source prefabs remain unchanged. The
main character, unrelated artwork, and versioned concept iterations are retained.

Legacy-retention verification: Git detects all five images and their metadata as
100% unchanged renames. The art importer still runs successfully using the legacy
paths, with identical hashes for the approved Level 1 scene and all six active
textures plus their metadata. Repository and all-scene/prefab checks passed, as
did 86 EditMode and 40 PlayMode tests. The original jump policies for all five
maintained routes were re-executed to regenerate their certificates after the
path changes; all passed strict batch replay (13 trials per route). Local evidence
is under `TestResults/Art/LegacyRecertification` and `LegacyStrictBatch`.
No build or device checks were added for this filename-only preservation step.

The importer removes generation padding, maps artwork into the original alpha
envelope, and sets canvas size/pixels-per-unit and pivot to preserve world bounds.
Cog and pipe outputs use three times the original imported canvas dimensions;
the background uses twice those dimensions. Original colliders, transforms,
movement, timings, camera policy, character bindings, and event callbacks remain
unchanged. A generated UI plate is cropped/resampled to a scalable nine-slice
sprite. All text remains editable Unity text; UI is authored in the scene by the
editor tool, not procedurally built at runtime.

## Visual checks

`Level1ArtCaptureTests.CaptureAdvancingLevelOne` simulates the actual
scene with ordinary physics and in-memory save storage, checks advancing frames,
game time and wheel rotation. Its optional screenshot mode captures the actual
runtime camera with the existing Canvas UI. It does not teleport, inject
completion, or alter timestep.
Local captures and evidence go to `TestResults/Art` and are excluded from Git.

Capture with the pinned Unity editor, graphics enabled (omit `-nographics`):

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.6.8f1/Editor/Unity.com' -batchmode -projectPath './Worms Lost in Space and Time(3.0)' -runTests -testPlatform PlayMode -testFilter Level1ArtCaptureTests.CaptureAdvancingLevelOne -worms-art-capture -screen-width 720 -screen-height 1280 -testResults './TestResults/Art/capture.xml' -logFile './Logs/Art-Capture.log'
```

The portrait camera capture is the primary mobile review view. Landscape exposes
existing background-coverage/camera framing limits; this art pass does not change
that policy. Camera-to-texture captures are not device screenshots or proof of
natural completion. Re-execute the recorded Level 1 inputs with the no-AI level
validator for traversal evidence. Device input, feel, and readability remain
manual checks.

## Executed validation

- Repository check: 357 assets/folders and GUIDs, 24 enabled build scenes.
- Scene/prefab binding validation: passed.
- EditMode: 86 passed, zero failed.
- PlayMode: 40 passed, zero failed, zero skipped.
- Graphics-enabled art capture: one passed; portrait and landscape runtime-camera
  PNGs written to `TestResults/Art`. Final simulation advanced 1.35 game seconds
  and over 4,000 frames; wheel angle advanced about 135 degrees. The character
  moved naturally; no completion API or teleport was used.
- Art authoring rerun: scene hash unchanged, confirming idempotence.
- Diff audit: preserved 13 existing transform/physics/script YAML blocks and
  295 non-art prefab overrides; original asset GUIDs remain unchanged.
- Fresh Level 1 recorded-input proposal: VerifiedReachable, 174 trials including
  analysis, 9.12-second completion at 60 Hz. The generated certificate keeps the
  same inputs/checkpoints and updates its source fingerprint after re-execution.
- Strict Level 1 replay: 13 trials passed. Strict maintained-route batch:
  levels 1, 3, 4, 8, and 10 all VerifiedReachable, 13 trials each.

The exploratory 30 Hz and 120 Hz Level 1 policies ended Dead; 60 Hz completed.
This is consistent with the separately documented frame-rate concern; no timing
or simulation policy was changed for the artwork.

Two initial validation attempts were not passes: an ignored capture fixture
caused the strict runner to reject the otherwise 39-pass PlayMode report, and a
one-trial route budget ended Inconclusive during calibration. The capture check
now runs meaningful presentation assertions in the regular suite, with only
rendering optional. The final full suite and properly budgeted route runs passed.

Windows/Android builds and physical device checks were not run for this art pass.
Portrait cropping, touch/lifecycle, movement feel, and device rendering remain
manual acceptance work. Raw logs, XML, screenshots, and analysis reports stay in
ignored local output folders.

## Exact generation prompts

Each gameplay sprite used its matching original as silhouette reference,
`art/concepts/round-01/cog-platform-v01.png` as rendering reference, and
`art/inspirations/characters/main-character-large.png` as the unchanged style anchor.
The UI plate used the round-01 cog, round-01 pressure pipe, and character in that order.

## cog-spokes

```text
Create ONE production-candidate transparent 2D cartoon steampunk-space sprite for Worms Lost in Space and Time. All images are reference inputs, not edit targets. First image is the old asset silhouette/role; second is the new approved cog rendering style, third is the unchanged main character style anchor. Use bold slightly hand-inked near-black outlines, chunky simplified shapes, broad clean cel shading with a few restrained graphic off-white highlights. Warm bright gold/brass front faces, burnt copper collars, dark charcoal recesses and very small teal engineering accents. Match cartoon style; no pixel art, realism, 3D rendering, fine noisy texture, gritty rust or ornate filigree. Mobile-size readability. One object only on genuinely transparent alpha including its holes. No baked checkerboard, cast shadow, scene, words, label, character, logo, or watermark. For cogs: perfectly circular face-on orthographic view, perfectly centered axle, full silhouette visible, tooth tips on a consistent circle with evenly spaced blunt teeth, minimal padding, no perspective depth extrusion. Preserve the broad geometry and holes in the old asset; this is a skin that must read the same gameplay silhouette. SUBJECT: one large six-spoke cog wheel, radial chunky open spokes linking the outer gold toothed ring to a compact layered copper and gold hub. Subtle teal narrow center ring. Keep open spokes large. No exterior attachments.
```

## cog-double-ring

```text
Create ONE production-candidate transparent 2D cartoon steampunk-space sprite for Worms Lost in Space and Time. All images are reference inputs, not edit targets. First image is the old asset silhouette/role; second is the new approved cog rendering style, third is the unchanged main character style anchor. Use bold slightly hand-inked near-black outlines, chunky simplified shapes, broad clean cel shading with a few restrained graphic off-white highlights. Warm bright gold/brass front faces, burnt copper collars, dark charcoal recesses and very small teal engineering accents. Match cartoon style; no pixel art, realism, 3D rendering, fine noisy texture, gritty rust or ornate filigree. Mobile-size readability. One object only on genuinely transparent alpha including its holes. No baked checkerboard, cast shadow, scene, words, label, character, logo, or watermark. For cogs: perfectly circular face-on orthographic view, perfectly centered axle, full silhouette visible, tooth tips on a consistent circle with evenly spaced blunt teeth, minimal padding, no perspective depth extrusion. Preserve the broad geometry and holes in the old asset; this is a skin that must read the same gameplay silhouette. SUBJECT: one concentric double-ring cog: large outer toothed gold ring surrounding a smaller copper/brass internally nested toothed ring, broad transparent center. Four short understated radial metal bridges join the rings but preserve open circular negative spaces. This is a distinctive hollow ring wheel, NOT a spoked wheel with a solid hub. A few large simple bolts. No opaque disk in center.
```

## cog-cutouts

```text
Create ONE production-candidate transparent 2D cartoon steampunk-space sprite for Worms Lost in Space and Time. All images are reference inputs, not edit targets. First image is the old asset silhouette/role; second is the new approved cog rendering style, third is the unchanged main character style anchor. Use bold slightly hand-inked near-black outlines, chunky simplified shapes, broad clean cel shading with a few restrained graphic off-white highlights. Warm bright gold/brass front faces, burnt copper collars, dark charcoal recesses and very small teal engineering accents. Match cartoon style; no pixel art, realism, 3D rendering, fine noisy texture, gritty rust or ornate filigree. Mobile-size readability. One object only on genuinely transparent alpha including its holes. No baked checkerboard, cast shadow, scene, words, label, character, logo, or watermark. For cogs: perfectly circular face-on orthographic view, perfectly centered axle, full silhouette visible, tooth tips on a consistent circle with evenly spaced blunt teeth, minimal padding, no perspective depth extrusion. Preserve the broad geometry and holes in the old asset; this is a skin that must read the same gameplay silhouette. SUBJECT: one broad gold toothed disk cog with FIVE large circular transparent cutouts arranged evenly around a small dark hexagonal central axle opening, as the original image. Copper inner bevel around holes, simple restrained steel bolts between cutouts. Keep the five holes large and clean. No solid hub covering the center hole.
```

## hazard-pipe

```text
Create ONE production-candidate transparent 2D cartoon steampunk-space sprite for Worms Lost in Space and Time. All images are reference inputs, not edit targets. First image is the old asset silhouette/role; second is the new approved cog rendering style, third is the unchanged main character style anchor. Use bold slightly hand-inked near-black outlines, chunky simplified shapes, broad clean cel shading with a few restrained graphic off-white highlights. Warm bright gold/brass front faces, burnt copper collars, dark charcoal recesses and very small teal engineering accents. Match cartoon style; no pixel art, realism, 3D rendering, fine noisy texture, gritty rust or ornate filigree. Mobile-size readability. One object only on genuinely transparent alpha including its holes. No baked checkerboard, cast shadow, scene, words, label, character, logo, or watermark. For cogs: perfectly circular face-on orthographic view, perfectly centered axle, full silhouette visible, tooth tips on a consistent circle with evenly spaced blunt teeth, minimal padding, no perspective depth extrusion. Preserve the broad geometry and holes in the old asset; this is a skin that must read the same gameplay silhouette. SUBJECT: one very long narrow straight horizontal pressure pipe used as a stationary lethal boundary in Level 1. Side-on cylindrical tube, pipe about nine times as wide as tall, occupy almost full canvas width, ample transparent empty space above and below if canvas is less wide. No bends or protruding valves. Warm copper/orange metal tube, several broad gold bolted collars and one dark gunmetal reinforcing band, slim teal inspection inset entirely within tube profile. A few dark-red inset accent stripes suggest dangerous pressure machinery. Rounded clear black outline. Keep overall silhouette a thin rectangular horizontal strip, no extra pieces or feet. No pipe too thick relative to length.
```

## ui-plate

```text
Create one blank wide rounded rectangular UI button plate sprite for a 2D anime/cartoon steampunk-space mobile game, on genuine transparent alpha. Use reference 1 approved cog and reference 2 approved pipe only as rendering style/material references, reference 3 main character is unchanged style anchor. Bold near-black hand-inked outlines, chunky graphic shapes, clean broad cel shading, no pixel art or realism. Wide horizontal plate with muted brass/copper rim, four small readable steel screw heads near corners, dark blue-teal inset center left very plain and empty so actual editable Unity text can be overlaid. Rounded corners, very narrow even border relative to the ample dark text area, small warm rim glints, do not add glyphs or text or UI icons. One flat frontal plate only. No scene, shadow outside plate, no logo, character, checkerboard, or perspective. Three times as wide as tall with padding. Need clean frame for scalable buttons and meter backing.
```
