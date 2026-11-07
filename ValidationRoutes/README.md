# Maintained level routes

These four version 2 certificates cover levels 1, 3, 4 and 10 at a 60 FPS rendering
profile, using 50 Hz simulation ticks. Run them without
searching or using an AI service:

```powershell
./scripts/validate-levels.ps1 -Operation batch -ReplayDirectory ./ValidationRoutes
```

Source/editor changes deliberately make them stale. Follow the proposal and fresh
certification procedure in [LEVEL_VALIDATION.md](../docs/LEVEL_VALIDATION.md), review
the newly verified certificates and run strict replay before replacing these files.
Do not edit fingerprints to obtain a pass. Reports and logs belong in ignored
`TestResults/`, not this directory.

Level 8's old certificate could not be recertified after the timing change, even
with a five-minute search and uniform one-to-three-tick timing-offset proposals.
It needs manual playtesting or further search. It is unresolved, not proved
impossible, and its obsolete certificate is excluded. Level 7 also remains
unresolved. These certificates do not imply
that every catalog level is viable, or that timing is equivalent across devices
or devices. Current routes completed at 30/60/120 FPS; strict saved-route replay
passed in both checkouts. See [the evidence](../docs/FIXED_TICK_GAMEPLAY.md) and
[completion times/difficulty](../docs/FIXED_TICK_LEVEL_REPORT.md).
