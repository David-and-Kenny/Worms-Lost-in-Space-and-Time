using NUnit.Framework;
using Worms.Gameplay;

public sealed class JumpInputBufferTests
{
    [Test]
    public void ObservedBriefPressSurvivesReleaseAndIsConsumedOnce()
    {
        var input = new JumpInputBuffer();
        input.Sample(true);
        input.Sample(false);
        Assert.That(input.Consume(true), Is.True);
        Assert.That(input.Consume(true), Is.False);
    }

    [Test]
    public void HeldInputSurvivesFlightAndJumpsOnAttachment()
    {
        var input = new JumpInputBuffer();
        input.Sample(true);
        Assert.That(input.Consume(false), Is.False);
        Assert.That(input.Consume(true), Is.True);
        Assert.That(input.Consume(true), Is.True);
        input.Sample(false);
        Assert.That(input.Consume(true), Is.False);
    }

    [Test]
    public void ReleasedAirborneTapDoesNotWaitForALaterLanding()
    {
        var input = new JumpInputBuffer();
        input.Sample(true);
        input.Sample(false);
        Assert.That(input.Consume(false), Is.False);
        Assert.That(input.Consume(true), Is.False);
    }

    [Test]
    public void ClearingDropsPendingAndHeldInput()
    {
        var input = new JumpInputBuffer();
        input.Sample(true);
        input.Clear();
        Assert.That(input.Consume(true), Is.False);
        input.Sample(true);
        Assert.That(input.Consume(true), Is.True);
    }
}
