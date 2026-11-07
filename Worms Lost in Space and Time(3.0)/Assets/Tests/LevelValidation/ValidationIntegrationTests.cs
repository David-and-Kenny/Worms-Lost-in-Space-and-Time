#if UNITY_EDITOR
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Worms.LevelValidation
{
    public sealed class ValidationIntegrationTests
    {
        private ValidationEnvironment environment;
        [SetUp]
        public void SetUp() { environment = new ValidationEnvironment(); }
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return SceneManager.LoadSceneAsync("Splash");
            yield return null;
            environment.Dispose();
        }
        private static LevelManifest Fixture(string name) => JsonUtility.FromJson<LevelManifest>(File.ReadAllText(FixtureAuthoring.Folder + name + ".json"));
        private static LevelValidationRequest Options() => new LevelValidationRequest { OutputDirectory = "unused", SearchSeconds = 30, TrialSeconds = 8, MaximumTrials = 1000 };
        private sealed class NoJump : Worms.Gameplay.IPlayerInput { public bool JumpHeld => false; }

        [UnityTest]
        public IEnumerator RotatingWheelAndPlayerStayPhaseLocked()
        {
            var player = new GameObject("Phase player").AddComponent<Player>();
            player.GetComponent<Rigidbody2D>().gravityScale = 0;
            player.InputSource = new NoJump();
            var wheel = new GameObject("Phase wheel").AddComponent<Wheel>();
            wheel.speed = 80;
            wheel.holder = new GameObject("Holder").transform;
            wheel.holder.SetParent(wheel.transform);
            yield return new WaitForFixedUpdate();
            player.AttachToWheel(wheel, Vector2.right);
            for (int frame = 0; frame < 30; frame++)
            {
                yield return new WaitForFixedUpdate();
                Assert.That(Mathf.Abs(Mathf.DeltaAngle(player.GetComponent<Rigidbody2D>().rotation, wheel.holder.eulerAngles.z - 90)), Is.LessThan(.05f));
                Assert.That(Vector2.Distance(player.GetComponent<Rigidbody2D>().position, wheel.holder.position), Is.LessThan(.001f));
            }
            Object.Destroy(player.gameObject); Object.Destroy(wheel.gameObject);
        }

        [Test]
        public void EnvironmentRestoresClockPhysicsAndRandomState()
        {
            float capture = Time.captureDeltaTime, scale = Time.timeScale, step = Time.fixedDeltaTime;
            var mode = Physics2D.simulationMode;
            int rate = Application.targetFrameRate, vSync = QualitySettings.vSyncCount;
            var random = UnityEngine.Random.state;
            float expected = UnityEngine.Random.value;
            UnityEngine.Random.state = random;
            using (new ValidationEnvironment())
            {
                Time.captureDeltaTime = .1f; Physics2D.simulationMode = SimulationMode2D.Script;
                UnityEngine.Random.InitState(999);
            }
            Assert.That(Time.captureDeltaTime, Is.EqualTo(capture));
            Assert.That(Time.timeScale, Is.EqualTo(scale));
            Assert.That(Time.fixedDeltaTime, Is.EqualTo(step));
            Assert.That(Physics2D.simulationMode, Is.EqualTo(mode));
            Assert.That(Application.targetFrameRate, Is.EqualTo(rate));
            Assert.That(QualitySettings.vSyncCount, Is.EqualTo(vSync));
            Assert.That(UnityEngine.Random.value, Is.EqualTo(expected));
            UnityEngine.Random.state = random;
        }

        [UnityTest]
        public IEnumerator PhysicalSearchCertifiesARealFinishAndNormalPhysicsReplay()
        {
            var options = Options(); LevelValidator validator = null;
            var runner = new UnityTrialRunner(options, () => validator.BudgetExpired);
            var automatic = new UnityTrialRunner(options, () => validator.BudgetExpired);
            validator = new LevelValidator(runner, options, automatic);
            LevelValidationReport report = null;
            yield return validator.Evaluate(Fixture("StaticRoute"), null, value => report = value);
            Assert.That(report.Outcome, Is.EqualTo(ValidationOutcome.VerifiedReachable), report.Reason);
            Assert.That(report.Replay.Jumps.Count, Is.GreaterThan(0));
            Assert.That(report.AnalysisComplete, Is.True);
            Assert.That(report.TimingWindows, Is.Not.Empty);
            Assert.That(report.Profiles.Count, Is.EqualTo(3));
            Assert.That(report.Profiles.TrueForAll(profile => profile.Completed), Is.True, "The fixed-tick policy must complete at every render profile.");
            Assert.That(report.HeldInput.Complete && report.HeldInput.Verified, Is.True, report.HeldInput.Reason);
            Assert.That(report.HeldInput.Profiles.Count, Is.EqualTo(3));
            Assert.That(report.HeldInput.Groups.Count, Is.EqualTo(1));
            options.Operation = "replay";
            LevelValidationReport repeated = null;
            yield return validator.Evaluate(Fixture("StaticRoute"), report.Replay, value => repeated = value);
            Assert.That(repeated.Outcome, Is.EqualTo(ValidationOutcome.VerifiedReachable), repeated.Reason);
            foreach (int rate in new[] { 30, 120 })
            {
                TrialResult crossRate = null;
                yield return runner.Run(Fixture("StaticRoute"), new TrialPlan { Jumps = report.Replay.Jumps }, rate, value => crossRate = value);
                var expected = new TrialResult { Outcome = TrialOutcome.Completed, Events = report.Replay.Expected };
                Assert.That(ReplayComparison.Equivalent(expected, crossRate, out string difference), Is.True, rate + " FPS: " + difference);
                Assert.That(crossRate.SaveWrites, Is.EqualTo(1));
            }
        }
        [UnityTest]
        public IEnumerator OnePreparedHoldTraversesTwoRealAttachmentsAndRejectsAnEarlyExtraLaunch()
        {
            var options = Options(); options.SearchSeconds = 60;
            LevelValidator validator = null;
            var runner = new UnityTrialRunner(options, () => validator.BudgetExpired);
            validator = new LevelValidator(runner, options);
            LevelValidationReport report = null;
            yield return validator.Evaluate(Fixture("HeldChain"), null, value => report = value);
            Assert.That(report.Outcome, Is.EqualTo(ValidationOutcome.VerifiedReachable), report.Reason);
            Assert.That(report.Replay.Jumps.Count, Is.EqualTo(2));
            Assert.That(report.HeldInput.Verified, Is.True, report.HeldInput.Reason);
            Assert.That(report.HeldInput.Groups.Count, Is.EqualTo(1));
            Assert.That(report.HeldInput.Groups[0].Preheld, Is.True);
            var wrong = new TrialPlan { Jumps = RouteSearch.Copy(new TrialPlan { Jumps = report.Replay.Jumps }).Jumps,
                ButtonHolds = report.HeldInput.Groups };
            wrong.Jumps[0].Frame += 2;
            TrialResult failed = null;
            yield return runner.Run(Fixture("HeldChain"), wrong, 60, value => failed = value);
            Assert.That(failed.Outcome, Is.EqualTo(TrialOutcome.Diverged));
            Assert.That(failed.SaveWrites, Is.Zero);
        }

        [UnityTest]
        public IEnumerator SealedPhysicalExitReturnsInconclusiveWithinTrialBudget()
        {
            var options = Options(); options.MaximumTrials = 20;
            LevelValidator validator = null;
            validator = new LevelValidator(new UnityTrialRunner(options, () => validator.BudgetExpired), options);
            LevelValidationReport report = null;
            yield return validator.Evaluate(Fixture("SealedExit"), null, value => report = value);
            Assert.That(report.Outcome, Is.EqualTo(ValidationOutcome.Inconclusive), report.Reason);
            Assert.That(report.Replay, Is.Null);
        }
        [UnityTest]
        public IEnumerator OxygenDeathAndBreakableDetachUseRealComponentsAndNeverSave()
        {
            var options = Options(); var runner = new UnityTrialRunner(options);
            TrialResult frontier = null;
            yield return runner.Run(Fixture("BreakableWheel"), new TrialPlan { StopAtFrontier = true }, 60, value => frontier = value);
            var countdown = UnityEngine.Object.FindFirstObjectByType<CountdownExplode>();
            Assert.That(frontier.Outcome, Is.EqualTo(TrialOutcome.Frontier));
            Assert.That(frontier.SafeWaitSeconds, Is.EqualTo(countdown.timer).Within(.0001f));
            TrialResult result = null;
            yield return runner.Run(Fixture("BreakableWheel"), new TrialPlan(), 60, value => result = value);
            Assert.That(result.Outcome, Is.EqualTo(TrialOutcome.Dead));
            Assert.That(result.Events.Exists(item => item.Kind == "ForcedDetach"), Is.True);
            Assert.That(result.SaveWrites, Is.Zero);
        }
        [UnityTest]
        public IEnumerator CancellationTerminatesWithoutAReachabilityClaim()
        {
            var runner = new UnityTrialRunner(Options(), () => true);
            TrialResult result = null;
            yield return runner.Run(Fixture("StaticRoute"), new TrialPlan(), 60, value => result = value);
            Assert.That(result.Outcome, Is.EqualTo(TrialOutcome.Cancelled));
            Assert.That(result.SaveWrites, Is.Zero);
        }
        [UnityTest]
        public IEnumerator MovingHazardCalibrationAndNarrowTimingAreSupported()
        {
            var options = Options(); var runner = new UnityTrialRunner(options);
            TrialResult first = null;
            for (int repeat = 0; repeat < 10; repeat++)
            {
                TrialResult next = null;
                yield return runner.Run(Fixture("MovingHazard"), new TrialPlan(), 60, value => next = value);
                if (first == null) first = next;
                else Assert.That(ReplayComparison.Equivalent(first, next, out string reason), Is.True, reason);
            }
            LevelValidator validator = null;
            validator = new LevelValidator(new UnityTrialRunner(options, () => validator.BudgetExpired), options,
                new UnityTrialRunner(options, () => validator.BudgetExpired));
            LevelValidationReport report = null;
            yield return validator.Evaluate(Fixture("NarrowExit"), null, value => report = value);
            Assert.That(report.Outcome, Is.EqualTo(ValidationOutcome.VerifiedReachable), report.Reason);
            Assert.That(report.TimingWindows, Is.Not.Empty);
            Assert.That(report.PerturbationSuccesses, Is.LessThan(report.PerturbationTrials));
        }
        [UnityTest]
        public IEnumerator DirectFinishNeedsNoArtificialJumpOrPlatformRequirement()
        {
            var options = Options(); LevelValidator validator = null;
            validator = new LevelValidator(new UnityTrialRunner(options, () => validator.BudgetExpired), options,
                new UnityTrialRunner(options, () => validator.BudgetExpired));
            LevelValidationReport report = null;
            yield return validator.Evaluate(Fixture("DirectExit"), null, value => report = value);
            Assert.That(report.Outcome, Is.EqualTo(ValidationOutcome.VerifiedReachable), report.Reason);
            Assert.That(report.Replay.Jumps, Is.Empty);
        }
    }
}
#endif
