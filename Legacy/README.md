# Removed legacy integration

The unsupported 2015 Google Mobile Ads SDK, Android/iOS native plugins, and demo
implementation have been removed from the current source tree. They are not needed
to build the game and remain recoverable from original commit `9bdc650` in Git history.
The scene-attached demo script retains its original GUID as an inactive placeholder.

Do not restore this SDK to Assets. The maintained Google Mobile Ads Unity and
Unity IAP integrations now live under Scripts/Integrations/Monetization, with
production and developer ads disabled by default. See ../docs/MONETIZATION.md
for account setup and release acceptance. Original sources remain recoverable
from Git history.
