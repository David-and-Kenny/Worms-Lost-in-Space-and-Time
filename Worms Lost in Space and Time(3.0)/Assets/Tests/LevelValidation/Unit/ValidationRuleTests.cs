using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Worms.LevelValidation
{
    public sealed class ValidationRuleTests
    {
        private sealed class FakeRunner : ILevelTrialRunner
        {
            public int Calls;
            public int Completions;
            public bool Sealed;
            public bool Unstable;
            public int[] WinningFrames = { 7 };
            public List<int> CommandCounts = new List<int>();
            public IEnumerator Run(LevelManifest manifest, TrialPlan plan, int rate, Action<TrialResult> receive)
            {
                Calls++;
                CommandCounts.Add(plan.Jumps.Count);
                var result = new TrialResult { Outcome = TrialOutcome.Dead, LastFrame = 30 };
                if (Unstable && Calls % 2 == 0) result.Outcome = TrialOutcome.TimedOut;
                else if (!Sealed && plan.Jumps.Count == 1 && WinningFrames.Contains(plan.Jumps[0].Frame))
                { result.Outcome = TrialOutcome.Completed; result.SaveWrites = 1; result.Events.Add(new TraversalSample { Frame = 30, Kind = "Completed" }); Completions++; }
                else if (plan.StopAtFrontier && plan.Jumps.Count == 0 && plan.ObserveAfterFrame < 0)
                { result.Outcome = TrialOutcome.Frontier; result.LastFrame = 2; result.SurfaceId = "wheel"; result.AttachmentOrdinal = 1; result.AttachmentFrame = 0; }
                receive(result);
                yield break;
            }
        }
        private static LevelValidationRequest Request() => new LevelValidationRequest { OutputDirectory = "unused", SearchSeconds = 10, MaximumTrials = 1000, TrialSeconds = .5f };
        private static LevelManifest Manifest() => new LevelManifest { LevelId = "fixture", ScenePath = "Assets/fixture.unity", Fingerprint = "abc",
            Objects = new List<SceneObjectIdentity> { new SceneObjectIdentity { Id = "goal", Role = "finish", X = 4 } } };
        private static void Drain(IEnumerator iterator)
        {
            try { while (iterator.MoveNext()) if (iterator.Current is IEnumerator child) Drain(child); }
            finally { (iterator as IDisposable)?.Dispose(); }
        }

        [Test]
        public void SearchDiscoversRefinedTimingAndCertifiesWithFreshRuns()
        {
            var runner = new FakeRunner();
            LevelValidationReport report = null;
            Drain(new LevelValidator(runner, Request()).Evaluate(Manifest(), null, value => report = value));
            Assert.That(report.Outcome, Is.EqualTo(ValidationOutcome.VerifiedReachable));
            Assert.That(report.Replay.Jumps.Single().Frame, Is.EqualTo(7));
            Assert.That(runner.Completions, Is.GreaterThanOrEqualTo(4));
        }
        [Test]
        public void ExhaustedSearchDoesNotClaimImpossibility()
        {
            LevelValidationReport report = null;
            Drain(new LevelValidator(new FakeRunner { Sealed = true }, Request()).Evaluate(Manifest(), null, value => report = value));
            Assert.That(report.Outcome, Is.EqualTo(ValidationOutcome.Inconclusive));
            Assert.That(report.Reason, Does.Contain("not proof"));
        }
        [Test]
        public void MissingExitIsStructuralFailureBeforeSimulation()
        {
            var manifest = Manifest(); manifest.Errors.Add("No finish");
            var runner = new FakeRunner(); LevelValidationReport report = null;
            Drain(new LevelValidator(runner, Request()).Evaluate(manifest, null, value => report = value));
            Assert.That(report.Outcome, Is.EqualTo(ValidationOutcome.InvalidContent));
            Assert.That(runner.Calls, Is.Zero);
        }
        [Test]
        public void RecordedProposalIsRecertifiedRatherThanTrustingAnOldHash()
        {
            var old = new InputReplay { LevelId = "fixture", ScenePath = "Assets/fixture.unity", Fingerprint = "old", UnityVersion = Application.unityVersion };
            old.Expected.Add(new TraversalSample { Kind = "Completed" });
            old.Jumps.Add(new JumpCommand { Frame = 7, SurfaceId = "wheel", AttachmentOrdinal = 1, DelaySeconds = .1f });
            var runner = new FakeRunner(); LevelValidationReport report = null;
            Drain(new LevelValidator(runner, Request()).Evaluate(Manifest(), old, value => report = value));
            Assert.That(report.Outcome, Is.EqualTo(ValidationOutcome.VerifiedReachable));
            Assert.That(report.Replay.Fingerprint, Is.EqualTo("abc"));
            Assert.That(runner.Completions, Is.GreaterThanOrEqualTo(16));
            Assert.That(report.Replay.IsWellFormed(), Is.True);
        }
        [Test]
        public void ChangedFingerprintNeverReusesCertificate()
        {
            var request = Request(); request.Operation = "replay";
            var runner = new FakeRunner(); LevelValidationReport report = null;
            var replay = new InputReplay { LevelId = "fixture", Fingerprint = "old", UnityVersion = Application.unityVersion };
            Drain(new LevelValidator(runner, request).Evaluate(Manifest(), replay, value => report = value));
            Assert.That(report.Outcome, Is.EqualTo(ValidationOutcome.Stale));
            Assert.That(runner.Calls, Is.Zero);
        }
        [Test]
        public void NonrepeatableCalibrationWithholdsVerdict()
        {
            LevelValidationReport report = null;
            Drain(new LevelValidator(new FakeRunner { Unstable = true }, Request()).Evaluate(Manifest(), null, value => report = value));
            Assert.That(report.Outcome, Is.EqualTo(ValidationOutcome.RunnerUnstable));
        }
        [Test]
        public void ControlledClockSuccessAloneCannotIssueCertificate()
        {
            LevelValidationReport report = null;
            Drain(new LevelValidator(new FakeRunner(), Request(), new FakeRunner { Sealed = true }).Evaluate(Manifest(), null, value => report = value));
            Assert.That(report.Outcome, Is.EqualTo(ValidationOutcome.Inconclusive));
            Assert.That(report.Replay, Is.Null);
        }
        [Test]
        public void TrialBudgetIsAHardLimit()
        {
            var request = Request(); request.MaximumTrials = 12;
            var runner = new FakeRunner { Sealed = true }; LevelValidationReport report = null;
            Drain(new LevelValidator(runner, request).Evaluate(Manifest(), null, value => report = value));
            Assert.That(report.Outcome, Is.EqualTo(ValidationOutcome.Inconclusive));
            Assert.That(runner.Calls, Is.EqualTo(12));
        }
        [Test]
        public void SearchContinuesAfterAnAutomaticPhysicsCandidateFails()
        {
            LevelValidationReport report = null;
            var controlled = new FakeRunner { WinningFrames = new[] { 5, 8 } };
            var automatic = new FakeRunner { WinningFrames = new[] { 8 } };
            Drain(new LevelValidator(controlled, Request(), automatic).Evaluate(Manifest(), null, value => report = value));
            Assert.That(report.Outcome, Is.EqualTo(ValidationOutcome.VerifiedReachable));
            Assert.That(report.Replay.Jumps[0].Frame, Is.EqualTo(8));
            Assert.That(report.Diagnostics.Exists(item => item.StartsWith("Rejected candidate:")), Is.True);
        }
        [Test]
        public void MalformedCertificatesCannotEnterReplay()
        {
            var replay = new InputReplay();
            Assert.That(replay.IsWellFormed(), Is.False);
            replay.LevelId = "fixture"; replay.ScenePath = "Assets/fixture.unity";
            replay.Fingerprint = "abc"; replay.UnityVersion = Application.unityVersion;
            replay.Expected.Add(new TraversalSample { Kind = "Completed" });
            Assert.That(replay.IsWellFormed(), Is.True);
            replay.Jumps.Add(new JumpCommand { Frame = 2, SurfaceId = "wheel", AttachmentOrdinal = 1, DelaySeconds = float.NaN });
            Assert.That(replay.IsWellFormed(), Is.False);
            replay.Jumps[0].DelaySeconds = 0;
            replay.Jumps.Add(replay.Jumps[0]);
            Assert.That(replay.IsWellFormed(), Is.False);
        }
        [Test]
        public void PublishedScenePathAndCatalogIdResolveToTheSameIdentity()
        {
            var byId = SceneInspection.Inspect("level-01")[0];
            var byPath = SceneInspection.Inspect(byId.ScenePath)[0];
            Assert.That(byPath.LevelId, Is.EqualTo(byId.LevelId));
            Assert.That(byPath.Fingerprint, Is.EqualTo(byId.Fingerprint));
        }
        [Test]
        public void FingerprintIgnoresTextLineEndingsButDetectsContentChanges()
        {
            string temporarySource = "Assets/Scripts/ValidationFingerprint-" + Guid.NewGuid().ToString("N") + ".txt";
            const string scene = "Assets/Scenes/World1/Level1.unity";
            try
            {
                File.WriteAllText(temporarySource, "first\nsecond\n");
                string original = SceneInspection.Fingerprint(scene);
                File.WriteAllText(temporarySource, "first\r\nsecond\r\n");
                Assert.That(SceneInspection.Fingerprint(scene), Is.EqualTo(original));
                File.WriteAllText(temporarySource, "changed\nsecond\n");
                Assert.That(SceneInspection.Fingerprint(scene), Is.Not.EqualTo(original));
            }
            finally
            {
                File.Delete(temporarySource);
                if (File.Exists(temporarySource + ".meta")) File.Delete(temporarySource + ".meta");
            }
        }
        [Test]
        public void SearchEnumeratesCoarseThenEveryFrameWithoutMergingDifferentPrefixes()
        {
            var options = Request(); options.TrialSeconds = 1;
            var search = new RouteSearch(options, Manifest());
            search.Add(new TrialPlan(), new TrialResult { Outcome = TrialOutcome.Frontier, LastFrame = 2, SurfaceId = "wheel", AttachmentOrdinal = 1 });
            var frames = new List<int>(); TrialPlan next;
            while ((next = search.Next()) != null) if (next.Jumps.Count > 0) frames.Add(next.Jumps[0].Frame);
            Assert.That(frames.Take(3), Is.EqualTo(new[] { 2, 5, 8 }));
            Assert.That(frames.OrderBy(value => value), Is.EqualTo(Enumerable.Range(2, 59)));
        }
        [Test]
        public void ReplayComparisonDetectsTimingVelocityAndOxygenDrift()
        {
            var first = new TrialResult { Outcome = TrialOutcome.Completed, Events = new List<TraversalSample> { new TraversalSample { Angle = 359.99f } } };
            var second = new TrialResult { Outcome = TrialOutcome.Completed, Events = new List<TraversalSample> { new TraversalSample { Angle = .01f } } };
            Assert.That(ReplayComparison.Equivalent(first, second, out _), Is.True);
            second.Events[0].VelocityX = .1f;
            Assert.That(ReplayComparison.Equivalent(first, second, out _), Is.False);
            second.Events[0].VelocityX = 0; second.Events[0].Oxygen = 1;
            Assert.That(ReplayComparison.Equivalent(first, second, out _), Is.False);
            second.Events[0].Oxygen = 0; second.Events[0].Frame = 1;
            Assert.That(ReplayComparison.Equivalent(first, second, out _), Is.False);
        }
        [Test]
        public void ContactDiagnosticsAreSeparateFromAuthoritativeAttachmentCheckpoints()
        {
            var first = new TrialResult { Outcome = TrialOutcome.Completed, Events = new List<TraversalSample> {
                new TraversalSample { Kind = "Attached", Frame = 1 }, new TraversalSample { Kind = "AttachmentCheckpoint", Frame = 3 } } };
            var second = new TrialResult { Outcome = TrialOutcome.Completed, Events = new List<TraversalSample> {
                new TraversalSample { Kind = "Attached", Frame = 1, X = .003f }, new TraversalSample { Kind = "AttachmentCheckpoint", Frame = 3 } } };
            Assert.That(ReplayComparison.Equivalent(first, second, out _), Is.True);
            second.Events[1].X = .003f;
            Assert.That(ReplayComparison.Equivalent(first, second, out _), Is.False);
        }
        [Test]
        public void SearchDoesNotApplyProposalFramesFromADifferentRate()
        {
            var request = Request(); request.FramesPerSecond = 30;
            var saved = new InputReplay { LevelId = "fixture", ScenePath = "Assets/fixture.unity", Fingerprint = "old", UnityVersion = Application.unityVersion, FramesPerSecond = 60 };
            saved.Expected.Add(new TraversalSample { Kind = "Completed" });
            saved.Jumps.Add(new JumpCommand { Frame = 7, SurfaceId = "wheel", AttachmentOrdinal = 1, DelaySeconds = .1f });
            var runner = new FakeRunner(); LevelValidationReport report = null;
            Drain(new LevelValidator(runner, request).Evaluate(Manifest(), saved, value => report = value));
            Assert.That(runner.CommandCounts.Take(10).All(count => count == 0), Is.True);
            Assert.That(report.Replay.FramesPerSecond, Is.EqualTo(30));
        }
        [Test]
        public void AimHintsPrioritizePromisingAnglesWithoutRemovingOtherFrames()
        {
            var request = Request(); request.TrialSeconds = 2;
            var manifest = Manifest(); manifest.Objects[0].X = 0; manifest.Objects[0].Y = 4;
            manifest.Objects.Add(new SceneObjectIdentity { Id = "wheel", Role = "wheel", AngularSpeed = 90 });
            var search = new RouteSearch(request, manifest);
            search.Add(new TrialPlan(), new TrialResult { Outcome = TrialOutcome.Frontier, SurfaceId = "wheel", X = 1, Angle = -90 });
            Assert.That(search.Next().Jumps, Is.Empty, "Passive continuation remains an option.");
            var frames = new List<int>(); TrialPlan plan;
            while ((plan = search.Next()) != null) frames.Add(plan.Jumps[0].Frame);
            Assert.That(frames.Take(2), Does.Contain(60));
            Assert.That(frames.OrderBy(frame => frame), Is.EqualTo(Enumerable.Range(0, 121)));
        }
        [TestCase(0)] [TestCase(-1)] [TestCase(1000)]
        public void InvalidTimingProfilesAreRejected(int rate)
        {
            var request = Request(); request.FramesPerSecond = rate;
            Assert.Throws<ArgumentException>(() => request.Validate());
        }
    }
}
