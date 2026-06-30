# 2026-06-23 Weekly Closing Operational History

## Purpose

This document records the real human flow that was reconstructed in conversation and then aligned in the local operational data.

The goal is simple:

- preserve what really happened in the kitchen
- keep the two closed weeks understandable in natural language
- document the human corrections that were remembered later
- avoid re-learning the same context only from chat memory

This is an operational history document. It does not redefine backend rules. The base rules still live in:

- `WEEKLY_DOUGH_CLOSING_RULES.md`
- `WEEKLY_CARRYOVER_AND_FINISHED_RULES.md`
- `END_TO_END_WEEKLY_CARRYOVER_FLOW.md`

## Core Rules Confirmed By These Corrections

- `Weekly Closing` closes `Monday-Sunday`.
- `Daily Closing` and prep planning still operate on `Tuesday-Sunday`.
- Only dough that still physically exists can carry into the next week.
- A `Make Dough Load` is future dough, not ready dough.
- Dough becomes `Ready Now` only after `BallDough` is completed.
- Historical produced dough does not automatically mean live inventory.
- Historical reballed dough does not automatically mean live inventory.
- If tasks were forgotten in real life, the task history must be backfilled before the weekly carryover can be trusted.

## Enunciado 1: Closed Week Jun 8, 2026 - Jun 14, 2026

### What this week needed to preserve

At the end of this week, the kitchen did not truly reset to zero.

What mattered was not only what had been produced historically, but what was still physically present when the next service week started.

### Final authoritative closing for this week

The week `Monday Jun 8, 2026 - Sunday Jun 14, 2026` was left documented as:

- `NeededBalls = 1063`
- `ProducedBalls = 672`
- `UsedBalls = 923`
- `LostBalls = 60`
- `LeftoverReadyBalls = 296`
- `LeftoverAttentionBalls = 0`
- `LeftoverMixedLoads = 1`

### Human corrections that had to be remembered later

Later, it was clarified in natural language that the next service week did not begin from a fake clean slate.

The important remembered facts were:

- an older dough batch from `2026-06-11` was still part of real carryover and should not be mistaken for new current-week production
- there were also `4 reballed cases = 48 balls` physically on hand
- there was still `1 mixed load` pending balling

Operationally, this means the kitchen entered the next service week with:

- `296` regular ready balls
- `48` reballed balls physically on hand
- `1` mixed load still pending balling

### Why this mattered

If this week had been left as an artificial reset:

- the next week would start too low
- carryover would be understated
- the kitchen would appear to need more dough than it really needed

## Enunciado 2: Closed Week Jun 15, 2026 - Jun 21, 2026

### Daily usage that was already trusted

For the service week `Tuesday Jun 16, 2026 - Sunday Jun 21, 2026`, the daily closings were already the trusted record of what was actually used:

- `Tue Jun 16`: forecast `60`, actual used `80`
- `Wed Jun 17`: forecast `70`, actual used `80`
- `Thu Jun 18`: forecast `215`, actual used `200`
- `Fri Jun 19`: forecast `400`, actual used `325`
- `Sat Jun 20`: forecast `250`, actual used `230`
- `Sun Jun 21`: forecast `118`, actual used `95`

Total actual used for that service week:

- `1010 balls`

### What had been forgotten operationally

The missing part was not the daily closing.

What had been forgotten was the actual task timeline for dough loads and balling from the end of that week into Monday morning.

The remembered real flow was:

1. `Thu Jun 18`: a dough load was made.
2. `Fri Jun 19`: that Thursday load was balled.
3. `Fri Jun 19`: another dough load was made.
4. `Sat Jun 20`: the Friday load was balled.
5. `Sun Jun 21`: another dough load was made.
6. `Mon Jun 22`: the Sunday load was balled.
7. No extra Monday load should remain pending after that.
8. No extra pending `GenericDough` task should remain after that.

### What was wrong before the correction

Before the operational backfill, the data was telling a false story:

- a wrong `2026-06-17` load was still represented as a pending unballed batch
- stale pending tasks made it look like there was future dough that did not really exist
- the weekly closing ended as if there were `0` ready balls and `1` mixed load pending

That was not the real physical state.

### Final authoritative closing for this week

After reconstructing the real task flow, the week `Monday Jun 15, 2026 - Sunday Jun 21, 2026` was aligned to:

- `NeededBalls = 1113`
- `ProducedBalls = 1008`
- `UsedBalls = 1010`
- `LostBalls = 0`
- `LeftoverReadyBalls = 504`
- `LeftoverAttentionBalls = 0`
- `LeftoverMixedLoads = 0`

### Natural-language explanation of the ending state

At the moment the next week truly started, the kitchen had:

- `3 full ready lines`
- `1 line = 1 load = 14 cases x 12 balls = 168 balls`
- `3 lines = 504 ready balls`

There was no real mixed load left pending and no extra future load waiting to be counted.

### Why Monday Jun 22 was treated as part of the closing recovery

Operationally, `Mon Jun 22` was the morning when the `Sun Jun 21` load was finally turned into ready balls.

That balling action mattered because it changed the real carryover:

- before balling, that Sunday load was not ready
- after balling, it became part of the physically ready dough that the next week should inherit

So even though the closed service week ended on `Sun Jun 21`, the Monday morning balling had to be remembered to make the carryover truthful.

## Start Of The Next Week: Tue Jun 23, 2026

Once the forgotten tasks were backfilled and the weekly closing was corrected, the new week opened with:

- `WeeklyNeed = 943`
- `ReadyNow = 504`
- `FutureBalls = 0`
- `StillMissing = 439`

In natural language:

- the kitchen starts the new week with `3` full ready lines
- there is no fake pending load inflating future dough
- the real shortage is `943 - 504 = 439 balls`

## Human Corrections That Should Not Be Forgotten Again

These were not formula changes. They were human memory corrections about what really happened on the floor.

### Correction A: carry only what is physically left

The next week should inherit only:

- dough still ready on hand
- dough still on hand but attention
- dough still mixed and not yet balled

It should never inherit:

- all historical production
- all historical reballed dough
- a load that was already used up
- a stale pending task that no longer represents real dough

### Correction B: do not count the same load twice

One real load can appear in several technical records:

- `MakeDoughLoad`
- `BallDough`
- `DoughBatch`
- `DoughBatchQualityRecord`
- `DoughInventorySnapshot`

Those records describe the same physical dough at different stages. They are not separate loads.

### Correction C: if Friday-to-Monday work was not entered on time, backfill the tasks first

If the kitchen forgot to enter load and balling tasks for the end of the week:

1. recover the real task timeline first
2. remove stale pending tasks that are not real anymore
3. make sure the batch and balling records match the real flow
4. only then trust the weekly closing carryover

### Correction D: Monday can still belong to the previous recovery story

In this project, the service week is still `Tuesday-Sunday`.

That means Monday is a transition day:

- it may be the day when the last Sunday load gets balled
- it may still affect the physical carryover that Tuesday inherits

So Monday should not automatically be treated as a brand-new future load for the next week.

## Recommended Human Workflow Going Forward

To avoid repeating this recovery work:

1. Close each service day with `Daily Closing`.
2. Complete `Make Dough Load` on the day the load is really mixed.
3. Complete `BallDough` on the day the dough is really balled.
4. Before `Weekly Closing`, count what is physically left:
   - ready balls
   - attention balls
   - reballed or use-first dough
   - mixed but not balled loads
5. If something was forgotten, backfill the real tasks before correcting the weekly closing.
6. Use the weekly closing note and correction note to explain the real kitchen story in plain language.

## Short Operational Summary

### Week 1 summary

`Jun 8 - Jun 14, 2026` ended with real carryover. The next week did not start from zero.

### Week 2 summary

`Jun 15 - Jun 21, 2026` originally looked too empty because end-of-week load and balling work had not all been entered. After recovery, it correctly ended with `504` ready balls and `0` mixed loads.

### Current opening snapshot

`Tue Jun 23, 2026` starts from `504` ready balls and a shortage of `439` balls against the weekly goal of `943`.
