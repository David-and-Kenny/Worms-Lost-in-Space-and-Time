import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

export class RouteDifficulty {
  constructor(report) {
    this.jumps = report.Replay?.Jumps?.length ?? 0;
    this.successes = report.PerturbationSuccesses ?? 0;
    this.trials = report.PerturbationTrials ?? 0;
    this.narrowestSpanMs = null;
    this.rating = 'Not rated';
    this.tightJumps = 0;
    this.longestTightChain = 0;
    if (!report.AnalysisComplete) return;
    if (this.jumps === 0) { this.rating = 'Low'; return; }
    if (this.trials <= 0) return;

    const spans = [];
    for (let jump = 0; jump < this.jumps; jump++) {
      const windows = (report.TimingWindows ?? []).filter(window => window.JumpIndex === jump);
      if (windows.length === 0) return;
      const widest = Math.max(...windows.map(window =>
        (window.LastOffsetSeconds - window.FirstOffsetSeconds) * 1000));
      if (!Number.isFinite(widest) || widest < 0) throw new Error('Invalid timing window.');
      spans.push(widest);
    }
    this.narrowestSpanMs = Math.min(...spans);
    let chain = 0;
    for (const span of spans) {
      if (span <= 20.001) { this.tightJumps++; chain++; }
      else chain = 0;
      this.longestTightChain = Math.max(this.longestTightChain, chain);
    }
    if (this.narrowestSpanMs <= 20.001) this.rating = 'High';
    else if (this.narrowestSpanMs <= 60.001) this.rating = 'Moderate';
    else this.rating = 'Low';
  }
}

export class InputEffort {
  constructor(report) {
    this.verified = report.HeldInput?.Complete === true && report.HeldInput.Verified === true;
    this.presses = this.preparedPresses = this.continuedJumps = this.tightPresses = this.longestTightChain = null;
    if (!this.verified) return;
    const groups = report.HeldInput.Groups;
    this.presses = groups.length;
    this.preparedPresses = groups.filter(group => group.Preheld).length;
    this.continuedJumps = groups.reduce((sum, group) => sum + group.LastJump - group.FirstJump, 0);
    if (!report.AnalysisComplete) return;
    this.tightPresses = this.longestTightChain = 0;
    let chain = 0;
    for (const group of groups) {
      const windows = report.TimingWindows.filter(window => window.JumpIndex === group.FirstJump);
      const span = windows.length ? Math.max(...windows.map(window =>
        (window.LastOffsetSeconds - window.FirstOffsetSeconds) * 1000)) : Infinity;
      if (!group.Preheld && span <= 20.001) { this.tightPresses++; chain++; }
      else chain = 0;
      this.longestTightChain = Math.max(this.longestTightChain, chain);
    }
  }
}

export class LevelValidationReport {
  constructor() { this.levels = new Map(); }

  add(summary, source) {
    if (!summary.Complete || !Array.isArray(summary.Levels)
      || summary.Levels.length !== summary.RequestedLevels) throw new Error(`Incomplete summary: ${source}`);
    for (const level of summary.Levels) {
      const previous = this.levels.get(level.LevelId);
      if (previous && previous.level.Fingerprint !== level.Fingerprint)
        throw new Error(`Different source fingerprints for ${level.LevelId}; recertify before combining reports.`);
      if (previous?.level.Status === 'VerifiedReachable' && level.Status !== 'VerifiedReachable') continue;
      if (previous?.level.Status === 'VerifiedReachable' && previous.level.AnalysisComplete
        && level.Status === 'VerifiedReachable' && !level.AnalysisComplete) continue;
      if (level.Status === 'VerifiedReachable' && (!level.HasReplay || !Number.isFinite(level.CompletionSeconds)
        || level.CompletionSeconds < 0 || level.Replay?.Version !== 2)) throw new Error('Invalid certificate evidence.');
      this.levels.set(level.LevelId, { level, source });
    }
  }

  rows() {
    return [...this.levels.values()].sort((left, right) => left.level.LevelId.localeCompare(right.level.LevelId));
  }

  json() {
    const levels = this.rows().map(row => row.level);
    return JSON.stringify({ Version: 1, Complete: true, RequestedLevels: levels.length,
      Success: levels.every(level => level.Status === 'VerifiedReachable'), Levels: levels,
      LevelSources: Object.fromEntries(this.rows().map(row => [row.level.LevelId, row.source])),
      Sources: [...new Set(this.rows().map(row => row.source))] }, null, 2) + '\n';
  }

  evidence() {
    const snapshot = JSON.parse(this.json());
    snapshot.Purpose = 'Report snapshot; full replay checkpoints are maintained in ValidationRoutes/*.replay.json.';
    snapshot.Levels = snapshot.Levels.map(level => {
      const { Trajectory, ...metrics } = level;
      if (metrics.Replay) {
        const { Expected, ...inputs } = metrics.Replay;
        metrics.Replay = inputs;
      }
      return metrics;
    });
    return JSON.stringify(snapshot, null, 2) + '\n';
  }

  markdown() {
    const rows = this.rows();
    const certified = rows.filter(row => row.level.Status === 'VerifiedReachable');
    const lines = [
      '# Level completion and difficulty report', '',
      'Generated from completed Unity validation runs without an AI service.', '',
      `Evaluated: ${rows.length}; certified routes: ${certified.length}.`, '',
      ...rows.filter(row => row.level.Status === 'InvalidContent').map(({ level }) =>
        `**Content blocker — ${cell(level.LevelId)}:** ${cell(level.Reason)}`), '',
      'Completion times describe the discovered route at its recorded rendering profile; they are not best times.',
      'Timing demand describes measured execution precision. Overall human difficulty and learnability are uncalibrated; repeated failures are expected for this game.', '',
      '| Level | Completion (s) | FPS | Jumps | Exact timing demand | Tightest sampled span (ms) | Tight jumps / longest chain | Successful timing variations |',
      '| --- | ---: | ---: | ---: | --- | ---: | ---: | ---: |'
    ];
    for (const { level } of certified) {
      const difficulty = new RouteDifficulty(level);
      lines.push(`| ${cell(level.LevelId)} | ${level.CompletionSeconds.toFixed(2)} | ${level.Replay.FramesPerSecond} | ${difficulty.jumps} | ${difficulty.rating} | ${difficulty.narrowestSpanMs?.toFixed(0) ?? '—'} | ${difficulty.tightJumps}/${difficulty.longestTightChain} | ${difficulty.successes}/${difficulty.trials} |`);
    }
    lines.push('', '## Verified held-input effort', '',
      'A press group is one button-down interval verified through the production JumpInputBuffer at 30/60/120 FPS. Continuing to hold can launch on subsequent attachments without another press.',
      'Prepared presses begin before the target attachment, including while falling after a forced detach. Counts describe the demonstrated policy, not a proven minimum or physical touch-latency measurement.', '',
      '| Level | Press groups | Prepared presses | Jumps from continued holding | Tight timed presses / longest chain | Hold verification |',
      '| --- | ---: | ---: | ---: | ---: | --- |');
    for (const { level } of certified) {
      const effort = new InputEffort(level);
      lines.push(`| ${cell(level.LevelId)} | ${effort.presses ?? '—'} | ${effort.preparedPresses ?? '—'} | ${effort.continuedJumps ?? '—'} | ${effort.tightPresses ?? '—'}/${effort.longestTightChain ?? '—'} | ${effort.verified ? 'Verified' : 'Not verified'} |`);
    }
    lines.push('', 'Tight timed presses use the first jump’s exact-policy timing samples for each non-prepared group. This is a diagnostic proxy: hold-start/release perturbation windows and overall human difficulty are not measured. Prepared and continued-held jumps do not count as additional precisely timed presses.', '',
      '## Rating rules', '',
      'Only complete timing analysis is rated. For each jump, take its widest sampled success window; the tightest of those windows determines the span above.',
      'Timing demand is High for a sampled span ≤20 ms, Moderate for ≤60 ms, otherwise Low. Attempt counts, sampled failure ratios and total jump count do not determine this label. Tight jumps and consecutive tight-jump chains are reported separately.',
      'A 0 ms sampled span means one grid point succeeded, not a literally zero-width input window. The simulation grid is 20 ms. Consult each run request for its sampling radius; combined errors, alternative routes and human retry/learning costs remain separate measures.',
      'The exact-timing table varies launch ticks, not button-down/hold intervals. Use the separately verified hold-effort table to distinguish new presses from automatic launches while holding.',
      'An easier alternative route may exist. Incomplete analysis stays Not rated.', '',
      '## Unresolved searches and content issues', '',
      '| Level | Status | Reason |', '| --- | --- | --- |');
    for (const { level } of rows.filter(row => row.level.Status !== 'VerifiedReachable'))
      lines.push(`| ${cell(level.LevelId)} | ${cell(level.Status)} | ${cell(level.Reason)} |`);
    lines.push('', 'Inconclusive searches do not prove impossibility. A missing authored finish is a content blocker.', '',
      '## Evidence', '');
    for (const source of [...new Set(rows.map(row => row.source))]) lines.push(`- ${cell(source)}`);
    return lines.join('\n') + '\n';
  }

  csv() {
    const rows = [['Level', 'Status', 'CompletionSeconds', 'RenderingFPS', 'Jumps', 'Difficulty',
      'TightestSampledSpanMs', 'TightJumps', 'LongestTightChain', 'TimingSuccesses', 'TimingTrials', 'Fingerprint', 'Source',
      'HoldVerified', 'PressGroups', 'PreparedPresses', 'ContinuedHeldJumps', 'TightTimedPresses', 'LongestTightPressChain']];
    for (const { level, source } of this.rows()) {
      const verified = level.Status === 'VerifiedReachable';
      const difficulty = verified ? new RouteDifficulty(level) : null;
      const effort = verified ? new InputEffort(level) : null;
      rows.push([level.LevelId, level.Status, verified ? level.CompletionSeconds.toFixed(2) : '',
        verified ? level.Replay.FramesPerSecond : '', difficulty?.jumps ?? '', difficulty?.rating ?? '',
        difficulty?.narrowestSpanMs?.toFixed(0) ?? '', difficulty?.tightJumps ?? '', difficulty?.longestTightChain ?? '', difficulty?.successes ?? '', difficulty?.trials ?? '',
        level.Fingerprint, source, effort?.verified ?? false, effort?.presses ?? '', effort?.preparedPresses ?? '',
        effort?.continuedJumps ?? '', effort?.tightPresses ?? '', effort?.longestTightChain ?? '']);
    }
    return rows.map(row => row.map(value => `"${String(value ?? '').replaceAll('"', '""')}"`).join(',')).join('\n') + '\n';
  }
}

function cell(value) { return String(value ?? '').replaceAll('|', '/').replace(/[\r\n]+/g, ' '); }

function main(args) {
  const summaries = [];
  let output, csv, json, evidence;
  for (let index = 0; index < args.length; index += 2) {
    const [option, value] = args.slice(index, index + 2);
    if (!value) throw new Error(`Missing value for ${option}`);
    if (option === '--summary') summaries.push(value);
    else if (option === '--output') output = value;
    else if (option === '--csv') csv = value;
    else if (option === '--json') json = value;
    else if (option === '--evidence') evidence = value;
    else throw new Error(`Unknown argument: ${option}`);
  }
  if (summaries.length === 0 || !output || !csv)
    throw new Error('Usage: node scripts/report-level-validation.mjs --summary run/summary.json [--summary other/summary.json] --output report.md --csv report.csv [--json summary.json] [--evidence snapshot.json]');
  const report = new LevelValidationReport();
  for (const file of summaries) report.add(JSON.parse(fs.readFileSync(file, 'utf8').replace(/^\uFEFF/, '')), file);
  for (const destination of [output, csv, json, evidence].filter(Boolean)) fs.mkdirSync(path.dirname(destination), { recursive: true });
  fs.writeFileSync(output, report.markdown());
  fs.writeFileSync(csv, report.csv());
  if (json) fs.writeFileSync(json, report.json());
  if (evidence) fs.writeFileSync(evidence, report.evidence());
  console.log(`Wrote ${report.levels.size} level results to ${output} and ${csv}.`);
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  try { main(process.argv.slice(2)); }
  catch (error) { console.error(error.message); process.exitCode = 1; }
}
