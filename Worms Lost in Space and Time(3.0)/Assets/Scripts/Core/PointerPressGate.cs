namespace Worms.Core
{
    /// <summary>A pointer used by a menu remains consumed until all pointers are released.</summary>
    public sealed class PointerPressGate
    {
        private bool waitingForRelease;

        public void BlockUntilRelease()
        {
            waitingForRelease = true;
        }

        public void Reset()
        {
            waitingForRelease = false;
        }

        public bool Sample(bool pointerHeld, bool pointerOverControl)
        {
            if (!pointerHeld) waitingForRelease = false;
            else if (pointerOverControl) waitingForRelease = true;
            return pointerHeld && !waitingForRelease;
        }
    }
}
