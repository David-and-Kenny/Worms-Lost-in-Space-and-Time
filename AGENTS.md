# Working on Worms

Read `docs/REFACTOR_PLAN.md`, `docs/ARCHITECTURE.md`, and `docs/TESTING.md` before
changing gameplay. If implementation differs from those documents, update the
documents in the same change; do not assume a planned feature is implemented.

- Pin the editor to `ProjectSettings/ProjectVersion.txt`. Run only one Unity
  process against a checkout. Preserve asset GUIDs and existing UnityEvent and
  animation event bindings when moving or renaming components.
- Put decisions in small ordinary C# objects. Keep MonoBehaviours as lifecycle,
  input, physics, or presentation adapters. Core must not depend on Unity, UI,
  navigation, or file paths. Add interfaces at real substitution boundaries.
- Prefer descriptive names, normal formatting, explicit state transitions, and
  short cohesive methods. Avoid object-name lookups, global mutable gameplay
  state, dead commented code, procedural runtime UI construction, and redundant
  wrappers. Explain unusual constraints rather than narrating obvious code.
- Treat save files as user data. Tests use injected storage or temporary folders;
  never touch a developer's actual progress. Do not deserialize BinaryFormatter.
- For each behavior change identify its scenario in `docs/TESTING.md`. Use unit
  tests for rule combinations, PlayMode for Unity wiring, and the player/device
  script for rendering, real input, lifecycle, and natural level reachability.
  Keep tests independent; do not duplicate unit matrices in end-to-end tests.
- Run the repository check and affected suites before committing. Validate all
  scenes/prefabs after serialization changes. Report tests actually executed,
  failures, and unavailable device checks without calling them passes.
- Keep changes focused and use a PR. Commit source, metadata, catalog/prefab
  assets, package manifests/lockfiles, and shared settings. Exclude caches,
  build outputs, local reports, credentials, and signing material.
- When replacing artwork, retain the original in its existing folder with
  `.legacy` before the file extension (for example `cog2.legacy.png`). Preserve
  its Unity GUID and references. Keep legacy assets until the owner explicitly
  approves final asset cleanup; keep the main character unchanged unless asked.
- Use `scripts/validate-levels.ps1` and `docs/LEVEL_VALIDATION.md` for traversal
  testing. Validation must not depend on an AI model or consume AI tokens. Do not
  teleport, invoke completion APIs, or replace certificate hashes to obtain a pass.
  Re-execute recorded inputs to recertify changed content. Keep frame-rate policy
  and simulation changes separate from level authoring and report their evidence.
