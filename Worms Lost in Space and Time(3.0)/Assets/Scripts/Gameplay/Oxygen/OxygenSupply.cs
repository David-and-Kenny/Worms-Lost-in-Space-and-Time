using System;
namespace Worms.Gameplay
{
    /// <summary>Oxygen depletion keeps the legacy next-frame death boundary.</summary>
    public sealed class OxygenSupply
    {
        public float Current { get; private set; }
        public float Maximum { get; }
        public float BurnRate { get; }
        public float Fraction => Math.Max(0f, Math.Min(1f, Current / Maximum));
        private bool depletionNotified;
        public event Action<float> Changed;
        public event Action Depleted;
        public OxygenSupply(float current, float maximum, float burnRate)
        {
            if (!Finite(current)) throw new ArgumentOutOfRangeException(nameof(current));
            if (!Finite(maximum) || maximum <= 0f) throw new ArgumentOutOfRangeException(nameof(maximum));
            if (!Finite(burnRate) || burnRate < 0f) throw new ArgumentOutOfRangeException(nameof(burnRate));
            Current = current;
            Maximum = maximum;
            BurnRate = burnRate;
        }
        public void Tick(float deltaSeconds)
        {
            if (!Finite(deltaSeconds) || deltaSeconds < 0f) throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            if (depletionNotified) return;
            if (Current > 0f)
            {
                Current = (float)Math.Max(-float.MaxValue, (double)Current - (double)deltaSeconds * BurnRate);
                Changed?.Invoke(Fraction);
            }
            else
            {
                depletionNotified = true;
                Depleted?.Invoke();
            }
        }
        public void Refill()
        {
            Current = 10f;
            depletionNotified = false;
            Changed?.Invoke(Fraction);
        }
        private static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
    }
}
