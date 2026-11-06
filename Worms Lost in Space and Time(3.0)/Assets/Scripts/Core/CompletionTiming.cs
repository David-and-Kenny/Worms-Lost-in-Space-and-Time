using System;

namespace Worms.Core
{
    /// <summary>Preserves legacy Mathf.Round(seconds * 100f) / 100f without an engine dependency.</summary>
    public static class CompletionTiming
    {
        public static float RoundSeconds(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0)
                throw new ArgumentOutOfRangeException(nameof(seconds));
            if (seconds > float.MaxValue / 100f) return seconds;
            // Mono may retain extra precision in float locals. The bit round trip forces IEEE float32
            // before midpoint rounding, matching the float argument boundary of Mathf.Round.
            int scaledBits = BitConverter.SingleToInt32Bits(seconds * 100f);
            float hundredths = BitConverter.Int32BitsToSingle(scaledBits);
            return (float)Math.Round(hundredths, MidpointRounding.ToEven) / 100f;
        }
    }
}
