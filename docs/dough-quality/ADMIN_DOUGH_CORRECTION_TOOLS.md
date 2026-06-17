# Admin Dough Correction Tools

## Purpose

The `Admin / Dough Corrections` workspace gives operations a controlled way to fix live dough data without editing SQL directly.

Route:

- `/admin/dough-corrections`

Primary goals:

- stabilize Dough Prep when live data is physically wrong
- expose the current inventory snapshot in one place
- let Admin correct prep tasks and dough batches directly
- point Admin to the existing weekly closing, daily closing, dough usage, and dough quality correction flows

## Access rules

- `Admin` can edit from the correction workspace.
- `Manager` can view the dashboard snapshot, but the new correction actions in this workspace are view-only.
- Existing module-specific correction pages keep their own authorization rules.

## Snapshot cards

The dashboard intentionally shows the same live planning numbers that affect Dough Prep:

- `Ready Now`
- `Future Dough`
- `Produced This Week`
- `TrayCount Storage`
- weekly goal and still-missing values
- mixed but not balled and carryover context
- current-week daily closing rows
- recent prep tasks, dough batches, usage traces, quality records, and weekly closings

This page is meant to answer:

- what the UI is currently using
- whether a bad number comes from carryover, a batch, a closing, a trace, or quality state
- whether the local database still has the pending fractional `TrayCount` migration

## Immediate stability fix

`DoughUsageTrace.TrayCount` is modeled as `decimal` in C# and EF.

If the local SQL database still stores `DoughUsageTraces.TrayCount` as `int`, reads can fail with:

- `InvalidCastException: Unable to cast object of type 'System.Int32' to type 'System.Decimal'`

The repository now reads `TrayCount` through an explicit SQL cast so the app can still load traces while the database migration is pending.

Important:

- this is only a compatibility bridge
- the database still needs the fractional tray migration applied in each environment
- do not run `dotnet ef database update` without explicit approval

## Correction flows

### Prep tasks

Admin can:

- change task date
- change task type
- change quantity unit
- change recommended quantity
- move task back to pending, in progress, completed, or cancelled
- adjust completed quantity and completed timestamp
- relink source task or source batch

Use this when:

- `MakeDoughLoad` was recorded as `BallDough`
- a completed task quantity does not match reality

### Dough batches

Admin can:

- change batch date
- change total cases
- mark batch as balled or unballed
- flag event-exception batches
- void orphan batches instead of deleting them

Use this when:

- a mixed batch still appears in planning but no longer exists physically
- a batch was balled on the wrong date

Voided batches stay in history but are excluded from the live batch projection.

### Existing module links

The admin workspace links to the current correction surfaces for:

- weekly closing
- daily closing
- dough usage traces
- dough quality and reball review

That avoids creating duplicate business logic while still giving operations one entry point.

## Operational warning

Manual corrections affect planning calculations.

Admin should only change data after confirming the physical state of:

- ready balls
- mixed but not balled dough
- daily used dough
- reballed inventory
- discarded dough
