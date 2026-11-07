# Level completion and difficulty report

Generated from completed Unity validation runs without an AI service.

Evaluated: 20; certified routes: 4.

**Content blocker — level-20:** No active authored finish trigger.

Completion times describe the discovered route at its recorded rendering profile; they are not best times.
Difficulty is a provisional rating of that route, not a human playtest rating of every possible route.

| Level | Completion (s) | FPS | Jumps | Provisional difficulty | Tightest sampled span (ms) | Successful timing variations |
| --- | ---: | ---: | ---: | --- | ---: | ---: |
| level-01 | 7.96 | 60 | 5 | High | 0 | 15/125 |
| level-03 | 9.32 | 60 | 5 | High | 0 | 19/125 |
| level-04 | 9.86 | 60 | 4 | High | 0 | 4/100 |
| level-10 | 2.60 | 60 | 1 | High | 40 | 3/25 |

## Rating rules

Only complete timing analysis is rated. For each jump, take its widest sampled success window; the tightest of those windows determines the span above.
High: span ≤20 ms, sampled success ratio <25%, or at least eight jumps. Moderate: span ≤60 ms, ratio <50%, or at least five jumps. Otherwise Low; zero-jump routes are Low.
A 0 ms sampled span means one grid point succeeded, not a literally zero-width input window. The 20 ms grid samples single-jump offsets within ±240 ms; combined errors and other waiting periods are untested.
An easier alternative route may exist. Incomplete analysis stays Not rated.

## Unresolved searches and content issues

| Level | Status | Reason |
| --- | --- | --- |
| level-02 | Inconclusive | No route found within the configured search frontier/budget. This is not proof of impossibility. |
| level-05 | Inconclusive | No route found within the configured search frontier/budget. This is not proof of impossibility. |
| level-06 | Inconclusive | No route found within the configured search frontier/budget. This is not proof of impossibility. |
| level-07 | Inconclusive | No route found within the configured search frontier/budget. This is not proof of impossibility. |
| level-08 | Inconclusive | No route found within the configured search frontier/budget. This is not proof of impossibility. |
| level-09 | Inconclusive | No route found within the configured search frontier/budget. This is not proof of impossibility. |
| level-11 | Inconclusive | No route found within the configured search frontier/budget. This is not proof of impossibility. |
| level-12 | Inconclusive | No route found within the configured search frontier/budget. This is not proof of impossibility. |
| level-13 | Inconclusive | No route found within the configured search frontier/budget. This is not proof of impossibility. |
| level-14 | Inconclusive | No route found within the configured search frontier/budget. This is not proof of impossibility. |
| level-15 | Inconclusive | No route found within the configured search frontier/budget. This is not proof of impossibility. |
| level-16 | Inconclusive | No route found within the configured search frontier/budget. This is not proof of impossibility. |
| level-17 | Inconclusive | No route found within the configured search frontier/budget. This is not proof of impossibility. |
| level-18 | Inconclusive | No route found within the configured search frontier/budget. This is not proof of impossibility. |
| level-19 | Inconclusive | No route found within the configured search frontier/budget. This is not proof of impossibility. |
| level-20 | InvalidContent | No active authored finish trigger. |

Inconclusive searches do not prove impossibility. A missing authored finish is a content blocker.

## Evidence

- TestResults/LevelValidation/FixedTick-Catalog-Final/summary.json
- TestResults/LevelValidation/FixedTick-Deep03/summary.json
- TestResults/LevelValidation/FixedTick-Analysis04/summary.json
- TestResults/LevelValidation/FixedTick-Deep08/summary.json
