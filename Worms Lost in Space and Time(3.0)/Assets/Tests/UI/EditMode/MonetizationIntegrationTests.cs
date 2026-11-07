using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Worms.Core;
using Worms.Monetization;
using Worms.Navigation;
using Worms.UI;

public sealed class MonetizationIntegrationTests
{
    private sealed class Catalog : ILevelCatalog
    {
        public IReadOnlyList<LevelDefinition> Levels { get; } = new[]
        { new LevelDefinition { Id = "first", SceneName = "Level1", WorldId = "world-1" } };
        public LevelDefinition GetLevel(string id) => id == "first" ? Levels[0] : null;
        public LevelDefinition FindByScene(string scene) => scene == "Level1" ? Levels[0] : null;
        public LevelDefinition GetNext(string id) => null;
    }
    private sealed class Storage : ISaveStorage
    {
        public SaveData Load() => null;
        public void Save(SaveData data) { }
    }
    private sealed class Loader : ISceneLoader
    {
        public string Scene = "Level1";
        public string CurrentSceneName => Scene;
        public int Loads;
        public bool CanLoad(string scene) => true;
        public void Load(string scene, Action completed) { Loads++; Scene = scene; completed(); }
        public void Quit() { }
    }
    private sealed class Transition : ISceneTransitionGate
    {
        public Action Resume;
        public bool Restarting;
        public void BeforeNavigate(string destination, bool restarting, Action navigate)
        { Resume = navigate; Restarting = restarting; }
    }

    [Test]
    public void NavigatorWaitsForAdAndCoalescesRepeatedRetryAndClose()
    {
        var catalog = new Catalog();
        var loader = new Loader();
        var transition = new Transition();
        var navigator = new SceneNavigator(loader, catalog, new ProgressionService(catalog,
            new SaveService(new Storage(), catalog)), transitionGate: transition);
        Assert.That(navigator.Restart(), Is.True);
        Assert.That(transition.Restarting, Is.True);
        Assert.That(navigator.Restart(), Is.False);
        Assert.That(loader.Loads, Is.Zero);
        transition.Resume();
        transition.Resume();
        Assert.That(loader.Loads, Is.EqualTo(1));
        Assert.That(navigator.IsLoading, Is.False);
    }

    [Test]
    public void StaleAdCloseDoesNotLoadOverAnotherScene()
    {
        var catalog = new Catalog();
        var loader = new Loader();
        var transition = new Transition();
        var navigator = new SceneNavigator(loader, catalog, new ProgressionService(catalog,
            new SaveService(new Storage(), catalog)), transitionGate: transition);
        navigator.Restart();
        loader.Scene = "World1";
        transition.Resume();
        Assert.That(loader.Loads, Is.Zero);
        Assert.That(navigator.IsLoading, Is.False);
    }

    [Test]
    public void PurchaseCacheUsesOnlyInjectedDirectoryAndRetainsBackup()
    {
        string folder = Path.Combine(Path.GetTempPath(), "worms-purchases-" + Guid.NewGuid());
        try
        {
            var storage = new EntitlementFileStorage(folder);
            Assert.That(storage.Load(), Is.False);
            storage.Save(true);
            Assert.That(storage.Load(), Is.True);
            storage.Save(false);
            Assert.That(storage.Load(), Is.False);
            Assert.That(File.ReadAllText(Path.Combine(folder, "remove-ads-v1.txt.bak")), Is.EqualTo("owned-v1"));
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }

    [Test]
    public void CommittedDefaultsNeverRequestAdsOrInitializePurchases()
    {
        var settings = Resources.Load<MonetizationSettings>("Configuration/MonetizationSettings");
        Assert.That(settings, Is.Not.Null);
        Assert.That(settings.ProductionEnabled, Is.False);
        Assert.That(settings.StorePurchasesEnabled, Is.False);
        Assert.That(settings.PreviewTestAds, Is.False);
        Assert.That(settings.AdsEnabled, Is.False);
        Assert.That(settings.FailuresPerAd, Is.EqualTo(25));
    }

    [TestCase("MainMenu")]
    [TestCase("WorldMenu")]
    public void EditableSettingsHaveCompleteBindings(string prefabName)
    {
        var view = Resources.Load<GameObject>("UI/" + prefabName).GetComponent<MonetizationSettingsView>();
        Assert.That(view, Is.Not.Null);
        Assert.That(view.Panel.activeSelf, Is.False);
        Assert.That(view.Open, Is.Not.Null);
        Assert.That(view.Close, Is.Not.Null);
        Assert.That(view.Buy, Is.Not.Null);
        Assert.That(view.Restore, Is.Not.Null);
        Assert.That(view.Privacy, Is.Not.Null);
        Assert.That(view.Policy, Is.Not.Null);
        Assert.That(view.PurchaseLabel, Is.Not.Null);
        Assert.That(view.Status, Is.Not.Null);
    }

    [Test]
    public void DeveloperPreviewCannotSelectAProductionAdUnit()
    {
        var settings = ScriptableObject.CreateInstance<MonetizationSettings>();
        try
        {
            settings.ProductionEnabled = true;
            settings.AudienceAndPrivacyReviewed = true;
            settings.AndroidInterstitialId = "ca-app-pub-1111111111111111/2222222222";
            settings.IosInterstitialId = "ca-app-pub-1111111111111111/3333333333";
            Assert.That(settings.AdsEnabled, Is.False);
            settings.PreviewTestAds = true;
            Assert.That(settings.AdsEnabled, Is.True);
            Assert.That(settings.InterstitialId, Does.Contain("3940256099942544"));
        }
        finally { UnityEngine.Object.DestroyImmediate(settings); }
    }

    [Test]
    public void ProductionGuardRejectsMissingIdsAndAcceptsDisabledDevelopmentDefaults()
    {
        var settings = ScriptableObject.CreateInstance<MonetizationSettings>();
        try
        {
            var guard = Type.GetType("MonetizationBuildGuard, Assembly-CSharp-Editor", true);
            var validate = guard.GetMethod("ValidateSettings", BindingFlags.Public | BindingFlags.Static);
            Assert.That(validate, Is.Not.Null);
            Assert.DoesNotThrow(() => validate.Invoke(null, new object[] { settings, UnityEditor.BuildTarget.Android, true }));
            settings.ProductionEnabled = true;
            var missingPrivacy = Assert.Throws<TargetInvocationException>(() => validate.Invoke(null,
                new object[] { settings, UnityEditor.BuildTarget.Android, false }));
            Assert.That(missingPrivacy.InnerException, Is.TypeOf<UnityEditor.Build.BuildFailedException>());
            settings.AudienceAndPrivacyReviewed = true;
            settings.StorePurchasesEnabled = true;
            settings.PrivacyPolicyUrl = "https://example.com/privacy";
            var missingIds = Assert.Throws<TargetInvocationException>(() => validate.Invoke(null,
                new object[] { settings, UnityEditor.BuildTarget.iOS, false }));
            Assert.That(missingIds.InnerException, Is.TypeOf<UnityEditor.Build.BuildFailedException>());
        }
        finally { UnityEngine.Object.DestroyImmediate(settings); }
    }
}
