import fs from 'node:fs';
import path from 'node:path';
import { isDeepStrictEqual } from 'node:util';
import { fileURLToPath } from 'node:url';
import { LevelValidationReport } from './report-level-validation.mjs';

/** Checks committed artifact agreement; Unity remains responsible for source freshness and physical proof. */
export class ValidationArtifactCheck {
  constructor({ catalog, snapshot, certificates, markdown, csv, unityVersion }) {
    Object.assign(this, { catalog, snapshot, certificates, markdown, csv, unityVersion });
  }

  validate() {
    const errors = [];
    const require = (condition, message) => { if (!condition) errors.push(message); };
    const { snapshot } = this;
    require(snapshot.Version === 1 && snapshot.Complete === true, 'Evidence must be a completed version 1 snapshot.');
    const levels = snapshot.Levels ?? [];
    const ids = levels.map(level => level.LevelId);
    const catalogIds = this.catalog.map(level => level.Id);
    require(new Set(catalogIds).size === catalogIds.length, 'Duplicate catalog IDs.');
    require(new Set(ids).size === ids.length, 'Duplicate evidence IDs.');
    require(snapshot.RequestedLevels === catalogIds.length && isDeepStrictEqual([...ids].sort(), [...catalogIds].sort()),
      'Evidence must cover the current catalog exactly.');
    require(snapshot.Success === true && levels.every(level => level.Status === 'VerifiedReachable'),
      'Published catalog levels require certified finish routes; keep unfinished scenes out of the published catalog.');
    require(snapshot.Validation?.UnityVersion === this.unityVersion, 'Evidence editor differs from the pinned editor.');
    const certificateIds = Object.keys(this.certificates).sort();
    require(isDeepStrictEqual(certificateIds, [...catalogIds].sort()), 'Missing or extra maintained certificate files.');

    for (const level of levels) {
      const prefix = level.LevelId + ': ';
      const certificate = this.certificates[level.LevelId];
      if (!certificate) continue;
      const catalog = this.catalog.find(item => item.Id === level.LevelId);
      require(certificate.Version === 2 && certificate.LevelId === level.LevelId, prefix + 'Wrong certificate version/identity.');
      require(/^Assets\/.+\.unity$/.test(certificate.ScenePath ?? '') && !certificate.ScenePath.includes('..')
        && path.posix.basename(certificate.ScenePath, '.unity') === catalog?.SceneName, prefix + 'Scene does not match catalog.');
      require(/^[a-f0-9]{64}$/.test(level.Fingerprint ?? '') && level.Fingerprint === certificate.Fingerprint
        && level.Replay?.Fingerprint === certificate.Fingerprint, prefix + 'Fingerprint mismatch. Reissue artifacts through Unity.');
      require(certificate.UnityVersion === this.unityVersion, prefix + 'Certificate editor differs from pinned editor.');
      require(level.HasReplay === true && level.Replay?.Version === 2
        && isDeepStrictEqual(level.Replay.Jumps, certificate.Jumps), prefix + 'Report inputs differ from certificate.');
      require(level.Replay?.FramesPerSecond === certificate.FramesPerSecond && level.Replay?.Seed === certificate.Seed,
        prefix + 'Report replay profile differs from certificate.');
      const jumps = certificate.Jumps ?? [];
      require(jumps.length <= 64 && jumps.every((jump, index) => Number.isInteger(jump.Frame) && jump.Frame >= 0
        && (index === 0 || jump.Frame > jumps[index - 1].Frame) && Number.isInteger(jump.AttachmentOrdinal)
        && jump.AttachmentOrdinal > 0 && typeof jump.SurfaceId === 'string' && jump.SurfaceId.length > 0
        && Number.isFinite(jump.DelaySeconds) && jump.DelaySeconds >= 0), prefix + 'Malformed jump commands.');
      const events = certificate.Expected ?? [];
      const terminal = events.at(-1);
      require(events.length > 0 && terminal?.Kind === 'Completed' && events.every((event, index) =>
        Number.isInteger(event.Frame) && event.Frame >= 0 && (index === 0 || event.Frame >= events[index - 1].Frame)
        && ['X', 'Y', 'Angle', 'VelocityX', 'VelocityY', 'Oxygen', 'Time'].every(key => Number.isFinite(event[key]))),
      prefix + 'Malformed traversal checkpoints or missing real finish.');
      require(Number.isFinite(level.CompletionSeconds) && level.CompletionSeconds >= 0
        && Math.abs(level.CompletionSeconds - (terminal?.Time ?? NaN)) < .001
        && Math.abs(level.CompletionSeconds - (terminal?.Frame ?? NaN) * .02) < .001,
      prefix + 'Completion time differs from the recorded finish.');
      require(level.AnalysisComplete === true && [30, 60, 120].every(rate =>
        level.Profiles?.filter(profile => profile.FramesPerSecond === rate && profile.Completed).length === 1),
      prefix + 'Missing complete timing/rate analysis.');
      require(jumps.every((_, index) => level.TimingWindows?.some(window => window.JumpIndex === index))
        && level.TimingWindows?.every(window => Number.isInteger(window.JumpIndex) && window.JumpIndex >= 0
          && window.JumpIndex < jumps.length && Number.isFinite(window.FirstOffsetSeconds)
          && Number.isFinite(window.LastOffsetSeconds) && window.FirstOffsetSeconds <= window.LastOffsetSeconds),
      prefix + 'Missing or invalid timing windows.');
      const held = level.HeldInput;
      require(held?.Complete === true && held.Profiles?.length === 3, prefix + 'Missing held-input analysis.');
      if (held) {
        const profiles = held.Profiles ?? [];
        require([30, 60, 120].every(rate => profiles.filter(profile => profile.FramesPerSecond === rate
          && typeof profile.Completed === 'boolean').length === 1), prefix + 'Missing held-input rendering profiles.');
        require(typeof held.Verified === 'boolean' && held.Verified === profiles.every(profile => profile.Completed),
          prefix + 'Held-input verdict contradicts profiles.');
        let nextJump = 0;
        let previousEnd = -1;
        for (const group of held.Groups ?? []) {
          require(group.FirstJump === nextJump && Number.isInteger(group.LastJump) && group.LastJump >= nextJump
            && group.LastJump < jumps.length && Number.isInteger(group.StartFrame) && group.StartFrame >= 0
            && typeof group.Preheld === 'boolean' && group.StartFrame > previousEnd
            && group.StartFrame <= jumps[group.FirstJump]?.Frame
            && (group.Preheld || group.StartFrame === jumps[group.FirstJump]?.Frame)
            && group.EndFrame === jumps[group.LastJump]?.Frame,
          prefix + 'Malformed hold groups.');
          nextJump = group.LastJump + 1;
          previousEnd = group.EndFrame;
        }
        require(nextJump === jumps.length, prefix + 'Hold groups must cover every recorded jump exactly once.');
      }
    }
    if (errors.length) throw new Error(errors.join('\n'));
    const report = new LevelValidationReport();
    report.add(snapshot, 'docs/LEVEL_VALIDATION_EVIDENCE.json');
    const normalized = value => value.replaceAll('\r\n', '\n');
    require(normalized(this.markdown) === report.markdown(), 'Markdown report is outdated; regenerate it from the tracked evidence.');
    require(normalized(this.csv) === report.csv(), 'CSV report is outdated; regenerate it from the tracked evidence.');
    if (errors.length) throw new Error(errors.join('\n'));
    return { levels: levels.length, certificates: certificateIds.length };
  }
}

export function parseCatalog(text) {
  const levels = [];
  for (const line of text.split(/\r?\n/)) {
    const id = line.match(/^\s+- Id:\s*(\S+)\s*$/);
    if (id) levels.push({ Id: id[1] });
    const scene = line.match(/^\s+SceneName:\s*(\S+)\s*$/);
    if (scene && levels.length) levels.at(-1).SceneName = scene[1];
  }
  if (!levels.length || levels.some(level => !level.SceneName)) throw new Error('Invalid serialized level catalog.');
  return levels;
}

function main() {
  const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
  const read = file => fs.readFileSync(path.join(root, file), 'utf8').replace(/^\uFEFF/, '');
  const project = 'Worms Lost in Space and Time(3.0)/';
  const certificates = Object.fromEntries(fs.readdirSync(path.join(root, 'ValidationRoutes'))
    .filter(file => file.endsWith('.replay.json')).map(file => [file.slice(0, -12), JSON.parse(read('ValidationRoutes/' + file))]));
  const result = new ValidationArtifactCheck({
    catalog: parseCatalog(read(project + 'Assets/Resources/Configuration/LevelCatalog.asset')),
    snapshot: JSON.parse(read('docs/LEVEL_VALIDATION_EVIDENCE.json')), certificates,
    markdown: read('docs/FIXED_TICK_LEVEL_REPORT.md'), csv: read('docs/FIXED_TICK_LEVEL_REPORT.csv'),
    unityVersion: read(project + 'ProjectSettings/ProjectVersion.txt').match(/^m_EditorVersion:\s*(\S+)/m)?.[1]
  }).validate();
  console.log(`Validation artifacts agree: ${result.levels} published levels, ${result.certificates} certificates. Unity replay is still required after source changes.`);
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  try { main(); } catch (error) { console.error(error.message); process.exitCode = 1; }
}
