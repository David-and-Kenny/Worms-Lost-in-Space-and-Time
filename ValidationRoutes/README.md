# Maintained level routes

These five 60 Hz certificates cover levels 1, 3, 4, 8 and 10. Run them without
searching or using an AI service:

```powershell
./scripts/validate-levels.ps1 -Operation batch -ReplayDirectory ./ValidationRoutes
```

Source/editor changes deliberately make them stale. Follow the proposal and fresh
certification procedure in [LEVEL_VALIDATION.md](../docs/LEVEL_VALIDATION.md), review
the newly verified certificates and run strict replay before replacing these files.
Do not edit fingerprints to obtain a pass. Reports and logs belong in ignored
`TestResults/`, not this directory.

The catalog scan also found a route for Level 7, but later normal-physics
verification rejected it. It is excluded here. These certificates do not imply
that every catalog level is viable, or that timing is equivalent across devices
or frame rates. See [the evidence](../docs/LEVEL_VALIDATION_RESULTS.md).
