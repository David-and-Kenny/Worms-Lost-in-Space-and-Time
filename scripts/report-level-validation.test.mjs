import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { execFileSync } from 'node:child_process';
import { RouteDifficulty, LevelValidationReport } from './report-level-validation.mjs';

function route(overrides = {}) {
  return { LevelId: 'level-01', Fingerprint: 'current', Status: 'VerifiedReachable', HasReplay: true,
    CompletionSeconds: 4.2, AnalysisComplete: true, PerturbationTrials: 25, PerturbationSuccesses: 20,
    Replay: { Version: 2, FramesPerSecond: 60, Jumps: [{}] },
    TimingWindows: [{ JumpIndex: 0, FirstOffsetSeconds: -.1, LastOffsetSeconds: .1 }], ...overrides };
}
function summary(...levels) { return { Complete: true, RequestedLevels: levels.length, Levels: levels }; }

test('ratings distinguish precision, broader timing and route length', () => {
  assert.equal(new RouteDifficulty(route()).rating, 'Low');
  assert.equal(new RouteDifficulty(route({ TimingWindows: [{ JumpIndex: 0, FirstOffsetSeconds: 0, LastOffsetSeconds: .04 }] })).rating, 'Moderate');
  assert.equal(new RouteDifficulty(route({ TimingWindows: [{ JumpIndex: 0, FirstOffsetSeconds: 0, LastOffsetSeconds: .02 }] })).rating, 'High');
  const long = route({ Replay: { Jumps: Array(8).fill({}) },
    TimingWindows: Array.from({ length: 8 }, (_, JumpIndex) => ({ JumpIndex, FirstOffsetSeconds: -.1, LastOffsetSeconds: .1 })) });
  assert.equal(new RouteDifficulty(long).rating, 'High');
});

test('incomplete evidence stays unrated and zero-jump analysis is low', () => {
  assert.equal(new RouteDifficulty(route({ AnalysisComplete: false })).rating, 'Not rated');
  assert.equal(new RouteDifficulty(route({ TimingWindows: [] })).rating, 'Not rated');
  assert.equal(new RouteDifficulty(route({ Replay: { Jumps: [] }, PerturbationTrials: 0 })).rating, 'Low');
});

test('disconnected windows use the widest valid window for each jump', () => {
  const rating = new RouteDifficulty(route({ TimingWindows: [
    { JumpIndex: 0, FirstOffsetSeconds: -.1, LastOffsetSeconds: .1 },
    { JumpIndex: 0, FirstOffsetSeconds: .2, LastOffsetSeconds: .2 }] }));
  assert.equal(rating.rating, 'Low');
  assert.equal(rating.narrowestSpanMs, 200);
});

test('aggregation retains verified evidence and reports content blockers honestly', () => {
  const report = new LevelValidationReport();
  report.add(summary(route(), { LevelId: 'level-20', Fingerprint: 'scene20', Status: 'InvalidContent', Reason: 'No finish.' }), 'catalog');
  report.add(summary(route({ Status: 'Inconclusive', HasReplay: false })), 'retry');
  assert.match(report.markdown(), /level-01 \| 4\.20/);
  assert.match(report.markdown(), /level-20 \| InvalidContent \| No finish\./);
  assert.match(report.markdown(), /Inconclusive searches do not prove impossibility/);
});

test('partial runs and mixed-source evidence are rejected', () => {
  const report = new LevelValidationReport();
  assert.throws(() => report.add({ ...summary(route()), Complete: false }, 'partial'), /Incomplete/);
  report.add(summary(route()), 'current');
  assert.throws(() => report.add(summary(route({ Fingerprint: 'old' })), 'old'), /Different source/);
});

test('CLI writes readable Markdown and CSV without Unity or network access', () => {
  const folder = fs.mkdtempSync(path.join(os.tmpdir(), 'worms-report-'));
  try {
    const input = path.join(folder, 'summary.json');
    const output = path.join(folder, 'report.md');
    const csv = path.join(folder, 'report.csv');
    fs.writeFileSync(input, JSON.stringify(summary(route())));
    execFileSync(process.execPath, [path.join(import.meta.dirname, 'report-level-validation.mjs'),
      '--summary', input, '--output', output, '--csv', csv]);
    assert.match(fs.readFileSync(output, 'utf8'), /certified routes: 1/);
    assert.match(fs.readFileSync(csv, 'utf8'), /"level-01","VerifiedReachable","4.20","60","1","Low"/);
  } finally { fs.rmSync(folder, { recursive: true, force: true }); }
});
