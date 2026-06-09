# Weekly Carryover And Finished Rules

## Goal

Make the weekly dough view readable for managers by separating:

1. Dough still available for the current week.
2. Dough already in process for the current week.
3. Dough still missing for the current week.
4. Dough activity that happened before this week and is only historical reference.

## Current Week Window

- The operational week is Tuesday through Sunday.
- `Finished This Week` only counts dough work completed inside the current operational week window.
- A completed task must also belong to the current week window to count in `Finished This Week`.

## What Counts As Weekly Coverage

`Dough Still Missing This Week` is calculated from:

- `Ready Now`
- `Still Fermenting`
- `Mixed But Not Balled`

This keeps weekly coverage tied to dough that is still usable or still moving toward usability in the current week.

## What Does Not Reduce Weekly Missing

These values are displayed for visibility, but are not treated as extra coverage on top of current stock:

- `Finished This Week`
- `Previous Week Used / Finished`

Reason:

- `Ready Now` already represents dough that is still available.
- If a `BallDough` task was completed and the dough is still on hand, it is already reflected in `Ready Now`.
- Historical completion activity should not be double-counted as new coverage.

## Metric Semantics

### Dough Needed This Week

- Total dough balls required from Tuesday through Sunday.
- Includes normal baseline dough plus event dough.

### Ready Now

- Dough balls still available right now for the current week.
- Comes from the latest available inventory snapshot.
- Real carryover can count here if it is still available.

### Still Fermenting

- Dough batches already mixed, not ready yet, but expected to become ready within the current week.
- Does not count as available yet.

### Mixed But Not Balled

- Dough loads already mixed and ready within the current week, but not yet balled.
- Does not count as available balls yet.
- `MakeDoughLoad` contributes here, not to `Ready Now`.

### Finished This Week

- Countable dough tasks completed inside the current week window.
- This is an activity/progress reference.
- If that dough is still on hand, it is already reflected in `Ready Now`.

### Dough Still Missing This Week

- Remaining weekly shortage after `Ready Now`, `Still Fermenting`, and `Mixed But Not Balled` are counted.
- Does not subtract previous-week activity.

### Previous Week Used / Finished

- Dough task activity completed before the current week window.
- Display only.
- Must show clear text:
  `This number is not counted as available for this week.`

## Task Type Rules

### MakeDoughLoad

- Does not increase available balls.
- Can increase `Still Fermenting` or `Mixed But Not Balled`, depending on readiness.

### BallDough

- Increases available balls when completed.
- Can affect `Ready Now`.

## Validation Scenarios

1. If the work date is June 9, 2026 and the planning week is June 9 through June 14, 2026, completed tasks before June 9 do not count as `Finished This Week`.
2. `Previous Week Used / Finished` is displayed separately and does not reduce `Dough Still Missing This Week`.
3. Carryover still available, such as 24 dough balls, can count in `Ready Now`.
4. Carryover already used does not count in `Ready Now`.
5. `MakeDoughLoad` does not count as available balls.
6. `BallDough` completed does count as available balls.
