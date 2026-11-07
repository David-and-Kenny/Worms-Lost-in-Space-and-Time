using NUnit.Framework;

namespace Worms.LevelValidation
{
    public sealed class HeldInputPolicyTests
    {
        private static void Add(InputReplay replay, int ordinal, int attached, int launched)
        {
            string id = "wheel-" + ordinal;
            replay.Expected.Add(new TraversalSample { Kind = "Attached", Frame = attached, AttachmentOrdinal = ordinal, SurfaceId = id });
            replay.Jumps.Add(new JumpCommand { Frame = launched, SurfaceId = id, AttachmentOrdinal = ordinal });
        }

        [Test]
        public void ConsecutiveImmediateJumpsCanShareOnePreparedHold()
        {
            var replay = new InputReplay(); Add(replay, 1, 10, 11); Add(replay, 2, 30, 31); Add(replay, 3, 50, 51);
            var groups = HeldInputPolicy.Build(replay);
            Assert.That(groups.Count, Is.EqualTo(1));
            Assert.That(groups[0].Preheld, Is.True);
            Assert.That(groups[0].StartFrame, Is.Zero);
            Assert.That(groups[0].EndFrame, Is.EqualTo(51));
            Assert.That(groups[0].LastJump, Is.EqualTo(2));
        }

        [Test]
        public void ADelayedJumpRequiresReleaseAndAnotherPress()
        {
            var replay = new InputReplay(); Add(replay, 1, 10, 15); Add(replay, 2, 30, 35); Add(replay, 3, 50, 51);
            var groups = HeldInputPolicy.Build(replay);
            Assert.That(groups.Count, Is.EqualTo(2));
            Assert.That(groups[0].EndFrame, Is.EqualTo(15));
            Assert.That(groups[1].StartFrame, Is.EqualTo(35));
            Assert.That(groups[1].LastJump, Is.EqualTo(2));
            Assert.That(groups.TrueForAll(item => !item.Preheld), Is.True);
        }

        [Test]
        public void ForcedDetachBreaksAHoldButAllowsPreparingTheNextPressInFlight()
        {
            var replay = new InputReplay(); Add(replay, 1, 10, 15);
            replay.Expected.Add(new TraversalSample { Kind = "ForcedDetach", Frame = 40, AttachmentOrdinal = 2 });
            Add(replay, 3, 60, 61);
            var groups = HeldInputPolicy.Build(replay);
            Assert.That(groups.Count, Is.EqualTo(2));
            Assert.That(groups[1].StartFrame, Is.EqualTo(41));
            Assert.That(groups[1].Preheld, Is.True);
        }

        [Test]
        public void MissingAttachmentEvidenceDoesNotInventPreparedInput()
        {
            var replay = new InputReplay();
            replay.Jumps.Add(new JumpCommand { Frame = 10, AttachmentOrdinal = 1, SurfaceId = "unknown" });
            var group = HeldInputPolicy.Build(replay)[0];
            Assert.That(group.StartFrame, Is.EqualTo(10));
            Assert.That(group.Preheld, Is.False);
            Assert.That(HeldInputPolicy.Build(new InputReplay()), Is.Empty);
        }
    }
}
