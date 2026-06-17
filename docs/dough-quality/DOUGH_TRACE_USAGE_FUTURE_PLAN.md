# Dough Trace Usage Future Plan

## Purpose

Core `Dough Trace Usage` is now implemented in this phase.

This document tracks the next refinement steps that still remain after the first source-aware release.

## What Exists Now

The current implementation adds:

- usage traces linked to a dough quality source record
- tray-to-ball conversion using `12 balls per tray`
- remaining-by-source projection
- destination-aware source suggestions
- first-pass reball planning based on remaining old dough

That means the app now knows:

- which dough source was used
- how many trays and balls were used
- whether the dough came from good, attention, reballed, or must-use stock
- whether it was used for `Restaurant`, `Event`, or `FarmersMarket`

## What Still Needs Refinement

The first release does not fully unify every dough mutation path yet.

The biggest remaining gap is deeper reconciliation between:

- `Daily Closing` totals
- source-level `DoughUsageTrace` entries
- advanced dough quality mutation screens
- historical reball and discard paths

This phase improves planning safety, but some advanced flows still rely on legacy record mutations that should eventually share one projection model.

## Future Reconciliation Goal

The long-term target is:

- every usage, reball, discard, and correction action should resolve against the same remaining-by-source rules
- `Daily Closing` and usage traces should surface reconciliation warnings when totals drift
- weekly carryover should be explainable from the exact surviving sources, not only from aggregate math

## Future UX Improvements

The current screens focus on quick entry and simple planning.

Future upgrades can add:

- stronger mismatch warnings between `Daily Closing` and total traced usage
- faster batch/source pickers for busy kitchen workflows
- richer edit history for managers
- manager approval flow when PizzaMaker flags dough for review
- better timeline views showing usage, reball, and discard history per source

## Future Recommendation Improvements

The current planning heuristics are intentionally conservative.

Later improvements can add:

- smarter source ranking based on exact dough age bands
- better seasonality rules beyond `June-August`
- destination-specific recommendations that learn from real kitchen outcomes
- clearer discard thresholds using historical quality and loss patterns

## Scope Boundary After This Phase

Implemented now:

- trace usage persistence
- trace usage UI
- source batch/date picker
- tray/case logging flow
- destination-aware warnings and suggestions
- first reball planning projection

Still future:

- full daily-closing reconciliation workflow
- one shared mutation engine across all dough-quality actions
- AI-assisted prioritization and waste reduction recommendations
