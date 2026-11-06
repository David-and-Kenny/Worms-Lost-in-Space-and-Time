# Contributing

## Branches and reviews

Use short-lived feature branches (`codex/description`, `feature/description`, or
`fix/description`) and pull requests into `master`. Pull before starting work:

```sh
git switch master
git pull --ff-only
git switch -c feature/your-change
```

Run the repository check and applicable Unity checks, review the diff, then push
your branch and open a PR. Include what changed, scenes/devices tested, and
screenshots for visible changes. During early development, authors may squash
merge their own PRs once required checks pass; another developer's approval is
optional. Coordinate editor and package upgrades as dedicated PRs.

Current server protection: require a PR with zero mandatory approvals, resolve
conversations, require the `Repository hygiene` status check and an up-to-date
branch, and block force pushes/deletions of `master`. See
`docs/MIGRATION.md` for the settings actually applied during setup.

## Assets, scenes, and prefabs

- Move/rename assets in Unity so `.meta` files travel with them. Never regenerate
  a GUID to resolve a conflict; references depend on it.
- Use **Visible Meta Files** and **Force Text** serialization. These were already
  enabled in the recovered project and are validated by the repository check.
- Coordinate before editing the same scene or prefab. Prefer smaller prefabs and
  focused commits; avoid unrelated mass reserialization.
- On Windows, run `scripts/setup-git.ps1` with your editor path to configure the
  repository-local `unityyamlmerge` driver. It maps Git's base/incoming/current
  files to UnityYAMLMerge. Inspect the result in Unity after every merge.
- On macOS/Linux, configure the same driver using your editor's UnityYAMLMerge:

  ```sh
  git config --local merge.unityyamlmerge.name 'Unity Smart Merge'
  git config --local merge.unityyamlmerge.driver '"/absolute/path/to/UnityYAMLMerge" merge -p %O %B %A %A'
  git config --local merge.unityyamlmerge.recursive binary
  ```

  See the [Unity documentation](https://docs.unity.com/en-us/engine/6000.6/manual/get-started/project-configuration/version-control/smart-merge).
  Local Git driver configuration is not transferred by cloning. The attributes
  file alone does not install the tool. For unresolved conflicts, coordinate
  with the scene author and validate in Play mode before merging.

## Large binary assets

The active artwork is small, so this migration does not rewrite shared history
or move all existing images into LFS. Git LFS is installed for future use. Before
adding large source art/models/audio, agree on the file types and run, for example:

```sh
git lfs track '*.psd'
git add .gitattributes
```

Consider `--lockable` for source assets that cannot be merged. Every contributor
then needs Git LFS; coordinate locking and storage/bandwidth limits. Never track
`.meta`, scenes, or other Unity YAML in LFS. Review binary changes locally.

## Before checking in

Run `node scripts/validate-repository.mjs`, open with the team's exact editor
version, check the Console, and play the affected scenes. Review `git status`
and the diff. Include package manifests/lockfiles when dependencies change.
Keep generated output, local editor preferences, license files, and signing
material out of Git. Do not force-push shared branches or rewrite history to
remove the old cache; that needs a separate coordinated migration.

For behavioral work, run EditMode and PlayMode suites with `scripts/check-unity.ps1`
and list the scenario IDs covered from `docs/TESTING.md`. Use `-Check All` for the
complete pre-merge validation. Read `AGENTS.md` and `docs/ARCHITECTURE.md` for
dependency boundaries and authoring rules. Keep game logic in ordinary objects,
Unity adapters small, and serialized prefab/catalog data editable for designers.
