# Level completion and difficulty report

Generated from completed Unity validation runs without an AI service.

Evaluated: 20; certified routes: 19.

**Content blocker — level-20:** No active authored finish trigger.

Completion times describe the discovered route at its recorded rendering profile; they are not best times.
Timing demand describes measured execution precision. Overall human difficulty and learnability are uncalibrated; repeated failures are expected for this game.

| Level | Completion (s) | FPS | Jumps | Timing demand | Tightest sampled span (ms) | Tight jumps / longest chain | Successful timing variations |
| --- | ---: | ---: | ---: | --- | ---: | ---: | ---: |
| level-01 | 7.96 | 30 | 5 | High | 0 | 4/4 | 15/37 |
| level-02 | 8.54 | 30 | 4 | High | 0 | 3/3 | 7/24 |
| level-03 | 9.32 | 30 | 5 | High | 0 | 3/3 | 19/49 |
| level-04 | 9.86 | 30 | 4 | High | 0 | 4/4 | 4/20 |
| level-05 | 15.62 | 30 | 5 | High | 0 | 3/2 | 12/37 |
| level-06 | 13.68 | 30 | 8 | High | 0 | 7/7 | 13/52 |
| level-07 | 10.76 | 30 | 6 | High | 0 | 5/5 | 9/34 |
| level-08 | 10.42 | 30 | 9 | High | 0 | 9/9 | 10/45 |
| level-09 | 23.00 | 30 | 11 | High | 0 | 8/8 | 52/99 |
| level-10 | 2.60 | 30 | 1 | Moderate | 40 | 0/0 | 3/5 |
| level-11 | 10.70 | 30 | 5 | High | 0 | 5/5 | 5/25 |
| level-12 | 13.18 | 30 | 8 | High | 0 | 6/6 | 36/64 |
| level-13 | 13.98 | 30 | 8 | High | 0 | 8/8 | 9/40 |
| level-14 | 12.42 | 30 | 6 | High | 0 | 5/5 | 31/50 |
| level-15 | 12.00 | 30 | 8 | High | 0 | 8/8 | 8/40 |
| level-16 | 14.78 | 30 | 11 | High | 0 | 10/10 | 17/64 |
| level-17 | 13.32 | 30 | 7 | High | 0 | 5/5 | 18/55 |
| level-18 | 16.74 | 30 | 10 | High | 0 | 9/9 | 12/48 |
| level-19 | 22.16 | 30 | 14 | High | 0 | 14/14 | 15/70 |

## Rating rules

Only complete timing analysis is rated. For each jump, take its widest sampled success window; the tightest of those windows determines the span above.
Timing demand is High for a sampled span ≤20 ms, Moderate for ≤60 ms, otherwise Low. Attempt counts, sampled failure ratios and total jump count do not determine this label. Tight jumps and consecutive tight-jump chains are reported separately.
A 0 ms sampled span means one grid point succeeded, not a literally zero-width input window. The simulation grid is 20 ms. Consult each run request for its sampling radius; combined errors, alternative routes and human retry/learning costs remain separate measures.
These samples vary exact launch ticks, not button-down/hold intervals. Held input and buffering can make human execution more forgiving, especially for jumps immediately after attachment.
An easier alternative route may exist. Incomplete analysis stays Not rated.

## Unresolved searches and content issues

| Level | Status | Reason |
| --- | --- | --- |
| level-20 | InvalidContent | No active authored finish trigger. |

Inconclusive searches do not prove impossibility. A missing authored finish is a content blocker.

## Evidence

- docs/LEVEL_VALIDATION_EVIDENCE.json
