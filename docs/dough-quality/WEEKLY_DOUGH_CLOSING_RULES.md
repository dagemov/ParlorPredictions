# Weekly Dough Closing / Dough Recorrido

## Purpose

`Weekly Dough Closing` captures the real leftover dough at the end of an operational week so the next week starts from what is actually still in the kitchen, not from an artificial reset to zero.

This rule is intentionally implemented as a parallel backend workflow. It does **not** replace:

- `DoughPrepCalculationService`
- `PrepWeeklyDoughCalendarService`
- Dough Quality tracking
- prep task workflows

The goal of this phase is to preserve carryover truth, create auditability, and expose future-safe application use cases without changing the current weekly UI yet.

## Operational Week

- Operational week start: `Tuesday`
- Operational week end: `Sunday`
- Week length: `6 days`

All closings are normalized to the Tuesday-based operational week already used by weekly dough planning.

## What Weekly Closing Stores

Each closing records:

- `Week Start`
- `Week End`
- `Needed`
- `Produced`
- `Used`
- `Lost`
- `Leftover Ready Balls`
- `Leftover Attention Balls`
- `Leftover Mixed Loads`
- `Notes`
- `ClosedBy`
- `ClosedAt`
- optional correction trace: `CorrectedBy`, `CorrectedAt`, `CorrectionNote`

## Carryover Rules

### 1. Ready balls carry forward as available

`Leftover Ready Balls` from the previous closed week become `carryover available` for the next week.

### 2. Attention balls carry forward as available but attention

`Leftover Attention Balls` also carry into the next week and still count as available.

They must stay visually separate in future UX because they require review, but they are still usable inventory until discarded.

### 3. Mixed loads carry forward as pending balling

`Leftover Mixed Loads` from the previous week carry into the next week as:

- `Mixed But Not Balled`
- `pending balling`

They do **not** count as available balls.

### 4. Balling is the moment mixed dough becomes available

A mixed load is not treated as available inventory until it is actually balled.

Operational consequence:

- `MakeDoughLoad` does not increase available balls.
- `BallDough` completed can increase available balls.

### 5. Historical used/finished stays separate

`Used` or other historical week-end production numbers remain historical reference only.

They can be displayed for manager context as:

- `Previous Week Used / Finished`

But they must **not**:

- reduce next-week missing dough by themselves
- appear as current-week progress
- be treated as available carryover

### 6. No duplicate carryover

Only one closing is allowed per operational week.

If a closing already exists:

- the system rejects a second create for the same week
- the existing closing must be corrected through `CorrectWeeklyClosing`

This prevents double-counting carryover into the next week.

## Traceability Rules

- Only `Admin` or `Manager` can create a weekly closing.
- Only `Admin` or `Manager` can correct a weekly closing.
- Original `ClosedBy` and `ClosedAt` are preserved.
- Corrections store `CorrectedBy`, `CorrectedAt`, and optional `CorrectionNote`.

## Proposed Application Use Cases

### GetWeeklyClosings

Returns the weekly closing history for review, reporting, or future admin screens.

### CreateWeeklyClosing

Creates the end-of-week snapshot used to derive next-week carryover.

### CorrectWeeklyClosing

Updates the already-closed week without creating a duplicate carryover source.

### GetCarryoverForWeek

Returns the carryover inputs for a target week:

- carryover ready balls
- carryover attention balls
- carryover available balls
- mixed but not balled loads
- previous week produced/used/lost for separate reference

## Current Scope Boundary

This phase adds:

- domain model
- persistence
- application services
- tests
- migration

This phase does **not** yet:

- inject weekly closing carryover into the existing weekly UI
- replace current inventory snapshot math
- create MVC screens/controllers for Dough Recorrido

That wiring can happen in a later phase once the human flow is approved.
