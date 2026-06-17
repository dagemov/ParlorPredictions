# Dough Source Usage And Reball Requirements

## Source Tracking

- Every usage trace is tied to one dough source record.
- Source cards must show:
  - original balls
  - already used balls
  - remaining balls
  - recommended action
- Remaining-by-source visuals should make it obvious which dough is still available and which sources are nearly depleted.

## Reball Planning

- Reball planning must read the remaining balls by source.
- Reball planning must not use the gross historical quantity of the source.
- If a source started with 48 balls and traced usage already consumed 24, reball decisions use the remaining 24 only.

## Priority Rules

- `MustUseNextDay` should appear first for restaurant usage.
- `Reballed` is still usable for restaurant flow and should remain visible.
- `Attention` and older dough should stay visible for review decisions.
- Dough past the discard threshold must be shown as discard-only guidance and not as a usage source.

## Operational Separation

- `Service days` remain `Tuesday-Sunday`.
- `Weekly Closing` remains `Monday-Sunday`.
- Reball and dough source tracking follow the service workflow, not the weekly closing window.

## UX Requirements

- Keep one dough navigation flow.
- Do not introduce a second menu just for dough trace usage.
- Dough usage, dough prep, daily closing, weekly closing, and reball planning should remain reachable from the same prep experience.

## Guardrails

- Do not relax `Weekly Closing` constraints to accommodate dough trace usage.
- Do not let source usage tracking redefine daily-closing week rules.
- Prefer warnings when totals disagree instead of auto-correcting numbers without user review.
