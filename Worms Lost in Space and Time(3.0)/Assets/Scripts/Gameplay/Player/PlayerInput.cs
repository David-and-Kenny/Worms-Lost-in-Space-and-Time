using UnityEngine;
using Worms.Core;
using Worms.Input;

namespace Worms.Gameplay
{
    public interface IPlayerInput { bool JumpHeld { get; } }

    public interface IPlayerInputState
    {
        bool KeyboardJumpHeld { get; }
        bool PointerHeld { get; }
        bool TouchHeld { get; }
        bool PointerOverControl { get; }
    }

    public sealed class PlayerInput : IPlayerInput
    {
        private readonly IPlayerInputState state;
        private readonly PointerPressGate pointerGate;

        public PlayerInput() : this(new UnityPlayerInputState(), GameServices.PointerGate) { }

        public PlayerInput(IPlayerInputState state, PointerPressGate pointerGate)
        {
            this.state = state ?? throw new System.ArgumentNullException(nameof(state));
            this.pointerGate = pointerGate ?? throw new System.ArgumentNullException(nameof(pointerGate));
        }

        public bool JumpHeld
        {
            get
            {
                bool pointerAllowed = pointerGate.Sample(state.PointerHeld, state.PointerOverControl);
                return state.KeyboardJumpHeld || (state.TouchHeld && pointerAllowed);
            }
        }

        private sealed class UnityPlayerInputState : IPlayerInputState
        {
            public bool KeyboardJumpHeld => UnityEngine.Input.GetKey(KeyCode.Space);
            public bool PointerHeld => PointerInput.Held;
            public bool TouchHeld => PointerInput.TouchHeld;
            public bool PointerOverControl => PointerInput.OverControl;
        }
    }
}
