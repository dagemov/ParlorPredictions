# Dough Load Vs Dough Balls Rules

## Goal

Separate two real kitchen moments that the current prep flow tends to blur together:

1. `MakeDoughLoad`
2. `BallDough`

This document defines the business rule, the minimal migration path, and the expected UX language for ParlorPrediction.

## Why This Change Exists

In the real restaurant, a full dough load is not the same thing as ready dough balls.

When the kitchen completes a dough load:

- dough has been mixed
- the load exists operationally
- the team may need to ball it the next day
- it does **not** mean `168 ready balls` should count as available now

Only after the team completes the balling step should those balls count as available dough inventory.

## Core Rule

### MakeDoughLoad

What it means:

- the kitchen prepared a dough load or dough batch today
- this creates future work
- this does **not** create available dough balls yet

Operational meaning:

- `1 full load = 14 cases = 168 potential balls`
- the load becomes pending balling work for the next day

Availability rule:

- completing `MakeDoughLoad` must **not** increase available balls
- completing `MakeDoughLoad` must **not** reduce the same-day dough shortage by pretending the balls already exist

Expected follow-up:

- the system creates or suggests one or more `BallDough` tasks for the next day

### BallDough

What it means:

- the kitchen converts prepared dough into dough balls
- this is the moment when dough becomes available to count

Availability rule:

- completing `BallDough` **does** increase available dough balls
- the increase should use the actual quantity balled, not the potential load number

Expected follow-up:

- if the balling came from a known load, the batch should be marked as balled
- if dough quality tracking is active, the new balled dough should be eligible for Dough Quality review

## Terms

### Potential Balls

The number of balls a load could produce.

- `PotentialBalls = LoadCount * 168`

This number is helpful for planning and display, but it is **not** the same as available balls.

### Available Balls

Balls that are actually ready to count in inventory.

These come from:

- existing inventory snapshots
- completed legacy/generic dough tasks that already count as balls
- completed `BallDough` tasks

These do **not** come from:

- completed `MakeDoughLoad` tasks

### Pending Balling Work

Loads that were made but not yet balled.

This work should be visible as:

- `Needs attention now`
- `Ball 1 dough load from yesterday`
- `168 balls ready to be balled`

## Task Types

## 1. GenericDough

Compatibility type for current tasks.

Use when:

- the task already exists in the system
- the task was created before this change
- the task is still meant to behave like the old model

Behavior:

- still counts toward completed dough balls the same way current logic expects
- preserved to avoid breaking existing flows immediately

## 2. MakeDoughLoad

Use when:

- the task represents mixing one or more full loads

Recommended stored unit:

- `FullLoads`

Behavior:

- quantity is stored as load count
- balls equivalent is planning-only
- completion creates unballed dough batch records
- completion creates follow-up `BallDough` work for the next day
- completion does not increase available inventory

## 3. BallDough

Use when:

- the task represents converting a prepared load into dough balls

Recommended stored unit:

- `Balls`

Behavior:

- quantity is stored as actual balls balled
- completion increases available inventory
- completion should mark the source batch as balled when a source batch exists

## Minimal Migration Strategy

Keep the current dough task system alive while adding a small amount of new semantics.

### Preserve

- existing prep tasks
- existing recommendation flow
- existing dashboard flow
- existing dough recommendation data

### Add

- task type on `PrepTask`
- task quantity unit on `PrepTask`
- optional source load or source batch references for follow-up balling work

### Do Not Do In This Phase

- do not delete legacy dough tasks
- do not rewrite dashboard math blindly
- do not claim AI or forecasting behavior beyond what exists

## Calculation Rules

## Dough Prep Calculation

When computing:

- `CompletedBalls`
- `MissingBalls`
- `Need To Make`

the system should:

- count completed `GenericDough` work that represents balls
- count completed `BallDough`
- ignore completed `MakeDoughLoad` for available-ball math

Reason:

- a mixed load is not ready inventory yet

## Production Planning

Production planning should continue using:

- ready balls
- fermenting balls
- unballed balls

This change works well with that model because `MakeDoughLoad` can create real unballed batch records that later feed `BallDough`.

## UX Rules

The kitchen must see the distinction clearly.

### For MakeDoughLoad

Visible title:

- `Make 1 full dough load`

Visible description:

- `We need this dough load today so it can be balled tomorrow.`

Visible helper:

- `This does not count as available balls yet.`

Visible secondary detail:

- `Potential: 168 balls tomorrow`

### For BallDough

Visible title:

- `Ball 168 dough balls`

Visible description:

- `This dough load is ready to be balled today.`

Visible helper:

- `These balls count as available only after this task is completed.`

Visible attention state:

- `Needs attention now`

## Inventory Update Rules

### After MakeDoughLoad Completion

Expected system behavior:

- mark the load task complete
- create one or more unballed dough batch records
- create one or more next-day `BallDough` tasks
- do not update available dough balls

### After BallDough Completion

Expected system behavior:

- mark the ball task complete
- mark the source dough batch as balled when linked
- increase available dough balls by the actual balls created
- make the new dough visible to Dough Quality flow when possible

## Compatibility Rules

Existing dough tasks must remain valid.

### Legacy Behavior

If an older task has no explicit task type:

- treat it as `GenericDough`
- keep the current completed-balls behavior

### Forward Behavior

New workflow should prefer:

- `MakeDoughLoad` for mixing loads
- `BallDough` for making ready dough balls

## Risks To Watch

- a manager may assume `1 load done` means `168 balls available`; the UI must explicitly say otherwise
- partial balling can create a real-world difference between expected balls and actual balls
- inventory snapshots can drift if balling updates are not based on the latest tracked snapshot
- old tasks and new task types may coexist for a while, so views must label them clearly

## Acceptance Checks

The change is working if all of these are true:

1. Completing `MakeDoughLoad` does not increase available balls.
2. `1 full load` is displayed as `168 potential balls`, not `168 available balls`.
3. Completing `MakeDoughLoad` creates or suggests `BallDough` for the next day.
4. Completing `BallDough` increases available balls.
5. Pending `BallDough` work appears as attention or action-needed work.
6. Legacy dough tasks still work unless intentionally migrated.
