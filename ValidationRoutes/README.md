# Maintained level routes

These version 2 certificates record natural finish routes using 50 Hz simulation
ticks. Each file records its rendering profile. Run them without searching or
using an AI service:

```powershell
./scripts/validate-levels.ps1 -Operation batch -ReplayDirectory ./ValidationRoutes
```

Source/editor changes deliberately make them stale. Follow the proposal and fresh
certification procedure in [LEVEL_VALIDATION.md](../docs/LEVEL_VALIDATION.md), review
the newly verified certificates and run strict replay before replacing these files.
Do not edit fingerprints to obtain a pass. Reports and logs belong in ignored
`TestResults/`, not this directory.

For a fresh catalog iteration, run `./scripts/validate-catalog.ps1`. It treats
these inputs as proposals, certifies them again and searches unresolved levels
with complementary bounded profiles. Review its selected `Certificates/` folder
and strict replay result before replacing maintained files.

The [current completion and timing-demand report](../docs/FIXED_TICK_LEVEL_REPORT.md)
lists every evaluated level, including unresolved searches and invalid content.
These certificates do not establish that every catalog level is viable or that
device input latency and perceived difficulty are equivalent. See
[solver coverage](../docs/SOLVER_COVERAGE.md) for the search changes and
[fixed-tick movement](../docs/FIXED_TICK_GAMEPLAY.md) for the runtime timing policy.
