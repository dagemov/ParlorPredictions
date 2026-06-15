# Dough Trace Usage Implementation

## Purpose

This phase adds source-aware dough usage tracking and a first reball-planning read model.

The goal is to answer:

- what dough was used
- where it came from
- what destination consumed it
- what older dough still remains
- what should be used first, reviewed, reballed, or discarded next

## Scope In This Phase

This implementation adds:

- `DoughUsageTrace` persistence
- tray-to-ball conversion using `12 balls per tray`
- source-aware usage validation against remaining dough
- available source suggestions by destination and season
- remaining-by-source projection
- reball planning projection based on remaining old dough
- MVC screens for usage entry, source review, and reball planning

## Existing Boundaries To Preserve

- `Weekly Closing` remains `Monday-Sunday`
- `Daily Closing` remains `Tuesday-Sunday`
- usage traces do not replace daily closings
- usage traces feed availability and planning, but full daily-closing-to-trace reconciliation stays partial in this phase

## Source Of Truth

`DoughBatchQualityRecord` remains the source record for dough quality state.

`DoughUsageTrace` records consumption against that source.

The new read model calculates remaining dough by subtracting traced usage from the selected source record.

## New Core Rules

1. `BallsUsed = TrayCount * 12`
2. Usage cannot exceed the remaining balls for the selected source
3. Discarded dough cannot be selected as a usage source
4. `MustUseNextDay` is suggested first for restaurant usage
5. `Event` and `FarmersMarket` in `June-August` warn when the selected dough is old, attention, reballed, or must-use
6. Reball planning uses remaining balls after usage traces, not total historical dough

## Planning Heuristics In This Phase

The first read model uses a simple planning split:

- `UseFirst`
  - dough currently in `MustUseNextDay`
- `Review`
  - remaining dough in the attention window
- `Reball`
  - remaining dough older than the preferred attention window
- `Discard`
  - overdue `MustUseNextDay` dough or remaining dough well past the preferred window

These heuristics are intentionally conservative and can be refined later with better kitchen feedback.

## Known Gap In This Phase

Full reconciliation between:

- source-level usage traces
- the advanced dough quality review flow
- historical reball/discard mutation paths

is not fully closed yet.

This phase focuses on:

- correct source-aware usage entry
- correct remaining-by-source projections
- better carryover and reball planning visibility

Future refinement can make every dough-quality mutation path consume the exact same remaining-source projection.
