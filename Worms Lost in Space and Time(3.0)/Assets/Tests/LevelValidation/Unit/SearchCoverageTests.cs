using System.Linq;
using NUnit.Framework;

namespace Worms.LevelValidation
{
    public sealed class SearchCoverageTests
    {
        private static LevelManifest Map()
        {
            var map = new LevelManifest();
            map.Objects.Add(new SceneObjectIdentity { Id = "source", Role = "wheel", AngularSpeed = 90, SizeX = 2, SizeY = 2 });
            map.Objects.Add(new SceneObjectIdentity { Id = "exit", Role = "finish", X = 10, CenterX = 10, SizeX = 1, SizeY = 1 });
            return map;
        }

        [Test]
        public void PreferredActionsRetainEveryTickAndBothPassiveHorizons()
        {
            var map = Map();
            var schedule = new LaunchSchedule(new TrialResult { X = 1, Angle = -90 }, map.Objects[0], new RouteGeometry(map), 450);
            Assert.That(schedule.Delays.Where(delay => delay >= 0).OrderBy(delay => delay), Is.EqualTo(Enumerable.Range(0, 451)));
            Assert.That(schedule.Delays.Count(delay => delay < 0), Is.EqualTo(2));
            Assert.That(schedule.Delays[0], Is.EqualTo(-1));
        }

        [Test]
        public void DestinationsOnOppositeSidesBothGetEarlyLaunchCandidates()
        {
            var map = Map();
            map.Objects.Add(new SceneObjectIdentity { Id = "retreat", Role = "wheel", X = -10, CenterX = -10, SizeX = 2, SizeY = 2 });
            var schedule = new LaunchSchedule(new TrialResult { X = 1, Angle = -90 }, map.Objects[0], new RouteGeometry(map), 250);
            Assert.That(schedule.Delays.Take(10), Does.Contain(0));
            Assert.That(schedule.Delays.Take(10), Does.Contain(100), "A platform away from the exit must remain an early choice.");
        }

        [Test]
        public void ProbeLimitDoesNotReplaceTheLongerFallback()
        {
            var options = new LevelValidationRequest { TrialSeconds = 120 };
            var search = new RouteSearch(options, Map());
            search.Add(new TrialPlan(), new TrialResult { Outcome = TrialOutcome.Frontier, SurfaceId = "source", LastFrame = 10, AttachmentFrame = 5 });
            var shortProbe = search.Next();
            Assert.That(shortProbe.FrameLimit, Is.LessThan(6000));
            bool fullPassive = false;
            for (int index = 0; index < 100; index++)
            {
                var next = search.Next();
                if (next.Jumps.Count == 0 && next.FrameLimit == -1) { fullPassive = true; break; }
            }
            Assert.That(fullPassive, Is.True);
        }

        [Test]
        public void AFlightThatNeedsMoreTimeIsRetriedWithoutItsShortProbeLimit()
        {
            var search = new RouteSearch(new LevelValidationRequest { TrialSeconds = 120 }, Map());
            var probe = new TrialPlan { StopAtFrontier = true, FrameLimit = 100 };
            probe.Jumps.Add(new JumpCommand { Frame = 10 });
            search.Add(probe, new TrialResult { Outcome = TrialOutcome.TimedOut, LastFrame = 100 });
            var retry = search.Next();
            Assert.That(retry.FrameLimit, Is.EqualTo(-1));
            Assert.That(retry.Jumps.Single().Frame, Is.EqualTo(10));
        }

        [Test]
        public void OnlyOutwardFlightsBeyondTheEnvelopeAreShortened()
        {
            var geometry = new RouteGeometry(Map());
            Assert.That(geometry.OutsideSearchEnvelope(new TrialResult { X = 100, VelocityX = 10 }), Is.True);
            Assert.That(geometry.OutsideSearchEnvelope(new TrialResult { X = 100, VelocityX = -10 }), Is.False);
            Assert.That(geometry.OutsideSearchEnvelope(new TrialResult { X = 5, VelocityX = 10 }), Is.False);
        }

        [Test]
        public void DeferredFlightsRemainAvailableWhenTheLastFrontierIsExhausted()
        {
            var search = new RouteSearch(new LevelValidationRequest { TrialSeconds = 120 }, Map());
            search.Add(new TrialPlan(), new TrialResult { Outcome = TrialOutcome.Frontier, SurfaceId = "source", LastFrame = 5999 });
            var shortProbe = new TrialPlan { FrameLimit = 100 };
            shortProbe.Jumps.Add(new JumpCommand { Frame = 10 });
            search.Add(shortProbe, new TrialResult { Outcome = TrialOutcome.TimedOut, LastFrame = 100 });
            bool retried = false;
            TrialPlan next;
            while ((next = search.Next()) != null)
                if (next.Jumps.Count == 1 && next.Jumps[0].Frame == 10 && next.FrameLimit == -1) retried = true;
            Assert.That(retried, Is.True);
        }

        [Test]
        public void LongThinObstaclesDoNotBecomeGiantCircularOccluders()
        {
            var map = Map();
            map.Objects.Add(new SceneObjectIdentity { Id = "floor", Role = "hazard", X = 5, Y = -4,
                CenterX = 5, CenterY = -4, SizeX = 100, SizeY = .2f });
            Assert.That(new RouteGeometry(map).ExitCost("source", 0, 0), Is.EqualTo(10).Within(.001));
        }

        [Test]
        public void ExitCostsCanPreferASidewaysPlatformAroundAStaticObstacle()
        {
            var map = Map();
            map.Objects.Add(new SceneObjectIdentity { Id = "wall", Role = "obstacle", X = 5, CenterX = 5, SizeX = .2f, SizeY = 2 });
            map.Objects.Add(new SceneObjectIdentity { Id = "detour", Role = "wheel", Y = 5, CenterY = 5, SizeX = 2, SizeY = 2 });
            double cost = new RouteGeometry(map).ExitCost("source", 0, 0);
            Assert.That(cost, Is.GreaterThan(10).And.LessThan(20));
        }

        [Test]
        public void FreshArrivalsOnAnEarlyPlatformCannotStarveAnotherPlatform()
        {
            var map = Map();
            map.Objects.Add(new SceneObjectIdentity { Id = "other", Role = "wheel", X = -3, CenterX = -3, AngularSpeed = 90 });
            var search = new RouteSearch(new LevelValidationRequest(), map);
            search.Add(new TrialPlan(), new TrialResult { Outcome = TrialOutcome.Frontier,
                SurfaceId = "other", X = -3, LastFrame = 1000 });
            bool explored = false;
            for (int index = 0; index < 100; index++)
            {
                search.Add(new TrialPlan(), new TrialResult { Outcome = TrialOutcome.Frontier,
                    SurfaceId = "source", LastFrame = index + 10 });
                if (search.Next().ObserveAfterFrame == 1000) { explored = true; break; }
            }
            Assert.That(explored, Is.True, "Effort belongs to the platform, not just the newest arrival.");
        }

        [Test]
        public void LandingDiversityRetainsSubTickAnglePhaseAndDifferentHazardTimes()
        {
            var surface = new SceneObjectIdentity { Id = "wheel", AngularSpeed = 300 };
            var first = new TrialResult { SurfaceId = "wheel", X = 1, Angle = 1, LastFrame = 10 };
            var sameGrid = new TrialResult { SurfaceId = "wheel", X = 1, Angle = 7, LastFrame = 10 };
            var differentGrid = new TrialResult { SurfaceId = "wheel", X = 1, Angle = 3, LastFrame = 10 };
            var differentTime = new TrialResult { SurfaceId = "wheel", X = 1, Angle = 1, LastFrame = 40 };
            Assert.That(LandingDiversity.Cell(first, surface), Is.EqualTo(LandingDiversity.Cell(sameGrid, surface)));
            Assert.That(LandingDiversity.Cell(first, surface), Is.Not.EqualTo(LandingDiversity.Cell(differentGrid, surface)));
            Assert.That(LandingDiversity.Cell(first, surface), Is.Not.EqualTo(LandingDiversity.Cell(differentTime, surface)));
        }
    }
}
