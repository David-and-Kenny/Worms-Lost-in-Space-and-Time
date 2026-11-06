using System;

namespace Worms.UI
{
    /// <summary>A press reveals text; a released, later press advances. Held input never advances twice.</summary>
    public sealed class StoryFlow
    {
        private readonly string text;
        private readonly float letterSeconds;
        private float elapsed;
        private float revealedAt = float.NegativeInfinity;
        private bool held;
        private int visible;
        public string VisibleText => text.Substring(0, visible);
        public bool IsRevealed => visible == text.Length;
        public bool HasAdvanced { get; private set; }
        public StoryFlow(string text, float letterSeconds)
        {
            this.text = text ?? string.Empty;
            this.letterSeconds = Math.Max(0.001f, letterSeconds);
        }
        public bool Update(float seconds, bool pressed)
        {
            elapsed += Math.Max(0, seconds);
            visible = Math.Max(visible, Math.Min(text.Length, (int)(elapsed / letterSeconds)));
            bool edge = pressed && !held;
            held = pressed;
            if (!edge || HasAdvanced) return false;
            if (!IsRevealed)
            {
                visible = text.Length;
                revealedAt = elapsed;
                return false;
            }
            if (elapsed - revealedAt < 0.15f) return false;
            HasAdvanced = true;
            return true;
        }
    }
}
