using System;
using System.IO;
using NUnit.Framework;
using Worms.Core;

public sealed class MonetizationPolicyTests
{
    private sealed class Ads : IInterstitialAds
    {
        public bool Ready = true;
        public bool Throw;
        public int Shows;
        public Action Opened;
        public Action Finished;
        public bool IsReady => Ready;
        public void Show(Action opened, Action finished)
        {
            Shows++;
            if (Throw) throw new InvalidOperationException();
            Opened = opened;
            Finished = finished;
        }
        public void Dispose() { }
    }
    private sealed class Storage : IEntitlementStorage
    {
        public bool Owned;
        public bool Fail;
        public bool Load() => Owned;
        public void Save(bool owned) { if (Fail) throw new IOException("Injected write failure"); Owned = owned; }
    }

    private static void FailRuns(AdBreakPolicy policy, int count)
    {
        for (int i = 0; i < count; i++) { policy.BeginRun(); policy.RecordOutcome(false); }
    }
    private static AdBreakPolicy DuePolicy()
    {
        var policy = new AdBreakPolicy();
        FailRuns(policy, 25);
        return policy;
    }

    [Test]
    public void ForegroundClockExcludesBackgroundAndResumeHitches()
    {
        var clock = new ForegroundClock();
        clock.Tick(1, true);
        clock.Tick(10, true);
        clock.Tick(600, false);
        clock.Tick(600, true);
        clock.Tick(1, true);
        Assert.That(clock.Seconds, Is.EqualTo(11));
        clock.Interrupt();
        clock.Tick(1800, true);
        clock.Tick(1, true);
        Assert.That(clock.Seconds, Is.EqualTo(12));
    }

    [TestCase(-1)]
    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    public void ForegroundClockRejectsInvalidDeltas(double delta)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ForegroundClock().Tick(delta, true));
    }

    [Test]
    public void TwentyFifthFailedRunBecomesEligibleAndRetryDoesNotCountDeathTwice()
    {
        var policy = new AdBreakPolicy();
        FailRuns(policy, 24);
        Assert.That(policy.CanShow(300, true, false), Is.False);
        policy.BeginRun();
        policy.RecordOutcome(false);
        policy.RecordOutcome(false);
        Assert.That(policy.FailedRuns, Is.EqualTo(25));
        Assert.That(policy.CanShow(300, true, false), Is.True);
    }

    [Test]
    public void ThreeCompletionsIncludingReplaysBecomeEligible()
    {
        var policy = new AdBreakPolicy();
        for (int i = 0; i < 3; i++) { policy.BeginRun(); policy.RecordOutcome(true); }
        Assert.That(policy.CanShow(300, true, false), Is.True);
        Assert.That(policy.FailedRuns, Is.Zero);
    }

    [TestCase(299.99, true, false, false)]
    [TestCase(300, true, false, true)]
    [TestCase(300, false, false, false)]
    [TestCase(300, true, true, false)]
    [TestCase(double.NaN, true, false, false)]
    [TestCase(double.PositiveInfinity, true, false, false)]
    public void TimingPrivacyAndOwnershipGateAds(double time, bool allowed, bool owned, bool expected)
    {
        Assert.That(DuePolicy().CanShow(time, allowed, owned), Is.EqualTo(expected));
    }

    [Test]
    public void OpenedAdResetsBothCountersAndAppliesSharedCooldown()
    {
        var policy = DuePolicy();
        policy.BeginRun();
        policy.RecordOutcome(true);
        policy.RecordAdOpened(300);
        Assert.That(policy.CompletedRuns, Is.Zero);
        Assert.That(policy.FailedRuns, Is.Zero);
        FailRuns(policy, 25);
        Assert.That(policy.CanShow(479.99, true, false), Is.False);
        Assert.That(policy.CanShow(480, true, false), Is.True);
    }

    [Test]
    public void UnavailableAdNeverWaitsOrConsumesCounter()
    {
        var policy = DuePolicy();
        var ads = new Ads { Ready = false };
        var controller = new AdBreakController(policy, ads, () => 300);
        int navigations = 0;
        controller.Navigate(true, true, false, () => navigations++);
        Assert.That(navigations, Is.EqualTo(1));
        Assert.That(ads.Shows, Is.Zero);
        Assert.That(policy.FailedRuns, Is.EqualTo(25));
    }

    [Test]
    public void RepeatedTapsAndCallbacksNavigateExactlyOnce()
    {
        var policy = DuePolicy();
        var ads = new Ads();
        var controller = new AdBreakController(policy, ads, () => 300);
        int navigations = 0;
        controller.Navigate(true, true, false, () => navigations++);
        controller.Navigate(true, true, false, () => navigations++);
        Assert.That(navigations, Is.Zero);
        Assert.That(ads.Shows, Is.EqualTo(1));
        ads.Opened();
        ads.Opened();
        ads.Finished();
        ads.Finished();
        Assert.That(navigations, Is.EqualTo(1));
        Assert.That(policy.FailedRuns, Is.Zero);
    }

    [Test]
    public void FailedPresentationContinuesAndKeepsOpportunity()
    {
        var policy = DuePolicy();
        var ads = new Ads { Throw = true };
        var controller = new AdBreakController(policy, ads, () => 300);
        int navigations = 0;
        controller.Navigate(true, true, false, () => navigations++);
        Assert.That(navigations, Is.EqualTo(1));
        Assert.That(policy.CanShow(300, true, false), Is.True);
    }

    [Test]
    public void CancelledPresentationCannotCompleteANewerTransition()
    {
        var ads = new Ads();
        var controller = new AdBreakController(DuePolicy(), ads, () => 300);
        int navigations = 0;
        controller.Navigate(true, true, false, () => navigations++);
        var staleClose = ads.Finished;
        var staleOpen = ads.Opened;
        controller.Cancel();
        controller.Navigate(true, true, false, () => navigations++);
        staleOpen();
        staleClose();
        Assert.That(navigations, Is.Zero);
        ads.Finished();
        Assert.That(navigations, Is.EqualTo(1));
    }

    [Test]
    public void NonTransitionOpportunityDoesNotShowEvenWhenDue()
    {
        var ads = new Ads();
        var controller = new AdBreakController(DuePolicy(), ads, () => 300);
        controller.Navigate(false, true, false, () => { });
        Assert.That(ads.Shows, Is.Zero);
    }

    [Test]
    public void CachedPurchaseSuppressesAdsWhileOwnershipIsUnresolved()
    {
        var entitlement = new RemoveAdsEntitlement(new Storage { Owned = true });
        Assert.That(entitlement.Owned, Is.True);
        Assert.That(entitlement.IsResolved, Is.False);
        entitlement.MarkUnresolved();
        Assert.That(entitlement.Owned, Is.True);
    }

    [Test]
    public void GrantIsIdempotentAndSurvivesANewSession()
    {
        var storage = new Storage();
        var entitlement = new RemoveAdsEntitlement(storage);
        Assert.That(entitlement.ApplyVerifiedOwnership(true), Is.True);
        Assert.That(entitlement.ApplyVerifiedOwnership(true), Is.True);
        Assert.That(new RemoveAdsEntitlement(storage).Owned, Is.True);
    }

    [Test]
    public void FailedPersistenceDoesNotConfirmOwnershipResolutionButSuppressesAds()
    {
        var entitlement = new RemoveAdsEntitlement(new Storage { Fail = true });
        Assert.That(entitlement.ApplyVerifiedOwnership(true), Is.False);
        Assert.That(entitlement.Owned, Is.True);
        Assert.That(entitlement.IsResolved, Is.False);
        Assert.That(entitlement.Error, Is.Not.Empty);
    }

    [Test]
    public void VerifiedRevocationClearsCache()
    {
        var storage = new Storage { Owned = true };
        var entitlement = new RemoveAdsEntitlement(storage);
        Assert.That(entitlement.ApplyVerifiedOwnership(false), Is.True);
        Assert.That(entitlement.Owned, Is.False);
        Assert.That(storage.Owned, Is.False);
    }
}
