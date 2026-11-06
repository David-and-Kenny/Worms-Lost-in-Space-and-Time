using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class GameplayBaselineTests
{
    private readonly System.Collections.Generic.List<GameObject> objects = new System.Collections.Generic.List<GameObject>();
    private readonly System.Collections.Generic.List<Texture2D> textures = new System.Collections.Generic.List<Texture2D>();

    [TearDown]
    public void TearDown()
    {
        foreach (var item in objects)
            if (item) UnityEngine.Object.DestroyImmediate(item);
        objects.Clear();
        foreach (var texture in textures)
            if (texture) UnityEngine.Object.DestroyImmediate(texture);
        textures.Clear();
    }

    [Test]
    public void PlayerDetatchClearsAttachmentAndLaunchesAlongItsUpAxis()
    {
        var player = NewObject("player");
        var body = player.AddComponent<Rigidbody2D>();
        var type = FindType("Player");
        var component = player.AddComponent(type);
        var attached = type.GetField("playerAttached");
        attached.SetValue(component, true);
        player.transform.rotation = Quaternion.Euler(0, 0, 90);

        type.GetMethod("detatch").Invoke(component, null);

        Assert.IsFalse((bool)attached.GetValue(component));
        Assert.That(Vector2.Distance(body.linearVelocity, (Vector2)(player.transform.up * 5f)), Is.LessThan(0.001f));
    }

    [UnityTest]
    public IEnumerator PlayerStartsWithDownwardVelocityOfThree()
    {
        var player = NewObject("player start baseline");
        var body = player.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        player.AddComponent(FindType("Player"));

        yield return null;

        Assert.That(Vector2.Distance(body.linearVelocity, Vector2.down * 3f), Is.LessThan(0.001f));
    }

    [UnityTest]
    public IEnumerator WheelRotatesAndMovingSpikesStayOnTheirConfiguredPath()
    {
        var wheel = NewObject("wheel");
        var wheelType = FindType("Wheel");
        wheel.AddComponent(wheelType);
        var before = wheel.transform.rotation;

        var start = NewObject("spike start");
        var end = NewObject("spike end");
        start.transform.position = new Vector3(-2, 1, 0);
        end.transform.position = new Vector3(4, 3, 0);
        var spikes = NewObject("spikes");
        var spikeType = FindType("MovingSpikes");
        var spike = spikes.AddComponent(spikeType);
        spikeType.GetField("startPos").SetValue(spike, start.transform);
        spikeType.GetField("endPos").SetValue(spike, end.transform);

        yield return new WaitForSeconds(0.05f);

        Assert.That(Quaternion.Angle(before, wheel.transform.rotation), Is.GreaterThan(0f));
        var p = spikes.transform.position;
        Assert.That(p.x, Is.InRange(-2f, 4f));
        Assert.That(p.y, Is.InRange(1f, 3f));
        Assert.AreEqual(0f, p.z, 0.001f);
    }

    [Test]
    public void CameraChangeZoneReplacesFollowTargetAndAlternatesAxis()
    {
        var cameraObject = NewObject("camera");
        cameraObject.AddComponent<Camera>();
        var followType = FindType("CameraFollow");
        cameraObject.AddComponent(followType);
        var playerObject = NewObject("player");
        playerObject.AddComponent<Rigidbody2D>();
        var playerType = FindType("Player");
        var player = playerObject.AddComponent(playerType);
        playerType.GetField("cameraTransform").SetValue(player, cameraObject.transform);
        playerType.GetField("direction").SetValue(player, 0);
        var background = NewObject("background").transform;
        playerType.GetField("back").SetValue(player, background);

        var zone = NewObject("CameraChange");
        var center = NewObject("Center");
        center.transform.SetParent(zone.transform);
        var collider = zone.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        InvokePrivate(playerType, player, "OnTriggerEnter2D", collider);

        var followers = cameraObject.GetComponents(followType);
        var follow = followers[followers.Length - 1];
        Assert.AreSame(playerObject.transform, followType.GetField("target").GetValue(follow));
        Assert.AreSame(background, followType.GetField("background").GetValue(follow));
        Assert.AreSame(center.transform, followType.GetField("centerFloat").GetValue(follow));
        Assert.IsFalse((bool)followType.GetField("followingX").GetValue(follow));
        Assert.IsTrue((bool)followType.GetField("followingY").GetValue(follow));
        Assert.AreEqual(1, playerType.GetField("direction").GetValue(player));
    }

    [UnityTest]
    public IEnumerator ExplosionEmitsSevenBodyPartsAndOneRetryUi()
    {
        var player = NewObject("exploding player");
        player.AddComponent<Rigidbody2D>();
        var explodeType = FindType("Explode");
        var explode = player.AddComponent(explodeType);
        AssignBodyParts(explodeType, explode);
        yield return null;

        explodeType.GetMethod("OnExplode").Invoke(explode, null);
        yield return null;

        var ui = GameObject.Find("ClickToContinue");
        Assert.IsNotNull(ui, "Explosion should create the retry UI object.");
        var retryType = FindType("DisplayRestartText");
        var retry = ui.GetComponent(retryType);
        Assert.IsNotNull(retry, "Retry UI object should carry DisplayRestartText.");
        ((Behaviour)retry).enabled = false;
        objects.Add(ui);
        Assert.AreEqual(7, TrackBodyPartClones());
    }

    [UnityTest]
    public IEnumerator OxygenDepletionExplodesPlayerAndCreatesRetryUi()
    {
        var player = NewObject("oxygen player");
        player.AddComponent<Rigidbody2D>();
        player.AddComponent(FindType("Player"));
        var explodeType = FindType("Explode");
        var explode = player.AddComponent(explodeType);
        AssignBodyParts(explodeType, explode);
        var oxygenObject = NewObject("oxygen");
        var oxygenType = FindType("Oxygen");
        var oxygen = oxygenObject.AddComponent(oxygenType);
        oxygenType.GetField("air").SetValue(oxygen, -0.01f);
        oxygenType.GetField("airBurnRate").SetValue(oxygen, 0f);
        var bg = new Texture2D(4, 4);
        var bar = new Texture2D(4, 4);
        textures.Add(bg);
        textures.Add(bar);
        oxygenType.GetField("bgTexture").SetValue(oxygen, bg);
        oxygenType.GetField("airBarTexture").SetValue(oxygen, bar);

        yield return new WaitForSeconds(0.05f);

        var ui = GameObject.Find("ClickToContinue");
        Assert.IsNotNull(ui, "Depleted oxygen should route through explosion and create retry UI.");
        Assert.IsNotNull(ui.GetComponent(FindType("DisplayRestartText")));
        ((Behaviour)ui.GetComponent(FindType("DisplayRestartText"))).enabled = false;
        objects.Add(ui);
        Assert.AreEqual(7, TrackBodyPartClones());
        Assert.IsFalse(player);
    }

    private GameObject NewObject(string name)
    {
        var item = new GameObject(name);
        objects.Add(item);
        return item;
    }

    private static Type FindType(string name)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType(name, false);
            if (type != null) return type;
        }
        Assert.Fail("Could not find gameplay type " + name + " in loaded assemblies.");
        return null;
    }

    private static void InvokePrivate(Type type, object instance, string method, object argument)
    {
        type.GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(instance, new[] { argument });
    }

    private void AssignBodyParts(Type explodeType, Component explode)
    {
        string[] fields = { "arm1", "arm2", "head", "torso", "leg1", "leg2", "jetpack" };
        for (int i = 0; i < fields.Length; i++)
        {
            var part = NewObject("body part " + i);
            part.AddComponent<Rigidbody2D>();
            explodeType.GetField(fields[i]).SetValue(explode, part.transform);
        }
    }

    private int TrackBodyPartClones()
    {
        int count = 0;
        for (int i = 0; i < 7; i++)
        {
            var clone = GameObject.Find("body part " + i + "(Clone)");
            if (clone != null)
            {
                count++;
                objects.Add(clone);
            }
        }
        return count;
    }
}
