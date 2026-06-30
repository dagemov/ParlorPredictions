# End-To-End Weekly Carryover Flow

## Related Operational History

For the reconstructed real kitchen timeline behind the two already-closed June weeks, see:

- [2026-06-23 Weekly Closing Operational History](./2026-06-23_WEEKLY_CLOSING_OPERATIONAL_HISTORY.md)

## Purpose

This document validates the real end-to-end flow for:

- Dough Planning
- Weekly Closing
- Carryover
- Dough Loads
- Dough Quality

It documents:

- the human flow
- the current technical flow
- what already worked
- what was missing before this pass
- what was corrected in this pass
- the remaining risks

## Human Flow

### 1. End of week

At the end of the closing week, the manager records what really happened:

- how much dough was needed
- how much was produced
- how much was used
- how much was lost
- how many ready balls were left
- how many attention balls were left
- how many mixed loads were left unballed
- optional notes

This is now handled in `Weekly Closing`, preferably from the `Close This Week` confirmation flow.

### 2. Start of next week

When the next planning week starts:

- `Leftover Ready Balls` carry in as available
- `Leftover Attention Balls` carry in as available but attention
- `Leftover Mixed Loads` carry in as `Mixed But Not Balled`
- mixed loads do not count as available balls yet

Current week boundary split:

- closing week: `Monday-Sunday`
- service / prep planning week: `Tuesday-Sunday`

### 3. During the week

The kitchen sees:

- what is ready now
- what is still fermenting
- what is mixed but not balled
- what was finished this week
- what belongs only to the previous week as history

### 4. New dough production

When the team creates a `Make Dough Load` task:

- the task represents future dough capacity
- completing it does not add available balls yet
- the system creates next-day `Ball Dough` work

When the team completes `Ball Dough`:

- those balls are added to available inventory
- the dough becomes part of the usable current-week supply

### 5. Dough quality overlay

At the same time:

- `Attention Dough` still counts as available
- `Must Use Next Day` is shown to kitchen-facing users as `Use First`
- `Discarded Dough` no longer counts as available

## Technical Flow

## What already worked before this pass

### Dough load vs balls

This already worked in backend and tests:

- `MakeDoughLoad` does not count as available balls
- completing `MakeDoughLoad` creates `BallDough` follow-up work
- completing `BallDough` increases available balls
- `BallDough` is what turns potential dough into real available balls

Main files:

- [PrepTaskService.cs](/C:/Users/Hombr/source/repos/ParlorPredictions/src/ParlorPrediction.Application/Services/Prep/PrepTaskService.cs)
- [PrepTask.cs](/C:/Users/Hombr/source/repos/ParlorPredictions/src/ParlorPrediction.Domain/Entities/PrepTask.cs)
- [DoughTaskWorkflowTests.cs](/C:/Users/Hombr/source/repos/ParlorPredictions/tests/ParlorPrediction.Application.Tests/DoughTaskWorkflowTests.cs)

### Weekly finished vs previous week activity

This also already worked in backend and tests:

- completed tasks before the current planning window are not counted as `Finished This Week`
- previous week activity is shown separately
- mixed dough in process reduces missing coverage without becoming available balls

Main files:

- [PrepWeeklyDoughCalendarService.cs](/C:/Users/Hombr/source/repos/ParlorPredictions/src/ParlorPrediction.Application/Services/Prep/PrepWeeklyDoughCalendarService.cs)
- [PrepWeeklyDoughCalendarServiceTests.cs](/C:/Users/Hombr/source/repos/ParlorPredictions/tests/ParlorPrediction.Application.Tests/PrepWeeklyDoughCalendarServiceTests.cs)

### Dough quality availability rules

This also already worked:

- `Attention` counts as available
- `Discarded` does not count as available
- reballed dough moves into `MustUseNextDay`

Main files:

- [DoughQualityReadService.cs](/C:/Users/Hombr/source/repos/ParlorPredictions/src/ParlorPrediction.Application/Services/Dough/DoughQualityReadService.cs)
- [DoughQualityManagementService.cs](/C:/Users/Hombr/source/repos/ParlorPredictions/src/ParlorPrediction.Application/Services/Dough/DoughQualityManagementService.cs)
- [DoughQualityServicesTests.cs](/C:/Users/Hombr/source/repos/ParlorPredictions/tests/ParlorPrediction.Application.Tests/DoughQualityServicesTests.cs)

### Weekly closing backend

The backend model and use cases already existed:

- `CreateWeeklyClosing`
- `CorrectWeeklyClosing`
- `GetWeeklyClosings`
- `GetCarryoverForWeek`

Main files:

- [WeeklyDoughClosing.cs](/C:/Users/Hombr/source/repos/ParlorPredictions/src/ParlorPrediction.Domain/Entities/WeeklyDoughClosing.cs)
- [WeeklyDoughClosingManagementService.cs](/C:/Users/Hombr/source/repos/ParlorPredictions/src/ParlorPrediction.Application/Services/Dough/WeeklyDoughClosingManagementService.cs)
- [WeeklyDoughClosingReadService.cs](/C:/Users/Hombr/source/repos/ParlorPredictions/src/ParlorPrediction.Application/Services/Dough/WeeklyDoughClosingReadService.cs)
- [WeeklyDoughClosingServicesTests.cs](/C:/Users/Hombr/source/repos/ParlorPredictions/tests/ParlorPrediction.Application.Tests/WeeklyDoughClosingServicesTests.cs)

## What was missing before this pass

Before this pass, the largest gaps were:

1. `Weekly Closing` existed in backend, but managers could not use it in MVC.
2. Dough Prep and Weekly Goal did not visibly explain weekly closing carryover.
3. The carryover values were not wired as a fallback into current week planning when a new week started without a fresh current-week inventory snapshot yet.
4. The app still had two post-login shells:
   - the main app shell
   - the dough-only shell

## What was corrected in this pass

### 1. Weekly Closing is now visible in MVC

Manager/Admin can now:

- open `Weekly Closing`
- list saved closings
- record a new weekly closing
- correct an existing closing
- review carryover preview for the selected week
- post the form successfully from MVC without losing the values during model binding

Main files:

- [WeeklyClosingController.cs](/C:/Users/Hombr/source/repos/ParlorPredictions/src/ParlorPrediction.Mvc/Controllers/WeeklyClosingController.cs)
- [Index.cshtml](/C:/Users/Hombr/source/repos/ParlorPredictions/src/ParlorPrediction.Mvc/Views/WeeklyClosing/Index.cshtml)
- [Form.cshtml](/C:/Users/Hombr/source/repos/ParlorPredictions/src/ParlorPrediction.Mvc/Views/WeeklyClosing/Form.cshtml)

### 2. Carryover now feeds planning as a safe fallback

The new rule used in planning is:

- if a current-week inventory snapshot already exists, live current-week inventory remains the source of truth
- if a current-week inventory snapshot does not exist yet, the system can fall back to the previous week closing carryover

That fallback now affects:

- daily dough calculation
- dough production planning
- weekly calendar / week goal

Main files:

- [DoughPrepCalculationService.cs](/C:/Users/Hombr/source/repos/ParlorPredictions/src/ParlorPrediction.Application/Services/Dough/DoughPrepCalculationService.cs)
- [DoughProductionPlanningService.cs](/C:/Users/Hombr/source/repos/ParlorPredictions/src/ParlorPrediction.Application/Services/Dough/DoughProductionPlanningService.cs)
- [PrepWeeklyDoughCalendarService.cs](/C:/Users/Hombr/source/repos/ParlorPredictions/src/ParlorPrediction.Application/Services/Prep/PrepWeeklyDoughCalendarService.cs)

### 3. Weekly Goal now separates carryover from current-week progress

The UI now shows carryover separately from:

- current ready dough
- current in-process dough
- current-week finished dough
- previous week used/finished

Main files:

- [WeeklyDoughCalendarResponse.cs](/C:/Users/Hombr/source/repos/ParlorPredictions/src/ParlorPrediction.Contracts/Responses/Prep/WeeklyDoughCalendarResponse.cs)
- [WeeklyGoalProgressViewModel.cs](/C:/Users/Hombr/source/repos/ParlorPredictions/src/ParlorPrediction.Mvc/Models/Prep/WeeklyGoalProgressViewModel.cs)
- [WeeklyDoughCalendarViewModel.cs](/C:/Users/Hombr/source/repos/ParlorPredictions/src/ParlorPrediction.Mvc/Models/Prep/WeeklyDoughCalendarViewModel.cs)
- [_DoughProductionPlanningPartial.cshtml](/C:/Users/Hombr/source/repos/ParlorPredictions/src/ParlorPrediction.Mvc/Views/Prep/_DoughProductionPlanningPartial.cshtml)
- [DoughWeek.cshtml](/C:/Users/Hombr/source/repos/ParlorPredictions/src/ParlorPrediction.Mvc/Views/Prep/DoughWeek.cshtml)

### 4. Post-login navigation is now one shell

The dough-only layout was removed from active use.

Now:

- `Prep`
- `PrepTasks`
- `DoughQuality`
- `WeeklyClosing`

all use the main shared layout after login.

Main file:

- [_Layout.cshtml](/C:/Users/Hombr/source/repos/ParlorPredictions/src/ParlorPrediction.Mvc/Views/Shared/_Layout.cshtml)

## Rules of sum and subtraction

### Available right now

Counts as available:

- carryover ready balls
- carryover attention balls
- current live available inventory
- completed `BallDough`
- `Attention Dough`

Does not count as available:

- leftover mixed loads
- `MakeDoughLoad` just completed
- fermenting dough not ready yet
- discarded dough

### Weekly missing

`Still Missing This Week` uses:

`Week Needed - Ready Now - Still Fermenting - Mixed But Not Balled`

Historical used/finished from the previous week is shown separately and does not reduce current week missing.

## Example: 300 balls + 1 load

### Previous week closing

- `LeftoverReadyBalls = 300`
- `LeftoverMixedLoads = 1`
- `BallsPerLoad = 168`

### Start of current week

- `CarryoverAvailableBalls = 300`
- `MixedButNotBalled = 168 potential balls`
- `AvailableBalls = 300`
- `AvailableBalls != 468`

### If week need is 1063

Before balling:

- `StillMissing = 1063 - 300 = 763`
- `MixedButNotBalled = 168 potential balls`

After `Ball Dough` completion:

- `AvailableBalls = 468`
- `StillMissing = 1063 - 468 = 595`

## Test evidence

These cases are now covered by tests:

1. Weekly closing with 300 leftover balls carries 300 available balls into next week.
2. Weekly closing with 1 mixed load carries 168 potential balls but 0 additional available balls.
3. `MakeDoughLoad` completed does not increase available balls.
4. `BallDough` completed increases available balls.
5. Previous week used/finished is shown separately.
6. Carryover is not duplicated if closing is edited or reloaded.
7. `MustUseNextDay` appears as `Use First` in kitchen-facing copy.
8. `Attention` counts as available.
9. `Discarded` does not count as available.

Files:

- [WeeklyDoughClosingServicesTests.cs](/C:/Users/Hombr/source/repos/ParlorPredictions/tests/ParlorPrediction.Application.Tests/WeeklyDoughClosingServicesTests.cs)
- [PrepWeeklyDoughCalendarServiceTests.cs](/C:/Users/Hombr/source/repos/ParlorPredictions/tests/ParlorPrediction.Application.Tests/PrepWeeklyDoughCalendarServiceTests.cs)
- [DoughTaskWorkflowTests.cs](/C:/Users/Hombr/source/repos/ParlorPredictions/tests/ParlorPrediction.Application.Tests/DoughTaskWorkflowTests.cs)
- [DoughQualityServicesTests.cs](/C:/Users/Hombr/source/repos/ParlorPredictions/tests/ParlorPrediction.Application.Tests/DoughQualityServicesTests.cs)

## Remaining risks

### 1. Mixed carryover uses fallback logic, not a dedicated batch object

The current implementation treats weekly-closing mixed loads as a planning fallback when the new week has not created a fresh current-week inventory snapshot yet.

That is intentional to avoid double counting.

It also means:

- current-week live inventory stays authoritative once fresh current-week inventory exists
- mixed carryover is not yet materialized as its own dedicated `DoughBatch` or special carryover task

### 2. Weekly closing does not auto-create balling tasks for carryover mixed loads yet

The system can already show mixed carryover separately and the normal `BallDough` workflow already works.

But there is not yet a dedicated automatic workflow that turns `LeftoverMixedLoads` from weekly closing directly into pending carryover ball tasks.

### 3. Previous week used/finished may come from closing when present, otherwise from completed task history

This is intentional:

- weekly closing is more semantically correct when it exists
- task history remains the fallback when no weekly closing exists
