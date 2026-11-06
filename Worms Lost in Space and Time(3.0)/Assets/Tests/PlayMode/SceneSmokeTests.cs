using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Worms;
using Worms.Configuration;
using Worms.Core;
using Worms.Navigation;

public class SceneSmokeTests
{
    [SetUp]
    public void UseDisposableProgress()
    {
        var catalog = Resources.Load<LevelCatalog>("Configuration/LevelCatalog");
        Assert.IsNotNull(catalog, "Generate the committed level catalog before running scene integration tests.");
        GameServices.ConfigureForTests(catalog, new MemoryStorage());
        SceneNavigator.ConfigureForTests(null);
    }

    [UnityTearDown]
    public IEnumerator ClearSceneAndServices()
    {
        yield return SceneManager.LoadSceneAsync("Splash");
        GameServices.ResetForTests();
        SceneNavigator.ConfigureForTests(null);
    }

    [UnityTest]
    public IEnumerator EveryBuildSceneStartsWithoutRuntimeErrors()
    {
        Assert.Greater(SceneManager.sceneCountInBuildSettings, 0);
        for (int index = 0; index < SceneManager.sceneCountInBuildSettings; index++)
        {
            yield return SceneManager.LoadSceneAsync(index);
            yield return new WaitForSeconds(1);
            Assert.IsTrue(SceneManager.GetActiveScene().isLoaded);
            Debug.Log("Scene smoke check passed: " + SceneManager.GetActiveScene().path);
        }
    }

    private sealed class MemoryStorage : ISaveStorage
    {
        private SaveData data = new SaveData();
        public SaveData Load() => data;
        public void Save(SaveData value) => data = value;
    }
}
