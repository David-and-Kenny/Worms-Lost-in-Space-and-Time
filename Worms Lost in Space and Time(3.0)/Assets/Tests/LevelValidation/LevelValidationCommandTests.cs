#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Worms.LevelValidation
{
    public sealed class LevelValidationCommandTests
    {
        [UnityTest]
        [Timeout(86400000)] // The explicit per-level budget and process watchdog govern long catalog searches.
        public IEnumerator ExecuteRequest()
        {
            var request = ValidationCommands.ReadRequest();
            // Ordinary test-suite runs check discovery without launching a catalog search.
            if (request == null) { Assert.That(ValidationCommands.RequestPath(), Is.Null.Or.Empty); yield break; }
            var batch = new ValidationBatchReport();
            var bundle = JsonUtility.FromJson<ManifestBundle>(File.ReadAllText(Path.Combine(request.OutputDirectory, "manifest.json")));
            batch.RequestedLevels = bundle.Levels.Count;
            if (!string.IsNullOrEmpty(request.ReplayDirectory)) request.Operation = "replay";
            using (new ValidationEnvironment(request.Visual))
            {
                bool previousLogPolicy = LogAssert.ignoreFailingMessages;
                string engineError = null;
                Application.LogCallback onLog = (message, stack, type) =>
                {
                    if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) engineError = message + "\n" + stack;
                };
                // Errors become explicit per-level failures; they must not abort the remaining catalog.
                LogAssert.ignoreFailingMessages = true;
                Application.logMessageReceived += onLog;
                try
                {
                    foreach (var manifest in bundle.Levels)
                    {
                        Debug.Log("Level validation starting: " + manifest.LevelId);
                        engineError = null;
                        InputReplay saved = null;
                        if (request.Operation == "replay" || !string.IsNullOrEmpty(request.ReplayFile) || !string.IsNullOrEmpty(request.ProposalDirectory))
                        {
                            string directory = request.Operation == "replay" ? request.ReplayDirectory : request.ProposalDirectory;
                            string path = string.IsNullOrEmpty(directory) ? request.ReplayFile : ValidationCommands.ReplayPath(directory, manifest.LevelId);
                            if (!string.IsNullOrEmpty(path) && File.Exists(path)) saved = JsonUtility.FromJson<InputReplay>(File.ReadAllText(path));
                            if (saved != null && saved.IsWellFormed()) { request.Seed = saved.Seed; request.FramesPerSecond = saved.FramesPerSecond; }
                        }
                        LevelValidator validator = null;
                        var runner = new UnityTrialRunner(request, () => validator.BudgetExpired);
                        var automatic = new UnityTrialRunner(request, () => validator.BudgetExpired, controlledPhysics: false);
                        validator = new LevelValidator(runner, request, automatic);
                        LevelValidationReport report = null;
                        Exception failure = null;
                        yield return Guard(validator.Evaluate(manifest, saved, value => report = value), error => failure = error);
                        if (failure != null || engineError != null)
                            report = new LevelValidationReport { LevelId = manifest.LevelId, Fingerprint = manifest.Fingerprint,
                                Outcome = ValidationOutcome.ExecutionError, Reason = failure?.ToString() ?? engineError };
                        batch.Levels.Add(report);
                        batch.Complete = batch.Levels.Count == batch.RequestedLevels;
                        batch.Success = batch.Complete && batch.Levels.All(item => item.Outcome == ValidationOutcome.VerifiedReachable);
                        ValidationCommands.WriteReports(request.OutputDirectory, batch);
                    }
                    yield return SceneManager.LoadSceneAsync("Splash");
                    yield return null;
                }
                finally
                {
                    Application.logMessageReceived -= onLog;
                    LogAssert.ignoreFailingMessages = previousLogPolicy;
                    UnityEditor.SessionState.EraseString("Worms.ValidationRequest");
                    ValidationCommands.WriteReports(request.OutputDirectory, batch);
                }
            }
            Assert.That(batch.Levels.Any(item => item.Outcome == ValidationOutcome.ExecutionError), Is.False, "See validation reports for execution errors.");
        }

        // Flatten nested coroutines so report generation also handles asynchronous exceptions.
        private static IEnumerator Guard(IEnumerator routine, Action<Exception> failed)
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(routine);
            try
            {
                while (stack.Count > 0)
                {
                    bool advanced;
                    object current = null;
                    try { advanced = stack.Peek().MoveNext(); if (advanced) current = stack.Peek().Current; }
                    catch (Exception error) { failed(error); yield break; }
                    if (!advanced) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
                    if (current is IEnumerator nested) stack.Push(nested);
                    else yield return current;
                }
            }
            finally { while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose(); }
        }
    }
}
#endif
