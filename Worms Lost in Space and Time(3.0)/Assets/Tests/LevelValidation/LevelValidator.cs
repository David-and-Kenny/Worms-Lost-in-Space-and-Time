using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace Worms.LevelValidation
{
    public sealed class LevelValidator
    {
        private readonly ILevelTrialRunner runner;
        private readonly ILevelTrialRunner automaticRunner;
        private readonly LevelValidationRequest request;
        private readonly Stopwatch budget = new Stopwatch();
        private LevelValidationReport report;
        public bool BudgetExpired => budget.Elapsed.TotalSeconds >= request.SearchSeconds || report.Trials >= request.MaximumTrials;
        public LevelValidator(ILevelTrialRunner runner, LevelValidationRequest request, ILevelTrialRunner automaticRunner = null)
        { this.runner = runner; this.request = request; this.automaticRunner = automaticRunner ?? runner; }

        private IEnumerator Trial(LevelManifest manifest, TrialPlan plan, int rate, Action<TrialResult> receive, bool automatic = false)
        {
            if (BudgetExpired) { receive(new TrialResult { Outcome = TrialOutcome.Cancelled, Reason = "Validation budget exhausted." }); yield break; }
            yield return (automatic ? automaticRunner : runner).Run(manifest, plan, rate, result =>
            {
                if (result.Outcome == TrialOutcome.ExecutionError) throw new InvalidOperationException(result.Reason);
                if (result.Outcome == TrialOutcome.Completed && result.SaveWrites != 1)
                    throw new InvalidOperationException("A real finish must persist exactly once in disposable storage.");
                receive(result);
            });
            report.Trials++;
        }

        public IEnumerator Evaluate(LevelManifest manifest, InputReplay saved, Action<LevelValidationReport> completed)
        {
            request.Validate();
            report = new LevelValidationReport { LevelId = manifest.LevelId, Fingerprint = manifest.Fingerprint };
            report.Diagnostics.AddRange(manifest.Warnings);
            budget.Restart();
            try
            {
                if (manifest.Errors.Count > 0)
                {
                    report.Outcome = ValidationOutcome.InvalidContent;
                    report.Reason = string.Join("; ", manifest.Errors);
                    yield break;
                }
                if (request.Operation == "replay" && (saved == null || !saved.IsWellFormed()
                    || saved.LevelId != manifest.LevelId || saved.Fingerprint != manifest.Fingerprint || saved.UnityVersion != Application.unityVersion))
                {
                    report.Outcome = ValidationOutcome.Stale;
                    report.Reason = "Replay missing or its version, level, source fingerprint or editor version changed. Search to issue a new certificate.";
                    yield break;
                }
                var calibrationPlan = new TrialPlan();
                if (saved != null && saved.IsWellFormed() && saved.LevelId == manifest.LevelId)
                {
                    calibrationPlan.Jumps = saved.Jumps;
                    if (request.Operation != "replay") report.Diagnostics.Add("Recorded inputs are a proposal only; the old certificate is not trusted. Fresh verification is required.");
                }
                TrialResult baseline = null;
                for (int repeat = 0; repeat < 10; repeat++)
                {
                    if (BudgetExpired) { Inconclusive("Budget exhausted during calibration."); yield break; }
                    TrialResult next = null;
                    yield return Trial(manifest, calibrationPlan, request.FramesPerSecond, result => next = result);
                    if (next.Outcome == TrialOutcome.Cancelled) { Inconclusive(next.Reason); yield break; }
                    if (baseline == null) baseline = next;
                    else if (!ReplayComparison.Equivalent(baseline, next, out string difference))
                    {
                        report.Outcome = ValidationOutcome.RunnerUnstable;
                        report.Reason = "Identical trial " + (repeat + 1) + " changed: " + difference;
                        report.Diagnostics.Add(UnityEngine.JsonUtility.ToJson(baseline));
                        report.Diagnostics.Add(UnityEngine.JsonUtility.ToJson(next));
                        yield break;
                    }
                }
                if (request.Operation == "replay")
                {
                    var expected = new TrialResult { Outcome = TrialOutcome.Completed, Events = saved.Expected, SaveWrites = 1 };
                    if (baseline.Outcome != TrialOutcome.Completed || !ReplayComparison.Equivalent(expected, baseline, out _))
                    {
                        report.Outcome = ValidationOutcome.ReplayFailed;
                        report.Reason = "Saved route no longer reproduces its successful traversal.";
                        yield break;
                    }
                    report.Replay = saved;
                    Verified(baseline);
                    yield return VerifyAutomatic(manifest, calibrationPlan);
                    yield break;
                }

                TrialResult solution = baseline.Outcome == TrialOutcome.Completed ? baseline : null;
                var solutionPlan = RouteSearch.Copy(calibrationPlan);
                if (solution == null)
                {
                    var search = new RouteSearch(request, manifest);
                    var initial = new TrialPlan { StopAtFrontier = true };
                    TrialResult first = null;
                    yield return Trial(manifest, initial, request.FramesPerSecond, value => first = value);
                    if (first.Outcome == TrialOutcome.Completed) solution = first;
                    else search.Add(initial, first);
                    while (solution == null && !BudgetExpired)
                    {
                        var candidate = search.Next();
                        if (candidate == null) break;
                        TrialResult result = null;
                        yield return Trial(manifest, candidate, request.FramesPerSecond, value => result = value);
                        if (result.Outcome == TrialOutcome.Completed) { solution = result; solutionPlan = candidate; }
                        else search.Add(candidate, result);
                    }
                }
                if (solution == null) { Inconclusive("No route found within the configured search frontier/budget. This is not proof of impossibility."); yield break; }
                solutionPlan.StopAtFrontier = false;
                solutionPlan.ObserveAfterFrame = -1;
                for (int repeat = 0; repeat < 3; repeat++)
                {
                    if (BudgetExpired) { Inconclusive("Found a candidate but budget expired before certification."); yield break; }
                    TrialResult next = null;
                    yield return Trial(manifest, solutionPlan, request.FramesPerSecond, value => next = value);
                    if (next.Outcome != TrialOutcome.Completed || !ReplayComparison.Equivalent(solution, next, out string difference))
                    {
                        report.Outcome = ValidationOutcome.RunnerUnstable;
                        report.Reason = "Candidate did not survive fresh certification: " + next.Reason;
                        report.Diagnostics.Add(JsonUtility.ToJson(solution));
                        report.Diagnostics.Add(JsonUtility.ToJson(next));
                        yield break;
                    }
                }
                report.Replay = new InputReplay { LevelId = manifest.LevelId, ScenePath = manifest.ScenePath, Fingerprint = manifest.Fingerprint,
                    UnityVersion = Application.unityVersion, FramesPerSecond = request.FramesPerSecond, Seed = request.Seed,
                    Jumps = solutionPlan.Jumps, Expected = solution.Events };
                Verified(solution);
                yield return VerifyAutomatic(manifest, solutionPlan);
                if (report.Outcome != ValidationOutcome.VerifiedReachable) yield break;
                yield return Analyze(manifest, solutionPlan);
            }
            finally
            {
                budget.Stop(); report.WallSeconds = budget.Elapsed.TotalSeconds;
                completed(report);
            }
        }

        private void Verified(TrialResult result)
        {
            report.Outcome = ValidationOutcome.VerifiedReachable;
            report.Reason = "Reached the authored finish through normal input and reproduced the route.";
            report.CompletionSeconds = result.Elapsed;
            report.OxygenRemaining = result.Oxygen;
            report.Trajectory = result.Trace;
        }
        private void Inconclusive(string reason) { report.Outcome = ValidationOutcome.Inconclusive; report.Reason = reason; }

        private IEnumerator VerifyAutomatic(LevelManifest manifest, TrialPlan solution)
        {
            var policy = RouteSearch.Copy(solution);
            policy.RelativeTiming = true;
            for (int repeat = 0; repeat < 3; repeat++)
            {
                if (BudgetExpired)
                { Inconclusive("Budget expired before normal-physics verification."); report.Replay = null; yield break; }
                TrialResult result = null;
                yield return Trial(manifest, policy, request.FramesPerSecond, value => result = value, true);
                if (result.Outcome != TrialOutcome.Completed)
                {
                    report.Outcome = ValidationOutcome.ReplayFailed;
                    report.Reason = "Controlled route did not reproduce with Unity automatic physics: " + result.Reason;
                    report.Replay = null;
                    yield break;
                }
            }
        }

        private IEnumerator Analyze(LevelManifest manifest, TrialPlan solution)
        {
            foreach (int rate in new[] { 30, 60, 120 })
            {
                if (BudgetExpired) yield break;
                var policy = RouteSearch.Copy(solution);
                policy.RelativeTiming = true;
                TrialResult result = null;
                yield return Trial(manifest, policy, rate, value => result = value, true);
                report.Profiles.Add(new ProfileResult { FramesPerSecond = rate, Completed = result.Outcome == TrialOutcome.Completed, Reason = result.Reason });
            }
            int radius = (int)Math.Ceiling(.25f * request.FramesPerSecond);
            for (int index = 0; index < solution.Jumps.Count; index++)
            {
                int? start = null;
                for (int offset = -radius; offset <= radius; offset++)
                {
                    if (BudgetExpired) yield break;
                    var variation = RouteSearch.Copy(solution);
                    variation.RelativeTiming = true;
                    variation.Jumps[index].DelaySeconds += offset / (float)request.FramesPerSecond;
                    bool success = false;
                    if (variation.Jumps[index].DelaySeconds >= 0)
                    {
                        TrialResult result = null;
                        yield return Trial(manifest, variation, request.FramesPerSecond, value => result = value);
                        success = result.Outcome == TrialOutcome.Completed;
                        report.PerturbationTrials++;
                        if (success) report.PerturbationSuccesses++;
                    }
                    if (success && start == null) start = offset;
                    if (start != null && (!success || offset == radius))
                    {
                        report.TimingWindows.Add(new TimingWindow { JumpIndex = index,
                            FirstOffsetSeconds = start.Value / (float)request.FramesPerSecond,
                            LastOffsetSeconds = (success ? offset : offset - 1) / (float)request.FramesPerSecond });
                        start = null;
                    }
                }
            }
            report.AnalysisComplete = true;
        }
    }
}
