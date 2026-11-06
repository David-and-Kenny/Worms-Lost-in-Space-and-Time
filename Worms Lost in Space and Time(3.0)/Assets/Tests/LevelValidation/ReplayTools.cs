#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Worms.LevelValidation
{
    [InitializeOnLoad]
    public static class ReplayTools
    {
        private static LevelValidationReport preview;
        static ReplayTools() { SceneView.duringSceneGui += DrawReport; }

        [MenuItem("Worms/Level Validation/Replay saved route")]
        public static void Replay()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before starting a validation replay.");
            string path = EditorUtility.OpenFilePanel("Choose a .replay.json certificate", "", "json");
            if (string.IsNullOrEmpty(path)) return;
            var replay = JsonUtility.FromJson<InputReplay>(File.ReadAllText(path));
            if (replay == null || string.IsNullOrEmpty(replay.ScenePath)) throw new ArgumentException("Not a saved route certificate.");
            string repository = Directory.GetParent(Application.dataPath).Parent.FullName;
            string output = Path.Combine(repository, "TestResults", "LevelValidation", "interactive-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
            Directory.CreateDirectory(output);
            var request = new LevelValidationRequest { Operation = "replay", Level = replay.ScenePath, ReplayFile = path,
                OutputDirectory = output, Seed = replay.Seed, FramesPerSecond = replay.FramesPerSecond, Visual = true };
            string requestFile = Path.Combine(output, "request.json");
            File.WriteAllText(requestFile, JsonUtility.ToJson(request, true));
            SessionState.SetString("Worms.ValidationRequest", requestFile);
            try { ValidationCommands.Inspect(); }
            catch { SessionState.EraseString("Worms.ValidationRequest"); throw; }
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.PlayMode,
                testNames = new[] { "Worms.LevelValidation.LevelValidationCommandTests.ExecuteRequest" } }));
        }

        [MenuItem("Worms/Level Validation/View trajectory report")]
        public static void ViewReport()
        {
            string path = EditorUtility.OpenFilePanel("Choose a .report.json", "", "json");
            if (string.IsNullOrEmpty(path)) return;
            preview = JsonUtility.FromJson<LevelValidationReport>(File.ReadAllText(path));
            SceneView.RepaintAll();
        }
        [MenuItem("Worms/Level Validation/Clear trajectory")]
        public static void Clear() { preview = null; SceneView.RepaintAll(); }

        private static void DrawReport(SceneView view)
        {
            if (preview == null || preview.Trajectory == null) return;
            Handles.color = Color.cyan;
            for (int index = 1; index < preview.Trajectory.Count; index++)
                Handles.DrawLine(Position(preview.Trajectory[index - 1]), Position(preview.Trajectory[index]));
            if (preview.Replay == null || preview.Replay.Expected == null) return;
            Handles.color = Color.yellow;
            foreach (var sample in preview.Replay.Expected)
                if (sample.Kind == "Launched" || sample.Kind == "ForcedDetach")
                    Handles.Label(Position(sample), $"{sample.Kind}: frame {sample.Frame}, {sample.Angle:0.0}°");
        }
        private static Vector3 Position(TraversalSample sample) => new Vector3(sample.X, sample.Y, 0);
    }
}
#endif
