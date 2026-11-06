# Archived integrations

`GoogleMobileAds/` contains the original 2015 Google Mobile Ads SDK, native Android
and iOS plugins, and original demo script. It is outside Unity's `Assets` directory
so modern Unity does not import the unsupported Android library projects or compile
the obsolete integration. The scene-attached demo script retains its original GUID
as a disabled-ad placeholder. Gameplay can be developed without contacting ad servers.

Do not copy this SDK back into Assets. Reintegrate a maintained Google Mobile Ads
Unity SDK, test ad units, consent handling, and platform configuration as a separate
task before shipping. Original sources are also recoverable from Git history.
