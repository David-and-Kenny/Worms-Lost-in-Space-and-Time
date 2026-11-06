using System;

namespace Worms.Core
{
    /// <summary>One run. The first terminal request owns the result and its persistence.</summary>
    public sealed class LevelSession
    {
        private readonly ProgressionService progression;
        public SessionState State { get; private set; }
        public string LevelId { get; }
        public float ElapsedSeconds { get; private set; }
        public CompletionResult Completion { get; private set; }
        public event Action<LevelSession> Changed;

        public LevelSession(LevelDefinition level, ProgressionService progression)
        {
            if (level == null || string.IsNullOrEmpty(level.Id)) throw new ArgumentException("A level identity is required.", nameof(level));
            this.progression = progression ?? throw new ArgumentNullException(nameof(progression));
            LevelId = level.Id;
            State = SessionState.Playing;
        }

        public void Tick(float deltaSeconds)
        {
            if (float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds) || deltaSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            if (State != SessionState.Playing) return;
            ElapsedSeconds = (float)Math.Min(float.MaxValue, (double)ElapsedSeconds + deltaSeconds);
        }

        public bool TryComplete()
        {
            if (State != SessionState.Playing) return false;
            // Set the terminal guard before persistence or observers can reenter.
            State = SessionState.Completed;
            ElapsedSeconds = CompletionTiming.RoundSeconds(ElapsedSeconds);
            Completion = progression.RecordCompletion(LevelId, ElapsedSeconds);
            Changed?.Invoke(this);
            return true;
        }

        public bool TryDie()
        {
            if (State != SessionState.Playing) return false;
            State = SessionState.Dead;
            Changed?.Invoke(this);
            return true;
        }
    }
}
