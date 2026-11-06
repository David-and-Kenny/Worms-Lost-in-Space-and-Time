#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Worms.LevelValidation
{
    [Serializable]
    public sealed class ManifestBundle { public List<LevelManifest> Levels = new List<LevelManifest>(); }

    public static class ValidationCommands
    {
        public const string RequestArgument = "-worms-validation-request";
        public static string RequestPath()
        {
            var arguments = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(arguments, RequestArgument);
            return index >= 0 && index + 1 < arguments.Length ? arguments[index + 1]
                : SessionState.GetString("Worms.ValidationRequest", null);
        }
        public static LevelValidationRequest ReadRequest()
        {
            string path = RequestPath();
            if (string.IsNullOrWhiteSpace(path)) return null;
            var request = JsonUtility.FromJson<LevelValidationRequest>(File.ReadAllText(path));
            if (request == null) throw new ArgumentException("Request document is null.");
            request.Validate();
            return request;
        }

        public static void Inspect()
        {
            var request = ReadRequest() ?? throw new ArgumentException("Missing " + RequestArgument);
            Directory.CreateDirectory(request.OutputDirectory);
            var bundle = new ManifestBundle();
            string recordedDirectory = !string.IsNullOrEmpty(request.ReplayDirectory) ? request.ReplayDirectory : request.ProposalDirectory;
            if (request.Level == "certified" && !string.IsNullOrEmpty(recordedDirectory))
            {
                foreach (string path in Directory.GetFiles(recordedDirectory, "*.replay.json").OrderBy(item => item, StringComparer.Ordinal))
                {
                    var replay = JsonUtility.FromJson<InputReplay>(File.ReadAllText(path));
                    if (replay == null || !replay.IsWellFormed()) throw new ArgumentException("Malformed replay: " + path);
                    bundle.Levels.AddRange(SceneInspection.Inspect(replay.ScenePath));
                }
                if (bundle.Levels.Count == 0) throw new ArgumentException("No replay certificates in " + recordedDirectory);
                if (bundle.Levels.Select(item => item.LevelId).Distinct().Count() != bundle.Levels.Count) throw new ArgumentException("Duplicate level certificates.");
            }
            else bundle.Levels = SceneInspection.Inspect(request.Level);
            File.WriteAllText(Path.Combine(request.OutputDirectory, "manifest.json"), JsonUtility.ToJson(bundle, true));
            if (request.Operation == "inspect")
            {
                var batch = new ValidationBatchReport { Complete = true, RequestedLevels = bundle.Levels.Count, Success = bundle.Levels.All(item => item.Errors.Count == 0) };
                foreach (var manifest in bundle.Levels)
                    batch.Levels.Add(new LevelValidationReport { LevelId = manifest.LevelId, Fingerprint = manifest.Fingerprint,
                        Outcome = manifest.Errors.Count == 0 ? ValidationOutcome.Inconclusive : ValidationOutcome.InvalidContent,
                        Reason = manifest.Errors.Count == 0 ? "Structure inspected; reachability has not been tested." : string.Join("; ", manifest.Errors),
                        Diagnostics = manifest.Warnings });
                WriteReports(request.OutputDirectory, batch);
            }
            Debug.Log("Level validation manifest exported: " + bundle.Levels.Count + " levels.");
        }

        public static void WriteReports(string outputDirectory, ValidationBatchReport batch)
        {
            Directory.CreateDirectory(outputDirectory);
            var markdown = new StringBuilder("# Level validation\n\nThis report is produced by a local Unity utility; no AI service is used.\n\n");
            markdown.AppendLine($"Run complete: {batch.Complete} ({batch.Levels.Count}/{batch.RequestedLevels} levels).\n");
            markdown.AppendLine("| Level | Outcome | Trials | Wall seconds | Reason |");
            markdown.AppendLine("| --- | --- | ---: | ---: | --- |");
            foreach (var report in batch.Levels)
            {
                report.Status = report.Outcome.ToString();
                report.HasReplay = report.Replay != null;
                string stem = SafeFileName(report.LevelId);
                File.WriteAllText(Path.Combine(outputDirectory, stem + ".report.json"), JsonUtility.ToJson(report, true));
                if (report.Replay != null)
                    File.WriteAllText(Path.Combine(outputDirectory, stem + ".replay.json"), JsonUtility.ToJson(report.Replay, true));
                markdown.AppendLine($"| {report.LevelId} | {report.Status} | {report.Trials} | {report.WallSeconds:0.00} | {Cell(report.Reason)} |");
            }
            foreach (var report in batch.Levels.Where(item => item.Replay != null))
            {
                markdown.AppendLine($"\n## {report.LevelId}\n\nFinish: {report.CompletionSeconds:0.000}s; oxygen: {report.OxygenRemaining:0.000}; analysis complete: {report.AnalysisComplete}.\n");
                markdown.AppendLine("Timing offsets are sampled at the reference frame rate within ±250ms; untested offsets are unknown.");
                foreach (var window in report.TimingWindows)
                    markdown.AppendLine($"- Jump {window.JumpIndex + 1}: {window.FirstOffsetSeconds * 1000:0.0} to {window.LastOffsetSeconds * 1000:0.0} ms.");
                foreach (var profile in report.Profiles)
                    markdown.AppendLine($"- {profile.FramesPerSecond} Hz policy: {(profile.Completed ? "completed" : "did not complete")} ({profile.Reason}).");
                markdown.AppendLine($"- Single-jump variations: {report.PerturbationSuccesses}/{report.PerturbationTrials}. This is not a human difficulty rating.");
            }
            File.WriteAllText(Path.Combine(outputDirectory, "summary.json"), JsonUtility.ToJson(batch, true));
            File.WriteAllText(Path.Combine(outputDirectory, "summary.md"), markdown.ToString());
        }
        private static string Cell(string value) => (value ?? "").Replace("|", "/").Replace("\r", " ").Replace("\n", " ");
        private static string SafeFileName(string id) => new string(id.Select(character => char.IsLetterOrDigit(character) || character == '-' ? character : '_').ToArray());
        public static string ReplayPath(string directory, string levelId) => Path.Combine(directory, SafeFileName(levelId) + ".replay.json");
    }
}
#endif
