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
      if (level.Status === 'VerifiedReachable' && (!level.HasReplay || !Number.isFinite(level.CompletionSeconds)
        || level.CompletionSeconds < 0 || level.Replay?.Version !== 2)) throw new Error('Invalid certificate evidence.');
      this.levels.set(level.LevelId, { level, source });
    }
  }

  rows() {
    return [...this.levels.values()].sort((left, right) => left.level.LevelId.localeCompare(right.level.LevelId));
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
      '| Level | Completion (s) | FPS | Jumps | Timing demand | Tightest sampled span (ms) | Tight jumps / longest chain | Successful timing variations |',
      '| --- | ---: | ---: | ---: | --- | ---: | ---: | ---: |'
    ];
    for (const { level } of certified) {
      const difficulty = new RouteDifficulty(level);
      lines.push(`| ${cell(level.LevelId)} | ${level.CompletionSeconds.toFixed(2)} | ${level.Replay.FramesPerSecond} | ${difficulty.jumps} | ${difficulty.rating} | ${difficulty.narrowestSpanMs?.toFixed(0) ?? '—'} | ${difficulty.tightJumps}/${difficulty.longestTightChain} | ${difficulty.successes}/${difficulty.trials} |`);
    }
    lines.push('', '## Rating rules', '',
      'Only complete timing analysis is rated. For each jump, take its widest sampled success window; the tightest of those windows determines the span above.',
      'Timing demand is High for a sampled span ≤20 ms, Moderate for ≤60 ms, otherwise Low. Attempt counts, sampled failure ratios and total jump count do not determine this label. Tight jumps and consecutive tight-jump chains are reported separately.',
      'A 0 ms sampled span means one grid point succeeded, not a literally zero-width input window. The simulation grid is 20 ms. Consult each run request for its sampling radius; combined errors, alternative routes and human retry/learning costs remain separate measures.',
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
      'TightestSampledSpanMs', 'TightJumps', 'LongestTightChain', 'TimingSuccesses', 'TimingTrials', 'Fingerprint', 'Source']];
    for (const { level, source } of this.rows()) {
      const verified = level.Status === 'VerifiedReachable';
      const difficulty = verified ? new RouteDifficulty(level) : null;
      rows.push([level.LevelId, level.Status, verified ? level.CompletionSeconds.toFixed(2) : '',
        verified ? level.Replay.FramesPerSecond : '', difficulty?.jumps ?? '', difficulty?.rating ?? '',
        difficulty?.narrowestSpanMs?.toFixed(0) ?? '', difficulty?.tightJumps ?? '', difficulty?.longestTightChain ?? '', difficulty?.successes ?? '', difficulty?.trials ?? '',
        level.Fingerprint, source]);
    }
    return rows.map(row => row.map(value => `"${String(value ?? '').replaceAll('"', '""')}"`).join(',')).join('\n') + '\n';
  }
}

function cell(value) { return String(value ?? '').replaceAll('|', '/').replace(/[\r\n]+/g, ' '); }

function main(args) {
  const summaries = [];
  let output, csv;
  for (let index = 0; index < args.length; index += 2) {
    const [option, value] = args.slice(index, index + 2);
    if (!value) throw new Error(`Missing value for ${option}`);
    if (option === '--summary') summaries.push(value);
    else if (option === '--output') output = value;
    else if (option === '--csv') csv = value;
    else throw new Error(`Unknown argument: ${option}`);
  }
  if (summaries.length === 0 || !output || !csv)
    throw new Error('Usage: node scripts/report-level-validation.mjs --summary run/summary.json [--summary other/summary.json] --output report.md --csv report.csv');
  const report = new LevelValidationReport();
  for (const file of summaries) report.add(JSON.parse(fs.readFileSync(file, 'utf8').replace(/^\uFEFF/, '')), file);
  for (const destination of [output, csv]) fs.mkdirSync(path.dirname(destination), { recursive: true });
  fs.writeFileSync(output, report.markdown());
  fs.writeFileSync(csv, report.csv());
  console.log(`Wrote ${report.levels.size} level results to ${output} and ${csv}.`);
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  try { main(process.argv.slice(2)); }
  catch (error) { console.error(error.message); process.exitCode = 1; }
}
