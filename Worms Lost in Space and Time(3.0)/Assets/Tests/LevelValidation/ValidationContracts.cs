using System;
using System.Collections.Generic;

namespace Worms.LevelValidation
{
    public enum ValidationOutcome { VerifiedReachable, InvalidContent, Inconclusive, ReplayFailed, RunnerUnstable, Stale, ExecutionError }
    public enum TrialOutcome { Completed, Dead, Frontier, TimedOut, Diverged, Cancelled, ExecutionError }

    [Serializable]
    public sealed class LevelValidationRequest
    {
        public int Version = 1;
        public string Operation = "search";
        public string Level = "all";
        public string OutputDirectory;
        public string ReplayFile;
        public string ReplayDirectory;
        public string ProposalDirectory;
        public int Seed = 1729;
        public int FramesPerSecond = 60;
        public float SearchSeconds = 300;
        public int MaximumTrials = 10000;
        public int MaximumJumps = 64;
        public float TrialSeconds = 120;
        public int BeamWidth = 256;
        public int TimingRadiusTicks = 12;
        public bool Visual;
        public void Validate()
        {
            if (Version != 1 || (Operation != "inspect" && Operation != "search" && Operation != "replay" && Operation != "batch"))
                throw new ArgumentException("Unsupported request version or operation.");
            if (FramesPerSecond < 10 || FramesPerSecond > 240 || Seed < 0 || !Finite(SearchSeconds) || SearchSeconds <= 0
                || SearchSeconds > 3600 || !Finite(TrialSeconds) || TrialSeconds <= 0 || TrialSeconds > 600
                || MaximumTrials < 1 || MaximumTrials > 100000 || MaximumJumps < 1 || MaximumJumps > 64 || BeamWidth < 1 || BeamWidth > 1024
                || TimingRadiusTicks < 1 || TimingRadiusTicks > 50)
                throw new ArgumentException("Invalid validation budget or timing profile.");
            if (string.IsNullOrWhiteSpace(OutputDirectory)) throw new ArgumentException("An output directory is required.");
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    [Serializable]
    public sealed class SceneObjectIdentity
    {
        public string Id;
        public string Hierarchy;
        public string Name;
        public string Role;
        public float X;
        public float Y;
        public float AngularSpeed;
        public float CenterX;
        public float CenterY;
        public float SizeX;
        public float SizeY;
        public bool Circle;
        public bool Moving;
        public float BreakSeconds;
    }

    [Serializable]
    public sealed class LevelManifest
    {
        public string LevelId;
        public string SceneName;
        public string ScenePath;
        public string Fingerprint;
        public List<SceneObjectIdentity> Objects = new List<SceneObjectIdentity>();
        public List<string> Errors = new List<string>();
        public List<string> Warnings = new List<string>();
    }

    [Serializable]
    public sealed class JumpCommand
    {
        public int Frame;
        public string SurfaceId;
        public int AttachmentOrdinal;
        public float DelaySeconds;
    }

    [Serializable]
    public sealed class TraversalSample
    {
        public int Frame;
        public string Kind;
        public string SurfaceId;
        public int AttachmentOrdinal;
        public float Time;
        public float X;
        public float Y;
        public float Angle;
        public float VelocityX;
        public float VelocityY;
        public float Oxygen;
    }

    [Serializable]
    public sealed class InputReplay
    {
        public int Version = 2;
        public string LevelId;
        public string ScenePath;
        public string Fingerprint;
        public string UnityVersion;
        public int FramesPerSecond = 60;
        public int Seed;
        public List<JumpCommand> Jumps = new List<JumpCommand>();
        public List<TraversalSample> Expected = new List<TraversalSample>();
        public bool IsWellFormed()
        {
            if ((Version != 1 && Version != 2) || string.IsNullOrEmpty(LevelId) || string.IsNullOrEmpty(Fingerprint)
                || string.IsNullOrEmpty(UnityVersion) || string.IsNullOrEmpty(ScenePath) || !ScenePath.StartsWith("Assets/", StringComparison.Ordinal)
                || !ScenePath.EndsWith(".unity", StringComparison.Ordinal) || ScenePath.Contains("..")
                || FramesPerSecond < 10 || FramesPerSecond > 240 || Seed < 0 || Jumps == null || Jumps.Count > 64
                || Expected == null || Expected.Count == 0 || Expected.Count > 50000 || Expected[Expected.Count - 1] == null || Expected[Expected.Count - 1].Kind != "Completed") return false;
            int previous = -1;
            foreach (var jump in Jumps)
            {
                if (jump == null || jump.Frame <= previous || string.IsNullOrEmpty(jump.SurfaceId) || jump.AttachmentOrdinal < 1
                    || float.IsNaN(jump.DelaySeconds) || float.IsInfinity(jump.DelaySeconds) || jump.DelaySeconds < 0) return false;
                previous = jump.Frame;
            }
            return Expected.TrueForAll(item => item != null && item.Frame >= 0 && Finite(item.X) && Finite(item.Y)
                && Finite(item.Angle) && Finite(item.VelocityX) && Finite(item.VelocityY) && Finite(item.Oxygen) && Finite(item.Time));
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    [Serializable]
    public sealed class TrialResult
    {
        public List<JumpCommand> DeliveredJumps = new List<JumpCommand>();
        public TrialOutcome Outcome;
        public string Reason;
        public int SaveWrites;
        public int LastFrame;
        public float Elapsed;
        public float Oxygen;
        public float SafeWaitSeconds = -1;
        public float X;
        public float Y;
        public float Angle;
        public string SurfaceId;
        public float VelocityX;
        public float VelocityY;
        public int AttachmentOrdinal;
        public int AttachmentFrame;
        public List<TraversalSample> Events = new List<TraversalSample>();
        public List<TraversalSample> Trace = new List<TraversalSample>();
    }

    [Serializable]
    public sealed class TimingWindow
    {
        public int JumpIndex;
        public float FirstOffsetSeconds;
        public float LastOffsetSeconds;
    }

    [Serializable]
    public sealed class ProfileResult
    {
        public int FramesPerSecond;
        public bool Completed;
        public string Reason;
    }

    [Serializable]
    public sealed class LevelValidationReport
    {
        public int Version = 1;
        public string LevelId;
        public string Fingerprint;
        public ValidationOutcome Outcome;
        public string Reason;
        public int Trials;
        public double WallSeconds;
        public float CompletionSeconds;
        public float OxygenRemaining;
        public bool AnalysisComplete;
        public int TimingRadiusTicks;
        public int PerturbationTrials;
        public int PerturbationSuccesses;
        public List<TimingWindow> TimingWindows = new List<TimingWindow>();
        public List<ProfileResult> Profiles = new List<ProfileResult>();
        public List<string> Diagnostics = new List<string>();
        public InputReplay Replay;
        public bool HasReplay;
        public string Status;
        public List<TraversalSample> Trajectory = new List<TraversalSample>();
        public SearchCoverage Coverage = new SearchCoverage();
    }

    [Serializable]
    public sealed class SearchCoverage
    {
        public int ActionsTried;
        public int FrontiersReached;
        public int MaximumRouteJumps;
        public int TimedOutTrials;
        public List<string> SurfacesReached = new List<string>();
    }

    [Serializable]
    public sealed class ValidationBatchReport
    {
        public int Version = 1;
        public bool Success;
        public bool Complete;
        public int RequestedLevels;
        public List<LevelValidationReport> Levels = new List<LevelValidationReport>();
    }
}
