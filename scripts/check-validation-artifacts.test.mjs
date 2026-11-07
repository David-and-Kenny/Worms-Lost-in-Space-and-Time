import test from 'node:test';
import assert from 'node:assert/strict';
import { ValidationArtifactCheck, parseCatalog } from './check-validation-artifacts.mjs';
import { LevelValidationReport } from './report-level-validation.mjs';

function fixture() {
  const profile = FramesPerSecond => ({ FramesPerSecond, Completed: true });
  const point = (Kind, Frame) => ({ Kind, Frame, Time: Frame * .02,
    X: 0, Y: 0, Angle: 0, VelocityX: 0, VelocityY: 0, Oxygen: -1 });
  const certificate = { Version: 2, LevelId: 'level-01', ScenePath: 'Assets/Scenes/Level1.unity',
    UnityVersion: '6000.6.8f1', Fingerprint: 'a'.repeat(64), FramesPerSecond: 30, Seed: 1729,
    Jumps: [{ Frame: 50, AttachmentOrdinal: 1, SurfaceId: 'wheel', DelaySeconds: .2 }],
    Expected: [point('Attached', 40), point('Launched', 50), point('Completed', 200)] };
  const level = { LevelId: 'level-01', Status: 'VerifiedReachable', HasReplay: true,
    Fingerprint: certificate.Fingerprint, Replay: structuredClone(certificate), CompletionSeconds: 4,
    AnalysisComplete: true, Profiles: [30, 60, 120].map(profile), PerturbationTrials: 5, PerturbationSuccesses: 1,
    TimingWindows: [{ JumpIndex: 0, FirstOffsetSeconds: 0, LastOffsetSeconds: 0 }],
    HeldInput: { Complete: true, Verified: true, Profiles: [30, 60, 120].map(profile),
      Groups: [{ FirstJump: 0, LastJump: 0, StartFrame: 50, EndFrame: 50, Preheld: false }] } };
  const snapshot = { Version: 1, Complete: true, RequestedLevels: 1, Success: true,
    Validation: { UnityVersion: '6000.6.8f1' }, Levels: [level] };
  const report = new LevelValidationReport(); report.add(snapshot, 'docs/LEVEL_VALIDATION_EVIDENCE.json');
  return { catalog: [{ Id: 'level-01', SceneName: 'Level1' }], snapshot,
    certificates: { 'level-01': certificate }, markdown: report.markdown(), csv: report.csv(), unityVersion: '6000.6.8f1' };
}

test('complete, consistent artifacts pass and CRLF reports are portable', () => {
  const data = fixture(); data.markdown = data.markdown.replaceAll('\n', '\r\n');
  assert.deepEqual(new ValidationArtifactCheck(data).validate(), { levels: 1, certificates: 1 });
  assert.deepEqual(parseCatalog('  - Id: level-01\r\n    WorldId: world-1\r\n    SceneName: Level1\r\n'), data.catalog);
});
test('missing certificates and duplicate or incomplete catalog coverage fail', () => {
  const missing = fixture(); delete missing.certificates['level-01'];
  assert.throws(() => new ValidationArtifactCheck(missing).validate(), /Missing or extra/);
  const duplicate = fixture(); duplicate.snapshot.Levels.push(duplicate.snapshot.Levels[0]);
  assert.throws(() => new ValidationArtifactCheck(duplicate).validate(), /Duplicate evidence/);
  const unpublished = fixture(); unpublished.snapshot.Levels[0].Status = 'Inconclusive';
  assert.throws(() => new ValidationArtifactCheck(unpublished).validate(), /certified finish routes/);
});
test('fingerprints and editor versions must agree across artifacts', () => {
  const data = fixture(); data.certificates['level-01'].Fingerprint = 'b'.repeat(64);
  data.certificates['level-01'].UnityVersion = 'old';
  assert.throws(() => new ValidationArtifactCheck(data).validate(), /Fingerprint mismatch[\s\S]*pinned editor/);
});
test('changed commands, scene identity and finish times cannot hide in a snapshot', () => {
  const data = fixture(); data.certificates['level-01'].Jumps[0].Frame++;
  data.certificates['level-01'].ScenePath = 'Assets/Scenes/Other.unity';
  data.snapshot.Levels[0].CompletionSeconds = 3;
  assert.throws(() => new ValidationArtifactCheck(data).validate(), /Scene does not match[\s\S]*inputs differ[\s\S]*Completion time/);
});
test('missing timing/rate evidence and malformed held groups fail', () => {
  const data = fixture(); const level = data.snapshot.Levels[0];
  level.Profiles.pop(); level.HeldInput.Groups[0].LastJump = 7;
  assert.throws(() => new ValidationArtifactCheck(data).validate(), /timing\/rate[\s\S]*hold groups/);
});
test('manually edited Markdown or CSV must be regenerated', () => {
  for (const field of ['markdown', 'csv']) {
    const data = fixture(); data[field] += 'stale';
    assert.throws(() => new ValidationArtifactCheck(data).validate(), /outdated/);
  }
});
