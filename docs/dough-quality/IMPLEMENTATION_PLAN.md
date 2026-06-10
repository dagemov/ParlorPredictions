# Dough Quality Implementation Plan

## Phase order

1. Add backend rule documents
2. Add domain enums, rules, and entities
3. Add application services and repository interfaces
4. Add EF Core configurations and repositories
5. Add migration
6. Add tests
7. Run build and tests

## Application use cases

### Management

- `CreateDoughQualityRecord`
- `MarkDoughAsAttention`
- `CorrectDoughQualityStatus`
- `DiscardDough`
- `ReballDough`

### Read / evaluation

- `SearchDoughQualityRecords`
- `EvaluateDoughAttentionCandidates`
- `GetDoughQualitySummary`
- `GetDoughLossAnalytics`

## Persistence additions

New tables:

- `DoughBatchQualityRecords`
- `DoughLossRecords`
- `DoughReballRecords`

Key indexes:

- quality status
- source date
- created or balled at
- reballed at
- must use by date
- loss date
- loss reason

## Test targets

- attention counts as available
- discarded does not count as available
- reball creates partial recovery
- reball creates loss record when recovered quantity is lower than original quantity
- reball sets `MustUseByDate = ReballDate + 1 day`
- discard requires reason
- admin can correct status

## Important non-goals

- do not change UI in this phase
- do not replace current dough calculation
- do not auto-mark attention in a background job yet
- do not add AI training logic yet
