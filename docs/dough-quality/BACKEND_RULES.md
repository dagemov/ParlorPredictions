# Dough Quality Backend Rules

## Scope

This phase adds a dough quality layer to the backend without replacing the current dough calculation flow.

The current services must keep working:

- `DoughPrepCalculationService`
- `PrepWeeklyDoughCalendarService`

This phase does not include:

- UI redesign
- frontend forms
- Stitch prototype work
- AI training changes

## Quality statuses

`DoughQualityStatus`

- `Good`
- `Attention`
- `Reballed`
- `MustUseNextDay`
- `Discarded`

## Availability rules

The following statuses count as available dough:

- `Good`
- `Attention`
- `Reballed`
- `MustUseNextDay`

The following status does not count as available dough:

- `Discarded`

Important rule:

- `Attention` dough is still available until a manager or admin explicitly discards it.

## Attention rules

- Dough normally becomes a candidate for `Attention` after 3 to 4 operational days.
- This phase does not add an automatic job.
- This phase adds an evaluation use case:
  - `EvaluateDoughAttentionCandidates`
- `Attention` must record when it was marked and why.

## Reball rules

- Reball never recovers 100 percent of the dough.
- Reball is always treated as partial recovery in the default business path.
- Reball must capture:
  - quantity before reball
  - quantity recovered
  - quantity lost
- `QuantityLostBalls = QuantityBeforeReball - QuantityRecoveredBalls`
- After successful partial reball, the dough must move to `MustUseNextDay`.
- `MustUseByDate = ReballDate + 1 day`

## MustUseNextDay rules

- `MustUseNextDay` still counts as available dough.
- If the dough is not used by the next day, it becomes an attention candidate for manager review.
- The final business decision after that point belongs to the manager/admin.

## Discard rules

- Only `Manager` or `Admin` can discard dough.
- Discard requires a mandatory reason.
- Discard creates a `DoughLossRecord`.
- Once discarded, the dough no longer counts as available.

## Loss rules

`DoughLossReason`

- `TooHot`
- `OverFermented`
- `StoredTooManyDays`
- `Contamination`
- `FifoNotFollowed`
- `NotSoldEnough`
- `OverProduced`
- `ManagerDecision`
- `Other`

Losses must be stored for future analysis, even though this phase does not connect them to AI training.

## Role rules

- `DiscardDough`: `Manager` or `Admin`
- `CorrectDoughQualityStatus`: `Admin`
- `ReballDough`: active authorized kitchen user; in this implementation, `Admin`, `Manager`, and `PizzaMaker` are allowed for partial reball

## Filtering rules

Administrative review must allow filtering dough quality records by:

- creation / balled date
- reballed date

This supports manual correction without changing the current dough calculation services.
