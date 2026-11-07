using System;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

/// <summary>Explicit package bootstrap for a closed checkout; does not run on import.</summary>
public static class MonetizationPackageInstaller
{
    private static AddAndRemoveRequest request;
    private static double deadline;

    public static void Install()
    {
        request = Client.AddAndRemove(new[]
        {
            "file:com.google.ads.mobile-11.6.0.tgz",
            "com.unity.purchasing@5.5.0"
        });
        deadline = EditorApplication.timeSinceStartup + 600;
        EditorApplication.update += Poll;
    }

    private static void Poll()
    {
        if (!request.IsCompleted && EditorApplication.timeSinceStartup < deadline) return;
        EditorApplication.update -= Poll;
        if (!request.IsCompleted || request.Status == StatusCode.Failure)
        {
            Debug.LogError(request.Error?.message ?? "Package installation timed out.");
            EditorApplication.Exit(1);
            return;
        }
        Debug.Log("Monetization packages installed.");
        EditorApplication.Exit(0);
    }
}
