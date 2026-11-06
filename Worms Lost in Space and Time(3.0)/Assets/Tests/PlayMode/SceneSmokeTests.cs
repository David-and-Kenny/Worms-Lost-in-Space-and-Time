using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class SceneSmokeTests
{
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
}
