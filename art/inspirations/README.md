# Worms art inspirations

Visual references and art direction for Worms — Lost in Space and Time.
Use this collection to ground new characters, environments, props, effects,
interface art, and animations in a consistent visual language.

This folder lives outside the Unity project so reference material is not imported
or included in game builds. Production assets belong in the Unity project's
`Assets/Artwork` folder with their Unity metadata.

## Adding references

### File formats

| Material | Recommended format | What to include |
| --- | --- | --- |
| Illustrations, characters, screenshots, palettes | PNG | Prefer the original resolution; retain transparency when present. |
| Photos or painted backgrounds | JPG or PNG | Keep enough detail to see the qualities you like; avoid repeated JPG compression. |
| Existing web images | WebP, PNG, or JPG | Keep the original if it is already clear; conversion is optional. |
| Animation or motion reference | MP4 or GIF | Include the relevant short clip and describe the action or timestamps to inspect. |
| Sprite sheets | PNG | Record frame dimensions, frame order, frame rate, and whether it loops, if known. |
| Editable artwork | Original source, such as PSD, SVG, or Krita's KRA | Include a PNG preview so the design can be inspected without its authoring app. |
| Descriptions and feedback | Markdown (`.md`) | Use a notes file with the same base filename as the reference. |

There is no required image size or aspect ratio for inspiration. Keep useful
originals instead of resizing every reference to a shared canvas. Dimensions and
export requirements for game assets will be decided separately. For very large
source files or long videos, add a source link and a small preview or excerpt
instead of adding the entire file to the repository.

### Folder and filename conventions

Start with a small, curated set of references. Add topic folders such as
`characters`, `environments`, `props`, `effects`, `ui`, and `motion` when there
is material for them. Use descriptive lowercase filenames with hyphens.

Pair each reference with its description:

```text
art/inspirations/
  README.md
  REFERENCE_TEMPLATE.md
  characters/
    round-worm-expressions.png
    round-worm-expressions.md
  environments/
    purple-nebula.jpg
    purple-nebula.md
  motion/
    worm-jump.mp4
    worm-jump.md
```

These filenames are examples; the references have not been added yet. Use
`-v01`, `-v02`, and so on for design variations you want to compare.

### Describe what you like

For each reference, copy the entry in [REFERENCE_TEMPLATE.md](REFERENCE_TEMPLATE.md)
into its matching `.md` file. Plain language is enough. Explain:

1. **What you like:** point to a specific feature, such as the rounded body,
   expressive eyes, muted purple background, or springy landing.
2. **What to carry forward:** say which game asset should use that feature and
   whether it is essential or just an option to explore.
3. **What to avoid:** identify details you do not want carried over.
4. **What remains open:** leave undecided colors, proportions, or timing open
   rather than guessing production requirements.

For a sheet containing several designs, identify a row, column, or labeled
character. For video, give timestamps. If different images supply different
qualities, spell that out: "Use the body proportions from A and the palette
from B." Call out any conflicting preferences so they can be resolved.

Example notes for `round-worm-expressions.md`:

```markdown
# Round worm expressions

- File or URL: round-worm-expressions.png
- Creator / source: [creator name and original page URL, or own sketch]
- Usage rights / permission (or unknown): unknown
- Category: character
- Status: inspiration
- Approval or review feedback: exploring proportions; no final design approved

## What to carry forward

- I like the short, rounded body in the top-left design. Explore that shape
  for the player worm because it reads clearly at a small size.
- Keep the large eyes and clear eyebrow expressions.
- The soft shading is worth trying, but it is optional.

## What to avoid

- Do not use the costume or accessories.
- Avoid the tiny surface details; they disappear on a phone screen.

## Production notes

- Body color is undecided.
- Next experiment: a neutral pose and three expressions on a transparent canvas.

## Experiment history

- No generations yet.
```

You can also add an image and describe your preferences in chat, for example:
"For `round-worm-expressions.png`, I like the eyes and body shape, but make the
outline thinner." Ask the art agent to save that feedback in the matching notes
file so future sessions can use it. Feedback left only in chat may not be
available in another session.

### Track source and approval

Label material as inspiration, an experiment, approved direction, or rejected.
Approval must come from the project owner; a saved image is not automatically an
approved design. Keep source/credit and known usage rights alongside references;
reference material is not automatically cleared for inclusion in the game.
Use **inspiration** as the default status. When approving a reference, specify
the scope: approving its palette does not approve its character design.

## Using references for new work

1. Read these notes and inspect the relevant images before drafting a brief.
2. Select a few complementary references and say which attributes each supplies.
3. Include the selected images as actual reference inputs when generating art;
   a path in a prompt alone does not supply the image to the generator.
4. Record the brief, reference filenames, generation/edit instructions, and review
   feedback alongside experiments so successful directions can be repeated.
5. Promote approved choices into a concise style guide as the direction develops.

For animation, establish an approved character sheet first. Specify the action,
key poses, duration, frame rate, loop behavior, pivot, canvas size, and export
format. Review silhouette consistency, frame alignment, and loop seams before
using the result in Unity. Generated frame sequences may need manual cleanup.

## Current direction

The [existing main character collection](characters/README.md) includes the current
player sprite, larger illustrations, and sketch and pixel variants.
No new visual direction or specific qualities to retain have been approved yet.
Do not infer a new style from the game title alone.
