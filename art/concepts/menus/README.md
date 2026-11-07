# Start and menu artwork — first pass

This review pass themes Splash, StorySelect (main menu), World 1, World 2, and
their settings panels. It follows the approved Level 1 cartoon steampunk-space
direction: bold outlines, flat shading, brass controls, rustic copper pipes and
a quiet teal view into space. Background machinery stays duller than interactive
gold cogs. There are no loose gears floating outside the station.

The existing astronaut.png character sprite is reused unchanged. Titles, button
labels, level names and settings/status text are editable Unity uGUI Text, using
Bungee for headings/buttons and Nunito for supporting copy. Generated images
contain no baked-in lettering. Existing navigation, unlock rules, save behavior
and settings callbacks are retained.

## Assets and preservation

| Previous visual | Replacement |
| --- | --- |
| shiny-space-art.jpg and external_1500.jpg backgrounds | observatory-background.png |
| Decorative cog1, cog4, cog7 | level-cog.png and backdrop framing |
| Begin.jpg and plain menu button surfaces | menu-button.png with editable labels |
| gameLock.png | lock-badge.png |
| magicalconch.png award | star-badge.png |
| Old title/button typography | Bungee and Nunito with dark outlines |

Source PNGs are beside this README. Production imports live in
`Assets/Artwork/Menus`. The editor command trims transparent padding, scales UI
textures, sets bilinear sprite import and nine-slice borders; the opaque
background retains its source dimensions. The eight replaced images remain in
their original folders with `.legacy` before the extension, retaining their
bytes and existing .meta GUIDs. Older scenes sharing those GUIDs still resolve.
Original MainMenu and WorldMenu prefab layouts are preserved as
`MainMenu.legacy.prefab` and `WorldMenu.legacy.prefab`: these are copies with
new GUIDs, so the active prefabs keep their original GUIDs and scene bindings.
Keep all legacy assets until the owner approves final cleanup.

## Layout and authoring

Run `Worms > Art > Apply start and menu theme` in the pinned Unity Editor, or
`-batchmode -nographics -quit -executeMethod MenuArtTools.Apply` with the project
path. This editor-only command writes editable Canvas objects and preserves
persistent button callbacks. It does not generate images or call an AI service.

Safe-area roots contain title, subtitle, original character and native controls.
Backgrounds cover the viewport using EnvelopeParent. World selection uses
GridLayoutGroup with a small MenuLevelGrid presentation adapter: two columns in
portrait, five in landscape. Square cog faces hold their own labels/lock/star
badges. Settings use a VerticalLayoutGroup so unavailable controls collapse
without gaps. No complete UI hierarchy is constructed at runtime.

## Capture and review

Run Unity PlayMode test
`MenuArtCaptureTests.ThemedMenusUseReadableAssetsAndRetainControls` with
`-worms-menu-capture -screen-width 720 -screen-height 1280`, omitting
`-nographics`. Reports and the four screens/settings views at 720x1280 and
1280x720 go to ignored `TestResults/Art/Menus`. The fixture injects disposable
progress (one completed/starred level) and never reads or writes developer saves.
Regular suites execute the asset/font/control assertions without screenshot IO.

Review background coverage, legible text, distinct disabled states, locks/stars,
and the original character. These camera/Canvas renders do not certify physical
mobile input, store purchases, safe-area behavior on a device, or natural level
traversal. Run the documented player/device checks separately.

## Validation

Repository validation passed: 404 assets/folders and GUIDs, 24 enabled scenes.
Scene/prefab validation passed, along with all 122 EditMode and 46 PlayMode tests
(zero failures/skips). The graphics-enabled menu test passed separately and its
14 portrait/landscape captures were visually reviewed. Reapplying the authoring
command changed none of the four scene files or two active prefab files.
All eight original image files and their metadata are byte-identical under the
new legacy paths. Legacy prefab copies match the old layouts, with only the
root object name changed by Unity's copy operation; active prefab metadata is
unchanged. The six validation-report tool tests passed.

Maintained recorded inputs for levels 1, 3, 4 and 10 were freshly certified at
7.96s, 9.32s, 9.86s and 2.60s, including successful 30/60/120 FPS profiles. All
four new certificates then passed strict replay (13 trials each) before replacing
the maintained files. Recorded jump sequences were unchanged; no hashes were
hand-edited. Reports: `TestResults/Art/MenuRecertification` and `MenuStrictReplay`.
This covers the four maintained routes, not every catalog level.

An initial capture exposed untagged world-menu cameras; the capture fixture now
falls back to the scene camera and the rerun passed. Visual review also caught
badge positioning and hidden settings gaps, corrected before final validation.
Mobile/device and store purchase checks were not run. Screenshots and local
validation reports are intentionally excluded from source control.

## Fonts

Official Google Fonts sources, licensed under SIL Open Font License 1.1:
- [Bungee-Regular.ttf](https://github.com/google/fonts/tree/main/ofl/bungee)
- [Nunito variable font](https://github.com/google/fonts/tree/main/ofl/nunito)

Unmodified font files are in `Assets/Artwork/Menus/Fonts`. License and copyright
notices are bundled in `Assets/Resources/FontLicenses`, including in player builds.

## Generation record

Generated using built-in Imagegen. All five calls used these references in order:
1. `art/inspirations/characters/main-character-large.png` (style anchor only).
2. `art/concepts/round-02/level-background-v04.png` (approved background).
3. `art/concepts/level-01/ui-plate.png` (established control materials).

Exact submitted prompts follow. Each output was inspected before import.

### observatory-background.png

Transparent background: false.

Use case: stylized-concept. Original 2D hand-inked anime/cartoon steampunk-space mobile game artwork, matching the supplied references. Image 1 is the unchanged character style anchor: bold near-black outlines, chunky friendly silhouettes, flat cel-shaded planes and restrained graphic highlights. Do not draw or redesign the character. Image 2 is the approved station background: warm rustic copper/iron framing with deep navy-teal space, clean uncluttered view through glass. Image 3 is the established blank UI plate: brass rim, dark teal inset, cartoon bolts. Maintain that coherent cartoon visual vocabulary. No pixel art, photorealism, 3D render, noisy texture, gritty realistic weathering, words, glyphs, UI text, numbers, watermark, logo, or characters. SUBJECT: one portrait 9:16 full-bleed menu backdrop for a space station observatory/control room. Broad calm deep navy/teal center, large rounded observation window with sparse stars and one small blue-green planet, clean glass with absolutely no cog silhouettes floating in space. Muted oxidized bronze and rust-red copper pipework traces the side edges and bottom; a couple of cropped simple dull brown gears and gunmetal hull plates form a restrained edge frame. Quiet top quarter for a title, quiet middle/lower region for menu controls and unchanged character to be overlaid later. Do not draw controls, button shapes, title plaques or text. The backdrop is subordinate to later bright gold buttons, darker and less saturated. Simplify into bold outlined 2D cel-shaded shapes with 2-3 tones, no fine filigree. Keep the central 70 percent clear for text readability, generous spacing, inviting adventurous mood, opaque background.

### menu-button.png

Transparent background: true.

Use case: stylized-concept. Original 2D hand-inked anime/cartoon steampunk-space mobile game artwork, matching the supplied references. Image 1 is the unchanged character style anchor: bold near-black outlines, chunky friendly silhouettes, flat cel-shaded planes and restrained graphic highlights. Do not draw or redesign the character. Image 2 is the approved station background: warm rustic copper/iron framing with deep navy-teal space, clean uncluttered view through glass. Image 3 is the established blank UI plate: brass rim, dark teal inset, cartoon bolts. Maintain that coherent cartoon visual vocabulary. No pixel art, photorealism, 3D render, noisy texture, gritty realistic weathering, words, glyphs, UI text, numbers, watermark, logo, or characters. SUBJECT: exactly one blank wide rounded rectangular primary menu button. Horizontal 4:1 object on genuinely transparent alpha, fully visible with a little padding. Thick muted gold-brass outer lip with broad simple cartoon highlights, warm burnt-copper lower edge, near-black ink contour, four small steel corner screws. Dark navy-teal inset center covers at least 75 percent of width and 60 percent of height, flat and plain for editable text overlay. Keep borders even so a game engine can nine-slice this shape. No external pipes, gears, symbols, ornaments, letters or shadows outside the object. Same 2D graphic cel-shading as reference plate, friendly substantial mechanical construction, not a shiny 3D badge.

### level-cog.png

Transparent background: true.

Use case: stylized-concept. Original 2D hand-inked anime/cartoon steampunk-space mobile game artwork, matching the supplied references. Image 1 is the unchanged character style anchor: bold near-black outlines, chunky friendly silhouettes, flat cel-shaded planes and restrained graphic highlights. Do not draw or redesign the character. Image 2 is the approved station background: warm rustic copper/iron framing with deep navy-teal space, clean uncluttered view through glass. Image 3 is the established blank UI plate: brass rim, dark teal inset, cartoon bolts. Maintain that coherent cartoon visual vocabulary. No pixel art, photorealism, 3D render, noisy texture, gritty realistic weathering, words, glyphs, UI text, numbers, watermark, logo, or characters. SUBJECT: exactly one square-framed circular level-selection button sprite on transparent alpha. A broad warm gold toothed cog rim with 16 blunt rounded teeth, a narrow burnt-copper inner ring, a large perfectly plain dark navy center disk for later level-number text. Center disk is opaque, not a transparent hole. Only two simple silver fasteners near opposite points on the rim, tiny broad off-white highlight. Straight-on orthographic circular silhouette, centered, no perspective. Clear bold outlines, restrained 2-3 tone flat cel shading. No numbers, lettering, padlocks, stars, extra icons, external decoration or outside shadow. Very readable at a small mobile UI size. Cog is bright gold, distinct from dull brown background machinery.

### lock-badge.png

Transparent background: true.

Use case: stylized-concept. Original 2D hand-inked anime/cartoon steampunk-space mobile game artwork, matching the supplied references. Image 1 is the unchanged character style anchor: bold near-black outlines, chunky friendly silhouettes, flat cel-shaded planes and restrained graphic highlights. Do not draw or redesign the character. Image 2 is the approved station background: warm rustic copper/iron framing with deep navy-teal space, clean uncluttered view through glass. Image 3 is the established blank UI plate: brass rim, dark teal inset, cartoon bolts. Maintain that coherent cartoon visual vocabulary. No pixel art, photorealism, 3D render, noisy texture, gritty realistic weathering, words, glyphs, UI text, numbers, watermark, logo, or characters. SUBJECT: exactly one simple cartoon padlock status icon on genuinely transparent alpha. Squat rounded muted brass body, heavy gunmetal U-shaped shackle, single simple dark keyhole. Chunky near-black outer contour, broad clean two-tone gold shading and small cream highlight. Front-on symmetric silhouette. No text, numbers, emblem, surrounding circle, extra props or shadow. Keep empty padding around the icon. Readable as locked at only 28 pixels tall on a phone.

### star-badge.png

Transparent background: true.

Use case: stylized-concept. Original 2D hand-inked anime/cartoon steampunk-space mobile game artwork, matching the supplied references. Image 1 is the unchanged character style anchor: bold near-black outlines, chunky friendly silhouettes, flat cel-shaded planes and restrained graphic highlights. Do not draw or redesign the character. Image 2 is the approved station background: warm rustic copper/iron framing with deep navy-teal space, clean uncluttered view through glass. Image 3 is the established blank UI plate: brass rim, dark teal inset, cartoon bolts. Maintain that coherent cartoon visual vocabulary. No pixel art, photorealism, 3D render, noisy texture, gritty realistic weathering, words, glyphs, UI text, numbers, watermark, logo, or characters. SUBJECT: exactly one five-point achievement star icon on genuinely transparent alpha. Bright warm gold chunky friendly star, burnt-copper bevel along lower edges, bold dark ink outline and a small broad cream highlight. Very simple flat cel shading, front-on silhouette, not volumetric or realistic. No surrounding circle, gears, words, labels, sparkle rays, numbers, faces or extra props. Empty padding. Clear readable gold star at only 28 pixels tall on a phone, for an earned level award.
