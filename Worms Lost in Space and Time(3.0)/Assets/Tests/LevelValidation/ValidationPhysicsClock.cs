using UnityEngine;

namespace Worms.LevelValidation
{
    /// <summary>Test-only phase clock. Physics precedes gameplay Update; all real behaviours still run.</summary>
    [DefaultExecutionOrder(-32000)]
    public sealed class ValidationPhysicsClock : MonoBehaviour
    {
        public int FramesPerSecond = 60;
        public int Frame { get; private set; } = -1;
        private int accumulatedTicks;
        private void Update()
        {
            Frame++;
            // Integer ratio avoids carrying Unity's global fixed-step remainder between reloads.
            accumulatedTicks += 50;
            while (accumulatedTicks >= FramesPerSecond)
            {
                Physics2D.SyncTransforms();
                Physics2D.Simulate(.02f);
                accumulatedTicks -= FramesPerSecond;
            }
        }
    }
}
