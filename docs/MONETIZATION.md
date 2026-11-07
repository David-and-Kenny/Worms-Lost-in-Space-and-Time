# AdMob and Remove Ads

## Implemented behavior

The modern integration uses Google Mobile Ads Unity 11.6.0 and Unity IAP 5.5.0.
Android currently uses the plugin's standard `play-services-ads:25.5.0` backend,
which falls within the Families program's published 19.0.0-or-later SDK range.
Google now prefers its Next-Gen Android backend; standard 25.x remains supported
until June 30, 2027. Revisit that migration before this date and verify the exact
replacement's Families certification before switching the child-safe configuration.
The 2015 SDK remains removed; its original component/GUID is an inert compatibility
adapter. No Firebase, UGS login, cloud backend, analytics SDK, banner, rewarded ad,
subscription, or additional ad network is required.

Three completed runs OR 25 failed attempts make an interstitial eligible. Death
and Retry belong to one attempt, and a manual restart of a live run also counts
once. Replays count as completions. Counters are application-session state, not
progress data. No ad appears in the first 300 foreground seconds, and at least
180 foreground seconds separate opened ads. Background/ad/settings time and the
first frame across a pause/focus/presentation boundary are excluded. Cached ads
expire against real elapsed time, so backgrounding cannot extend their lifetime.
An opened ad resets both counters; a missing/failed ad leaves them eligible for
the next natural transition. No catch-up burst occurs.

Completion is saved before the result screen appears. An eligible ad runs on the
next navigation out of a completed run. A failed run only displays an ad when
restarting, not during death effects or when abandoning the level for a menu.
There is no wait for loading an unavailable ad. Duplicate taps/SDK callbacks and
stale callbacks cannot initiate duplicate or obsolete navigation. Audio and time
scale are restored after presentation; the closing touch is consumed until release.

## Safe committed defaults

`Assets/Resources/Configuration/MonetizationSettings.asset` has production ads,
purchase initialization, and test preview OFF. Normal developer testing makes
**zero ad requests**. Setting Preview Test Ads explicitly enables Google sample
ad units in the Editor/development players; it never selects production ad units.
SDK app settings also ship with Google's sample app IDs. Sample ads do not earn
revenue. Do not test production ads or click your own live ads.

Batch-mode tests do not initialize the monetization host, platform SDKs, or real
purchase storage. Domain tests inject storage; file tests use disposable folders.
Editor/desktop builds never initialize a real purchasing store. An enabled test
preview can exercise ads without live store products.

No age/download gate is added. The integration conservatively marks **all** ad
requests child-directed and under age of consent, sets the Child age treatment
and G content limit, and disables publisher first-party ID. UMP uses the matching
under-age signal; this can suppress consent forms and reduce ad availability.
There is no ATT/IDFA permission prompt. These are conservative technical defaults,
not a declaration that store audience/Kids policies are complete. Before release,
review the actual intended audience, Google Families eligibility for this SDK
version, Apple category requirements, and privacy disclosures. Do not merely
declare 13+ to avoid requirements when children are an intended audience.
The release review must also examine the merged Android manifest's advertising-ID
permission and determine whether a neutral age screen is required for the final
declared audience; child-protected requests alone do not settle store policy.

## Purchase behavior

The one product is `remove_ads`, NonConsumable, with a planned US base price of
$2.99. Actual prices come from store metadata and must be configured in each store.
Main/world menus contain an editable settings panel with purchase, restore,
privacy choices (when UMP requires them), policy, and Back controls.

Unity IAP connects, fetches products/purchases, and checks entitlement. Google
receipts are validated using the Play public licensing key and application ID;
Apple purchases use the SDK's StoreKit 2 verified transaction path and signed JWS.
There is no custom receipt-verification server. A modified client can bypass local
checks; this is the accepted limit of the store-based design.

Ownership is persisted before transaction confirmation. A failed write suppresses
ads for the current session but leaves the order unconfirmed for redelivery.
Repeated orders grant the same boolean entitlement idempotently. Deferred/failed
purchases do not grant ownership. Existing non-consumables restore through their
store account, including reinstall. Android-to-iOS transfer is not implemented.

`remove-ads-v1.txt` is an atomic, backed-up ownership cache, separate from progress
JSON. Cached ownership survives offline use; an explicit authoritative store
NotEntitled result clears it. A failed store check never revokes cached ownership.
Unresolved ownership suppresses production ads, even for a new player. Purchases
can be restored from Settings. No developer progress files are migrated or erased.

## Account and release handoff

1. Audit access to existing Google publisher and Play Console accounts. Reuse the
   Google account associated with the existing AdSense/publisher account. Complete
   AdMob verification, payments, tax, and payout steps requested by Google.
2. Confirm existing Play package and Apple bundle/store records before changing
   `com.KOGaming.worms`. Preserve published identifiers. Enroll/verify Apple
   Developer membership and App Store Connect access; accept paid-app agreements
   and configure banking/tax information on both stores.
3. Create Android and iOS apps in AdMob, plus one interstitial ad unit for each.
   Apps may initially be unpublished. Supply four IDs: Android/iOS app IDs and
   Android/iOS interstitial IDs. These IDs are not account credentials.
4. Publish a developer website, HTTPS privacy policy, support contact, and root
   `app-ads.txt` containing the exact authorized seller line from AdMob. Link the
   developer website on both listings. Complete AdMob verification/readiness review.
5. Create/activate the `remove_ads` non-consumable on each store, configure $2.99
   US base pricing/localizations, sandbox testers, and agreements. Supply Google's
   **public** Play licensing key for Android local receipt validation. Never place
   service-account credentials, signing keys, or account passwords in this asset.
6. Configure AdMob Privacy & messaging for the reviewed audience and regions.
   Complete Google Play ads/target audience/Data safety and Apple age rating/
   privacy declarations using the actual installed SDK behavior. Review the
   SDK's iOS privacy manifest and attribution configuration in the exported build.
7. Enter platform app IDs in BOTH Worms settings and Assets > Google Mobile Ads >
   Settings. Enter ad-unit IDs, policy URL, public Play license key, and product ID
   in Worms settings. Enable Store Purchases only after products are testable.
8. Run the device acceptance cases. Set Audience And Privacy Reviewed only when
   those declarations are settled. Enable Production Enabled only after the
   account/app approvals and test evidence are recorded. A production mobile build
   with ads enabled rejects missing/sample IDs, mismatched SDK app IDs, missing
   purchasing/privacy configuration, or a missing Android public licensing key.

Production remains off until these external steps are complete. Having source
code/build tests pass does not prove live ad delivery or store approval.

## Packages and reproducibility

The Google SDK is installed through Unity Package Manager from the committed
`Packages/com.google.ads.mobile-11.6.0.tgz`, downloaded from Google's documented
OpenUPM distribution. SHA-1: `dc1fb329e84b18006570c8a30671f43ea3aff669`.
The tarball includes Google's license and platform bridge binaries, not generated
build output. Unity resolves its dependency manager and Unity IAP dependencies;
the manifest/lockfile and SDK settings are committed. Update the archive and
package via the explicit editor installer when deliberately upgrading.

Android builds select `-buildTarget Android` before importing scripts so the
SDK's Android-only editor preprocessor runs. Commit its Gradle templates and
shared resolver settings; the resolver's dependency-state cache is ignored.

On Windows, a short checkout path avoids long-path failures in Unity IAP's
deployment dependency. The implementation worktree was validated through a
temporary `W:` mapping; this drive is not a project/runtime dependency.

## Implementation verification — November 6, 2026

- Complete `check-unity.ps1 -Check All`: repository hygiene, all enabled scenes
  and prefab bindings, **116 EditMode tests**, **40 PlayMode tests**, Windows
  development build, and Android ARM64 development build passed.
- All eight validation-runner regression cases passed, including the new check
  that Android is selected before SDK preprocessing.
- MainMenu's 64 and WorldMenu's 35 original serialized object IDs were retained.
- Five recorded input routes (levels 1, 3, 4, 8 and 10) were freshly certified
  through normal input at the reference 60 Hz policy after the final source
  change. No completion API, teleport, manual certificate-hash edit or AI service
  was used. All five fresh certificates then passed strict replay (13 trials per
  level) before being copied byte-for-byte into ValidationRoutes. This does not establish other frame-rate equivalence or all-level
  reachability. Re-run certification after integrating the separate physics work.
- Earlier runs exposed an obsolete fixed button-count assertion and an Android
  preprocessor target issue. Both were corrected; the full final run passed.
- **Not executed:** live ad delivery, physical-device rendering/input/lifecycle,
  sandbox purchase/restore on actual stores, and macOS/Xcode/iOS build/device checks.
  Production approval, audience declarations and account/ID configuration remain
  external release work, not passing automated tests.

## Sources and maintenance references

- [Google Unity setup](https://developers.google.com/admob/unity/quick-start)
- [Google consent setup](https://developers.google.com/admob/unity/privacy)
- [Google audience policy](https://support.google.com/googleplay/android-developer/answer/9867159)
- [Families SDK versions](https://support.google.com/googleplay/android-developer/answer/12955712)
- [Android SDK support timeline](https://ads-developers.googleblog.com/2026/07/announcing-google-mobile-ads-next-gen.html)
- [Apple Kids category](https://developer.apple.com/app-store/review/guidelines/#kids-category)
- [Unity receipt validation](https://docs.unity.com/en-us/iap/receipt-validation)
- [Unity purchase restoration](https://docs.unity.com/en-us/iap/restore-purchases)
