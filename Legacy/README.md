# Removed legacy integration

The unsupported 2015 Google Mobile Ads SDK, Android/iOS native plugins, and demo
implementation have been removed from the current source tree. They are not needed
to build the game and remain recoverable from original commit `9bdc650` in Git history.
The scene-attached demo script retains its original GUID as an inactive placeholder.

Do not restore this SDK to Assets. Reintegrate a maintained Google Mobile Ads
Unity SDK, test ad units, consent handling, and platform configuration as a separate
task before shipping. Original sources are also recoverable from Git history.
