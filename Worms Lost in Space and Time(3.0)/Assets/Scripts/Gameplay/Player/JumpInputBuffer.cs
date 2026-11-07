namespace Worms.Gameplay
{
    /// <summary>Retains an observed tap until physics consumes it; holding still jumps on attachment.</summary>
    public sealed class JumpInputBuffer
    {
        private bool held;
        private bool pressed;

        public void Sample(bool jumpHeld)
        {
            pressed |= jumpHeld && !held;
            held = jumpHeld;
        }

        public bool Consume(bool attached)
        {
            bool jump = attached && (pressed || held);
            pressed = false;
            return jump;
        }

        public void Clear()
        {
            held = false;
            pressed = false;
        }
    }

    /// <summary>Already observed commands scheduled on simulation ticks, independent of rendering.</summary>
    public interface IPlayerTickCommands
    {
        bool JumpRequested(int tick);
    }
}
