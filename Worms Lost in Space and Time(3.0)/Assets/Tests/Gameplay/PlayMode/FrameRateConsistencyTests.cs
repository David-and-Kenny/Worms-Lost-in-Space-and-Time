using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Worms;
using Worms.Configuration;
using Worms.Core;
using Worms.Gameplay;

public sealed class FrameRateConsistencyTests
{
    private sealed class Storage : ISaveStorage
    {
        public SaveData Load() => null;
        public void Save(SaveData data) { }
    }

    private sealed class TickCommands : IPlayerTickCommands
    {
        public Action<int> Observe;
        public bool JumpRequested(int tick)
        {
            Observe?.Invoke(tick);
            return tick == 25;
        }
    }

    private sealed class HeldInput : IPlayerInput
    {
        public bool Held;
        public bool JumpHeld => Held;
    }

    private float originalCapture;
    private Scene previous;
    private Scene fixture;

    [SetUp]
    public void SetUp()
    {
        originalCapture = Time.captureDeltaTime;
        previous = SceneManager.GetActiveScene();
        GameServices.ConfigureForTests(Resources.Load<LevelCatalog>("Configuration/LevelCatalog"), new Storage());
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.captureDeltaTime = originalCapture;
        if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        if (fixture.IsValid() && fixture.isLoaded) yield return SceneManager.UnloadSceneAsync(fixture);
        GameServices.ResetForTests();
    }

    [UnityTest]
    public IEnumerator RotationLaunchFlightAndHazardClockAgreeAcrossRenderingSchedules()
    {
        Vector2? referencePosition = null;
        Vector2 referenceVelocity = default;
        Vector2 referenceHazard = default;
        foreach (int rate in new[] { 30, 60, 120, 0 })
        {
            fixture = SceneManager.CreateScene("Fixed-tick-" + Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(fixture);
            var player = new GameObject("Player").AddComponent<Player>();
            var body = player.GetComponent<Rigidbody2D>();
            body.gravityScale = 0;
            var wheel = new GameObject("Wheel").AddComponent<Wheel>();
            wheel.speed = 90;
            wheel.direction = 1;
            wheel.holder = new GameObject("Holder").transform;
            wheel.holder.SetParent(wheel.transform);
            player.AttachToWheel(wheel, Vector2.right);
            var hazard = new GameObject("Moving hazard").AddComponent<MovingSpikes>();
            hazard.startPos = new GameObject("Start").transform;
            hazard.endPos = new GameObject("End").transform;
            hazard.endPos.position = Vector2.right * 8;
            hazard.speed = .5f;

            Vector2 position = default, velocity = default, hazardPosition = default;
            int launchTick = -1;
            bool observed = false;
            player.TraversalChanged += (source, kind, surface) =>
            {
                if (kind != PlayerTraversalKind.Launched) return;
                launchTick = player.Simulation.Tick;
                Assert.That(body.bodyType, Is.EqualTo(RigidbodyType2D.Dynamic));
                Vector2 heading = Quaternion.Euler(0, 0, -45) * Vector2.up;
                Assert.That(Vector2.Distance(body.linearVelocity, heading * 10), Is.LessThan(.001f));
            };
            player.TickCommands = new TickCommands { Observe = tick =>
            {
                if (tick != 75) return;
                position = body.position;
                velocity = body.linearVelocity;
                hazardPosition = hazard.transform.position;
                observed = true;
            } };
            int frame = 0;
            while (!observed && frame < 500)
            {
                Time.captureDeltaTime = rate == 0 ? new[] { .008f, .041f, .016f, .035f }[frame % 4] : 1f / rate;
                frame++;
                yield return null;
            }
            Assert.That(observed, Is.True, "Simulation did not advance.");
            Assert.That(launchTick, Is.EqualTo(25));
            Assert.That(velocity.magnitude, Is.EqualTo(10).Within(.001f));
            if (referencePosition.HasValue)
            {
                Assert.That(Vector2.Distance(position, referencePosition.Value), Is.LessThan(.001f), "Flight at profile " + rate);
                Assert.That(Vector2.Distance(velocity, referenceVelocity), Is.LessThan(.001f));
                Assert.That(Vector2.Distance(hazardPosition, referenceHazard), Is.LessThan(.001f));
            }
            else { referencePosition = position; referenceVelocity = velocity; referenceHazard = hazardPosition; }
            SceneManager.SetActiveScene(previous);
            yield return SceneManager.UnloadSceneAsync(fixture);
        }
    }

    [UnityTest]
    public IEnumerator BriefObservedTapLaunchesOnceAndPauseDropsPendingInput()
    {
        fixture = SceneManager.CreateScene("Buffered-input-" + Guid.NewGuid().ToString("N"));
        SceneManager.SetActiveScene(fixture);
        var player = new GameObject("Player").AddComponent<Player>();
        player.Simulation.AutomaticSteps = false;
        var body = player.GetComponent<Rigidbody2D>();
        body.gravityScale = 0;
        var input = new HeldInput();
        player.InputSource = input;
        var wheel = new GameObject("Wheel").AddComponent<Wheel>();
        wheel.speed = 0;
        wheel.holder = new GameObject("Holder").transform;
        wheel.holder.SetParent(wheel.transform);
        player.AttachToWheel(wheel, Vector2.up);
        Assert.That(body.bodyType, Is.EqualTo(RigidbodyType2D.Kinematic));
        Assert.That(body.useFullKinematicContacts, Is.True);
        Assert.That(body.linearVelocity, Is.EqualTo(Vector2.zero));

        int launches = 0;
        player.TraversalChanged += (source, kind, surface) => { if (kind == PlayerTraversalKind.Launched) launches++; };
        input.Held = true;
        yield return null;
        input.Held = false;
        yield return null;
        player.Simulation.Step();
        Assert.That(launches, Is.EqualTo(1));
        Assert.That(body.linearVelocity.magnitude, Is.EqualTo(10).Within(.001f));
        Assert.That(body.bodyType, Is.EqualTo(RigidbodyType2D.Dynamic));
        Assert.That(body.useFullKinematicContacts, Is.False);
        player.Simulation.Step();
        Assert.That(launches, Is.EqualTo(1));

        var other = new GameObject("Other wheel").AddComponent<Wheel>();
        other.holder = new GameObject("Other holder").transform;
        other.holder.SetParent(other.transform);
        player.AttachToWheel(other, Vector2.up);
        input.Held = true;
        yield return null;
        player.SendMessage("OnApplicationPause", true);
        input.Held = false;
        yield return null;
        player.Simulation.Step();
        Assert.That(player.playerAttached, Is.True);
        Assert.That(launches, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator CountdownForcedLaunchAndOxygenAgreeAcrossFrameRates()
    {
        int? expectedDetachTick = null;
        float? expectedAir = null;
        foreach (int rate in new[] { 30, 60, 120 })
        {
            fixture = SceneManager.CreateScene("Fixed-timers-" + Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(fixture);
            Time.captureDeltaTime = 1f / rate;
            var player = new GameObject("Player").AddComponent<Player>();
            var body = player.GetComponent<Rigidbody2D>();
            body.gravityScale = 0;
            var wheel = new GameObject("Breakable wheel").AddComponent<Wheel>();
            wheel.holder = new GameObject("Holder").transform;
            wheel.holder.SetParent(wheel.transform);
            var countdown = wheel.gameObject.AddComponent<CountdownExplode>();
            countdown.timer = .3f;
            var oxygen = new GameObject("Oxygen").AddComponent<Oxygen>();
            oxygen.air = 2;
            player.AttachToWheel(wheel, Vector2.right);
            int detachTick = -1;
            float air = -1;
            bool observed = false;
            player.TraversalChanged += (source, kind, surface) =>
            {
                if (kind != PlayerTraversalKind.ForcedDetach) return;
                Assert.That(detachTick, Is.EqualTo(-1), "Forced detach must happen once.");
                detachTick = player.Simulation.Tick;
                Assert.That(body.linearVelocity.magnitude, Is.EqualTo(5).Within(.001f));
            };
            player.TickCommands = new TickCommands { Observe = tick =>
            {
                if (tick != 50) return;
                air = oxygen.air;
                observed = true;
            } };
            for (int frame = 0; !observed && frame < 200; frame++) yield return null;
            Assert.That(observed, Is.True);
            Assert.That(detachTick, Is.InRange(15, 16));
            Assert.That(air, Is.EqualTo(1.02f).Within(.001f));
            if (expectedDetachTick.HasValue)
            {
                Assert.That(detachTick, Is.EqualTo(expectedDetachTick.Value));
                Assert.That(air, Is.EqualTo(expectedAir.Value).Within(.001f));
            }
            else { expectedDetachTick = detachTick; expectedAir = air; }
            SceneManager.SetActiveScene(previous);
            yield return SceneManager.UnloadSceneAsync(fixture);
        }
    }
}
