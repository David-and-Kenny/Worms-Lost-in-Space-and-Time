using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Worms;
using Worms.Core;
using Worms.Gameplay;
using Worms.Persistence;

public class GameplayAdapterTests
{
    private sealed class Catalog : ILevelCatalog
    {
        public IReadOnlyList<LevelDefinition> Levels { get; }
        public Catalog(string scene) { Levels = new[] { new LevelDefinition { Id = "test-level", SceneName = scene, WorldId = "world-1", StarTimeSeconds = 20f } }; }
        public LevelDefinition GetLevel(string id) { return id == Levels[0].Id ? Levels[0] : null; }
        public LevelDefinition FindByScene(string scene) { return scene == Levels[0].SceneName ? Levels[0] : null; }
        public LevelDefinition GetNext(string id) { return null; }
    }
    private sealed class Storage : ISaveStorage
    {
        public int Writes;
        public ISaveStorage Inner;
        public SaveData Load() { return Inner?.Load(); }
        public void Save(SaveData data) { Writes++; Inner?.Save(data); }
    }
    private sealed class InputFake : IPlayerInput { public bool Held; public bool JumpHeld => Held; }
    private Scene scene;
    private Scene previous;
    private Storage storage;
    private LevelSessionHost host;
    private string temporaryDirectory;

    [SetUp]
    public void SetUp()
    {
        previous = SceneManager.GetActiveScene();
        scene = SceneManager.CreateScene("GameplayFixture-" + Guid.NewGuid().ToString("N"));
        SceneManager.SetActiveScene(scene);
        storage = new Storage();
        GameServices.ConfigureForTests(new Catalog(scene.name), storage);
        host = LevelSessionHost.GetOrCreate();
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        yield return SceneManager.UnloadSceneAsync(scene);
        GameServices.ResetForTests();
        if (temporaryDirectory != null && Directory.Exists(temporaryDirectory)) Directory.Delete(temporaryDirectory, true);
        temporaryDirectory = null;
    }

    private static Player NewPlayer(string name = "renamed astronaut")
    {
        var go = new GameObject(name);
        var body = go.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        var player = go.AddComponent<Player>();
        player.InputSource = new InputFake();
        return player;
    }
    private static Wheel NewWheel(string name = "renamed surface")
    {
        var go = new GameObject(name);
        var wheel = go.AddComponent<Wheel>();
        wheel.speed = 0f;
        wheel.holder = new GameObject("renamed anchor").transform;
        wheel.holder.SetParent(go.transform);
        return wheel;
    }

    [UnityTest]
    public IEnumerator HeldJumpFollowsWheelOrientationSuppressesSameWheelAndAllowsAnother()
    {
        var player = NewPlayer();
        var input = new InputFake();
        player.InputSource = input;
        var wheel = NewWheel();
        input.Held = true;
        yield return null;
        yield return new WaitForFixedUpdate();
        Assert.That(Vector2.Distance(player.GetComponent<Rigidbody2D>().linearVelocity, Vector2.down * 3f), Is.LessThan(.001f));
        input.Held = false;
        Assert.IsTrue(player.AttachToWheel(wheel, Vector2.right));
        yield return null;
        yield return new WaitForFixedUpdate();
        Assert.That(Vector2.Distance(player.GetComponent<Rigidbody2D>().position, wheel.holder.position), Is.LessThan(.001f));
        Assert.That(Mathf.Abs(Mathf.DeltaAngle(player.GetComponent<Rigidbody2D>().rotation, -90)), Is.LessThan(.001f));
        Assert.AreEqual(-1f, player.transform.localScale.x);
        input.Held = true;
        yield return null;
        yield return new WaitForFixedUpdate();
        Assert.IsFalse(player.playerAttached);
        Assert.That(Vector2.Distance(player.GetComponent<Rigidbody2D>().linearVelocity, player.transform.up * 10f), Is.LessThan(.001f));
        Assert.IsFalse(player.AttachToWheel(wheel, Vector2.up));
        var other = NewWheel("different surface");
        other.direction = 1f;
        Assert.IsTrue(player.AttachToWheel(other, Vector2.up));
        Assert.AreEqual(1f, player.transform.localScale.x);
    }

    [UnityTest]
    public IEnumerator ConveyorUsesAuthoredSpeedAndDestroyedHolderForcesOneDetach()
    {
        var player = NewPlayer();
        var conveyor = new GameObject("renamed belt").AddComponent<Conveyor>();
        conveyor.speed = 7.5f;
        conveyor.transform.rotation = Quaternion.Euler(0, 0, 35f);
        yield return null;
        yield return new WaitForFixedUpdate();
        player.AttachToConveyor(conveyor);
        Assert.That(Vector2.Distance(player.GetComponent<Rigidbody2D>().linearVelocity, conveyor.transform.right * 7.5f), Is.LessThan(.001f));
        var wheel = NewWheel();
        player.AttachToWheel(wheel, Vector2.up);
        yield return null;
        yield return new WaitForFixedUpdate();
        UnityEngine.Object.Destroy(wheel.holder.gameObject);
        yield return null;
        yield return new WaitForFixedUpdate();
        yield return null;
        yield return new WaitForFixedUpdate();
        Assert.IsFalse(player.playerAttached);
        var velocity = player.GetComponent<Rigidbody2D>().linearVelocity;
        Assert.That(Vector2.Distance(velocity, player.transform.up * 5f), Is.LessThan(.001f));
        yield return null;
        yield return new WaitForFixedUpdate();
        Assert.That(Vector2.Distance(velocity, player.GetComponent<Rigidbody2D>().linearVelocity), Is.LessThan(.001f));
    }

    [UnityTest]
    public IEnumerator CountdownKeepsRunningAfterJumpAndOnlyDetachesItsCurrentOccupant()
    {
        var player = NewPlayer();
        var wheel = NewWheel();
        var countdown = wheel.gameObject.AddComponent<CountdownExplode>();
        countdown.timer = 10f;
        yield return null;
        yield return new WaitForFixedUpdate();
        yield return null;
        player.AttachToWheel(wheel, Vector2.right);
        Assert.AreSame(player, countdown.Occupant);
        Assert.IsTrue(countdown.countdown);
        player.detatch();
        Assert.IsNull(countdown.Occupant);
        Assert.IsTrue(countdown.countdown);
        var other = NewWheel();
        player.AttachToWheel(other, Vector2.up);
        countdown.timer = -1f;
        yield return null;
        yield return new WaitForFixedUpdate();
        yield return null;
        Assert.IsTrue(player.playerAttached, "An abandoned wheel must not detach a player from another surface.");
        Assert.IsFalse(wheel);
        var activeCountdown = other.gameObject.AddComponent<CountdownExplode>();
        player.detatch();
        var third = NewWheel();
        player.AttachToWheel(third, Vector2.up);
        player.detatch();
        player.AttachToWheel(other, Vector2.up);
        activeCountdown.timer = -1f;
        yield return null;
        yield return new WaitForFixedUpdate();
        yield return null;
        Assert.IsFalse(player.playerAttached);
        Assert.IsFalse(other);
        Assert.That(player.GetComponent<Rigidbody2D>().linearVelocity.magnitude, Is.EqualTo(5f).Within(.001f));
    }

    [Test]
    public void TypedCameraRegionReusesFollowAcrossRenamedTriggers()
    {
        var player = NewPlayer();
        var camera = new GameObject("camera").AddComponent<CameraFollow>();
        player.cameraTransform = camera.transform;
        player.back = new GameObject("backdrop").transform;
        var region = new GameObject("renamed region").AddComponent<CameraRegion>();
        region.center = new GameObject("renamed center").transform;
        var trigger = region.gameObject.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        player.SendMessage("OnTriggerEnter2D", trigger);
        Assert.IsFalse(camera.followingX);
        Assert.IsTrue(camera.followingY);
        Assert.AreSame(region.center, camera.centerFloat);
        Assert.AreSame(player.transform, camera.target);
        player.SendMessage("OnTriggerEnter2D", trigger);
        Assert.IsTrue(camera.followingX);
        Assert.IsFalse(camera.followingY);
        Assert.AreEqual(1, camera.GetComponents<CameraFollow>().Length);
    }

    [UnityTest]
    public IEnumerator CameraSmoothingMovesTowardAuthoredCenterAndPreservesBackgroundDepth()
    {
        var follow = new GameObject("framing camera").AddComponent<CameraFollow>();
        var target = new GameObject("target").transform;
        target.position = new Vector3(10f, 20f, 0f);
        var center = new GameObject("framing center").transform;
        center.position = new Vector3(2f, 5f, 0f);
        var background = new GameObject("background").transform;
        background.position = new Vector3(0f, 0f, 13f);
        follow.transform.position = new Vector3(0f, 0f, -10f);
        follow.Configure(target, background, center, true, false);
        yield return null;
        yield return new WaitForFixedUpdate();
        Assert.That(follow.transform.position.x, Is.GreaterThan(0f).And.LessThan(10f));
        Assert.That(follow.transform.position.y, Is.GreaterThan(0f).And.LessThan(5f));
        Assert.AreEqual(-10f, follow.transform.position.z);
        Assert.AreEqual(follow.transform.position.x, background.position.x);
        Assert.AreEqual(follow.transform.position.y, background.position.y);
        Assert.AreEqual(13f, background.position.z);
        follow.background = null;
        yield return null;
        yield return new WaitForFixedUpdate();
    }

    private static Explode AddExplosion(Player player)
    {
        var explosion = player.gameObject.AddComponent<Explode>();
        string[] names = { "arm1", "arm2", "head", "torso", "leg1", "leg2", "jetpack" };
        for (int i = 0; i < names.Length; i++)
        {
            var part = new GameObject("fixture part " + i);
            part.transform.position = new Vector3(1000f, 1000f);
            part.AddComponent<Rigidbody2D>().gravityScale = 0f;
            typeof(Explode).GetField(names[i]).SetValue(explosion, part.transform);
        }
        return explosion;
    }

    [UnityTest]
    public IEnumerator OxygenAndRepeatedHazardRequestsProduceOneBurstAndOneDeath()
    {
        var player = NewPlayer();
        var explosion = AddExplosion(player);
        var oxygen = new GameObject("oxygen").AddComponent<Oxygen>();
        typeof(Oxygen).GetField("player", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(oxygen, player);
        oxygen.air = 0f;
        int changed = 0;
        host.Changed += value => changed++;
        yield return null;
        yield return new WaitForFixedUpdate();
        if (explosion) { explosion.OnExplode(); explosion.OnExplode(); }
        yield return null;
        yield return new WaitForFixedUpdate();
        Assert.AreEqual(SessionState.Dead, host.Session.State);
        Assert.AreEqual(1, changed);
        Assert.AreEqual(0, storage.Writes);
        Assert.IsFalse(player);
        int clones = 0;
        foreach (var body in UnityEngine.Object.FindObjectsByType<Rigidbody2D>())
            if (body.name.StartsWith("fixture part ", StringComparison.Ordinal) && body.name.EndsWith("(Clone)", StringComparison.Ordinal)) clones++;
        Assert.AreEqual(7, clones);
    }

    [UnityTest]
    public IEnumerator DeadlyTriggerRoutesThroughSessionDeath() { return AssertDeadlyContact(true); }

    [UnityTest]
    public IEnumerator DeadlyCollisionRoutesThroughSessionDeath() { return AssertDeadlyContact(false); }

    private IEnumerator AssertDeadlyContact(bool isTrigger)
    {
        var player = NewPlayer();
        player.gameObject.AddComponent<CircleCollider2D>();
        AddExplosion(player);
        if (!isTrigger)
        {
            player.AttachToWheel(NewWheel(), Vector2.zero);
            Assert.That(player.GetComponent<Rigidbody2D>().bodyType, Is.EqualTo(RigidbodyType2D.Kinematic));
        }
        var hazard = new GameObject("renamed lethal obstacle");
        hazard.tag = "Deadly";
        hazard.AddComponent<BoxCollider2D>().isTrigger = isTrigger;
        yield return new WaitForFixedUpdate();
        yield return null;
        yield return new WaitForFixedUpdate();
        Assert.AreEqual(SessionState.Dead, host.Session.State);
        Assert.IsFalse(player);
        Assert.AreEqual(0, storage.Writes);
    }

    [Test]
    public void MissingWheelHolderRejectsAttachmentWithoutChangingPlayer()
    {
        var player = NewPlayer();
        var invalid = new GameObject("unconfigured surface").AddComponent<Wheel>();
        Assert.IsFalse(player.AttachToWheel(invalid, Vector2.zero));
        Assert.IsFalse(player.playerAttached);
        Assert.Throws<ArgumentException>(() => player.Attachment.Attach(player, invalid, Vector2.zero));
    }

    [UnityTest]
    public IEnumerator PhysicsCollisionUsesTypedWheelSurfaceAndIgnoresDecorativeRotators()
    {
        var player = NewPlayer();
        player.gameObject.AddComponent<CircleCollider2D>().radius = .5f;
        player.transform.position = new Vector3(1.2f, 0f);
        var wheel = NewWheel("renamed attachable wheel");
        wheel.gameObject.AddComponent<WheelSurface>().wheel = wheel;
        wheel.gameObject.AddComponent<CircleCollider2D>().radius = 1f;

        var bystander = NewPlayer("spinner contact player");
        bystander.transform.position = new Vector3(11.2f, 0f);
        bystander.gameObject.AddComponent<CircleCollider2D>().radius = .5f;
        var spinner = NewWheel("rotating hazard with an anchor");
        spinner.transform.position = new Vector3(10f, 0f);
        spinner.gameObject.AddComponent<CircleCollider2D>().radius = 1f;

        var beltPlayer = NewPlayer("belt contact player");
        beltPlayer.transform.position = new Vector3(20f, .5f);
        beltPlayer.gameObject.AddComponent<CircleCollider2D>().radius = .5f;
        var belt = new GameObject("renamed conveyor").AddComponent<Conveyor>();
        belt.speed = 6f;
        belt.transform.position = new Vector3(20f, 0f);
        belt.gameObject.AddComponent<BoxCollider2D>().size = new Vector2(4f, .5f);

        yield return new WaitForFixedUpdate();
        yield return null;
        yield return new WaitForFixedUpdate();
        Assert.IsTrue(player.playerAttached);
        Assert.AreSame(wheel, player.wheel);
        Assert.IsFalse(bystander.playerAttached, "A Wheel component can rotate a hazard without making it attachable.");
        Assert.IsNull(bystander.wheel);
        Assert.IsTrue(beltPlayer.playerAttached);
        Assert.AreSame(belt, beltPlayer.Attachment.Conveyor);
        Assert.That(beltPlayer.GetComponent<Rigidbody2D>().linearVelocity.x, Is.EqualTo(6f).Within(.001f));
    }

    [UnityTest]
    public IEnumerator HostPauseFreezesElapsedAndRecreatedHostStartsFresh()
    {
        var player = NewPlayer();
        var oxygen = new GameObject("paused oxygen").AddComponent<Oxygen>();
        typeof(Oxygen).GetField("player", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(oxygen, player);
        yield return null;
        yield return new WaitForFixedUpdate();
        host.SendMessage("OnApplicationPause", true);
        float time = host.Session.ElapsedSeconds;
        float air = oxygen.air;
        yield return null;
        yield return new WaitForFixedUpdate();
        Assert.AreEqual(time, host.Session.ElapsedSeconds);
        Assert.AreEqual(air, oxygen.air);
        host.SendMessage("OnApplicationPause", false);
        yield return null;
        yield return new WaitForFixedUpdate();
        Assert.Greater(host.Session.ElapsedSeconds, time);
        host.Session.TryDie();
        UnityEngine.Object.Destroy(host.gameObject);
        yield return null;
        yield return new WaitForFixedUpdate();
        host = LevelSessionHost.GetOrCreate();
        Assert.AreEqual(SessionState.Playing, host.Session.State);
        Assert.AreEqual(0f, host.Session.ElapsedSeconds);
    }

    [Test]
    public void ResetStaticsRecoversExistingHostWithFreshSession()
    {
        host.Session.TryDie();
        SessionState? presented = null;
        host.Changed += value => presented = value.State;
        typeof(LevelSessionHost).GetMethod("ResetStatics", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).Invoke(null, null);
        typeof(LevelSessionHost).GetMethod("Bootstrap", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).Invoke(null, null);
        var recovered = LevelSessionHost.Current;
        Assert.AreSame(host, recovered);
        Assert.AreEqual(SessionState.Playing, recovered.Session.State);
        Assert.AreEqual(SessionState.Playing, presented);
        int count = 0;
        foreach (var candidate in UnityEngine.Object.FindObjectsByType<LevelSessionHost>())
            if (candidate.gameObject.scene == scene) count++;
        Assert.AreEqual(1, count);
    }

#if UNITY_EDITOR
    [UnityTest]
    public IEnumerator AlienAnimationAttackCallbackRoutesThroughSingleSessionDeath()
    {
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/AlienB.prefab");
        Assert.IsNotNull(prefab);
        var alien = UnityEngine.Object.Instantiate(prefab).GetComponent<AlienB>();
        alien.transform.position = new Vector3(100f, 100f);
        var player = NewPlayer();
        player.tag = "Player";
        var collider = player.gameObject.AddComponent<CircleCollider2D>();
        AddExplosion(player);
        yield return null;
        yield return new WaitForFixedUpdate();
        alien.Attack();
        alien.SendMessage("OnTriggerStay2D", collider);
        alien.SendMessage("OnTriggerStay2D", collider);
        yield return null;
        yield return new WaitForFixedUpdate();
        Assert.AreEqual(SessionState.Dead, host.Session.State);
        Assert.IsFalse(player);
        Assert.AreEqual(0, storage.Writes);
    }

    [UnityTest]
    public IEnumerator RealFinishPrefabPhysicsSharesSaveAndDestroyAcrossBothLegacyAdapters()
    {
        temporaryDirectory = Path.Combine(Path.GetTempPath(), "Worms-Gameplay-" + Guid.NewGuid().ToString("N"));
        storage.Inner = new JsonFileSaveStorage(temporaryDirectory);
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Finish.prefab");
        Assert.IsNotNull(prefab);
        var finish = UnityEngine.Object.Instantiate(prefab);
        finish.transform.position = Vector3.zero;
        Assert.IsNotNull(finish.GetComponent<Finish>());
        Assert.IsNotNull(finish.GetComponent<SaveGame>());
        var player = new GameObject("physics finisher");
        player.tag = "Player";
        player.AddComponent<Rigidbody2D>().gravityScale = 0f;
        player.AddComponent<CircleCollider2D>().radius = .1f;
        player.AddComponent<BoxCollider2D>().size = Vector2.one * .1f;
        var attachedPlayer = player.AddComponent<Player>();
        attachedPlayer.AttachToWheel(NewWheel(), Vector2.zero);
        Assert.That(player.GetComponent<Rigidbody2D>().bodyType, Is.EqualTo(RigidbodyType2D.Kinematic));
        int changes = 0;
        host.Changed += value => changes++;
        yield return new WaitForFixedUpdate();
        yield return null;
        yield return new WaitForFixedUpdate();
        Assert.IsFalse(player);
        Assert.AreEqual(SessionState.Completed, host.Session.State);
        Assert.AreEqual(1, changes);
        Assert.AreEqual(1, storage.Writes);
        Assert.IsTrue(File.Exists(Path.Combine(temporaryDirectory, "progress-v1.json")));
        Assert.AreEqual("test-level", storage.Inner.Load().Levels[0].LevelId);
        Assert.IsFalse(host.Session.TryDie());
    }
#endif
}
