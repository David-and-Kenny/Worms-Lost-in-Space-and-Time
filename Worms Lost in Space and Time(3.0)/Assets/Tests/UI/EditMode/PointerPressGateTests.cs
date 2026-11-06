using NUnit.Framework;
using Worms.Core;
using Worms.Gameplay;
using Worms;

public sealed class PointerPressGateTests
{
    private sealed class InputState : IPlayerInputState
    {
        public bool KeyboardJumpHeld { get; set; }
        public bool PointerHeld { get; set; }
        public bool TouchHeld { get; set; }
        public bool PointerOverControl { get; set; }
    }

    [Test]
    public void NavigationConsumesHeldTouchUntilReleaseAndPreservesNextHeldPress()
    {
        var gate = new PointerPressGate();
        gate.BlockUntilRelease();
        Assert.That(gate.Sample(true, false), Is.False);
        Assert.That(gate.Sample(true, false), Is.False);
        Assert.That(gate.Sample(false, false), Is.False);
        Assert.That(gate.Sample(true, false), Is.True);
        Assert.That(gate.Sample(true, false), Is.True);
    }

    [Test]
    public void CompositionResetKeepsExistingInputAdaptersOnTheSharedGate()
    {
        var original = GameServices.PointerGate;
        original.BlockUntilRelease();
        GameServices.ResetForTests();
        Assert.That(GameServices.PointerGate, Is.SameAs(original));
        Assert.That(original.Sample(true, false), Is.True);
    }

    [Test]
    public void TouchBeginningOnControlStaysConsumedAfterDraggingAway()
    {
        var gate = new PointerPressGate();
        Assert.That(gate.Sample(true, true), Is.False);
        Assert.That(gate.Sample(true, false), Is.False);
        gate.Sample(false, false);
        Assert.That(gate.Sample(true, false), Is.True);
    }

    [Test]
    public void PlayerInputKeepsHeldKeyboardWhileObservingPointerRelease()
    {
        var state = new InputState { PointerHeld = true, TouchHeld = true, KeyboardJumpHeld = true };
        var gate = new PointerPressGate();
        gate.BlockUntilRelease();
        var input = new PlayerInput(state, gate);
        Assert.That(input.JumpHeld, Is.True, "Space remains a held signal while touch is suppressed.");
        state.PointerHeld = state.TouchHeld = false;
        Assert.That(input.JumpHeld, Is.True, "Keyboard must not prevent observing pointer release.");
        state.KeyboardJumpHeld = false;
        state.PointerHeld = state.TouchHeld = true;
        Assert.That(input.JumpHeld, Is.True);
    }
}
