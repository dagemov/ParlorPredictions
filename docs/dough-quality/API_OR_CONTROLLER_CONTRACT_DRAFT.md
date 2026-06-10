# Dough Quality API Or Controller Contract Draft

## Purpose

This document proposes future MVC-facing contracts for the Dough Quality layer.

It is intentionally a draft for controller and page-model planning.
It does not require changes to:

- domain logic
- application services
- persistence
- migrations

## Design Goal

Keep the controller surface aligned with:

- existing MVC route style under `/prep`
- current application service names
- future card-first UX needs

## Proposed Controller Area

Recommended future route base:

- `GET /prep/dough-quality/...`
- `POST /prep/dough-quality/...`

Reason:

- it matches the current `PrepController` route family
- it keeps Dough Quality close to Dough Prep operations
- it avoids mixing urgent kitchen actions into the dashboard area

## Authentication And Request Style

### Read endpoints

- available to `Admin`, `Manager`, and `PizzaMaker` when the screen needs it
- query-string driven

### Write endpoints

- MVC `POST`
- anti-forgery token required
- role checks must stay server-side

## Important Boundary

Some of the UX summary needs are page-composition needs, not direct application-service DTOs.

For example:

- `Need Today`
- `Need To Make`
- human recommendation text

These already come from existing Dough Prep flows, not from `DoughQualityReadService`.

So the future controller contract may return page-specific summary data that composes:

- existing Dough Prep calculations
- existing recommendation data
- Dough Quality summary data

This is a controller contract draft, not a proposal to replace current application DTOs.

## 1. GET Dough Quality Summary

### Proposed route

`GET /prep/dough-quality/summary?targetDate=2026-06-08`

### Primary purpose

Support Dough Prep Home with card-first summary data.

### Proposed query parameters

- `targetDate` optional, defaults to today

### Proposed response shape

```json
{
  "targetDate": "2026-06-08",
  "needTodayBalls": 420,
  "goodDoughBalls": 280,
  "attentionDoughBalls": 84,
  "mustUseNextDayBalls": 56,
  "totalAvailableDoughBalls": 420,
  "needToMakeBalls": 112,
  "humanRecommendationText": "Use must-use dough first and mix one additional load today.",
  "primaryAction": "ReviewOldDough",
  "primaryActionLabel": "Review Older Dough",
  "hasMustUseNextDayAlert": true
}
```

### Composition note

The controller can compose this from:

- existing Dough Prep page services
- `IDoughQualityReadService.GetSummaryAsync`

Current backend note:

- `GetSummaryAsync` currently returns overall Dough Quality totals
- a target-date page summary is therefore a controller composition concern in the first MVC pass

### Access

- `Admin`
- `Manager`
- `PizzaMaker` only if the team wants the same summary view for kitchen staff

## 2. GET Attention Candidates

### Proposed route

`GET /prep/dough-quality/attention-candidates?referenceDate=2026-06-08&createdOrBalledFromDate=2026-06-03&createdOrBalledToDate=2026-06-08&reballedFromDate=&reballedToDate=&currentStatus=Good`

### Primary purpose

Support Dough Quality Review.

### Proposed query parameters

- `referenceDate` required
- `createdOrBalledFromDate` optional
- `createdOrBalledToDate` optional
- `reballedFromDate` optional
- `reballedToDate` optional
- `currentStatus` optional

### Proposed response shape

```json
{
  "referenceDate": "2026-06-08",
  "filters": {
    "createdOrBalledFromDate": "2026-06-03",
    "createdOrBalledToDate": "2026-06-08",
    "reballedFromDate": null,
    "reballedToDate": null,
    "currentStatus": "Good"
  },
  "items": [
    {
      "doughBatchQualityRecordId": "00000000-0000-0000-0000-000000000000",
      "sourceDate": "2026-06-05",
      "createdOrBalledAt": "2026-06-05T09:15:00Z",
      "quantityBalls": 56,
      "currentStatus": "Good",
      "ageDays": 3,
      "candidateReason": "Dough has reached the attention age window."
    }
  ]
}
```

### Composition note

Current backend contracts expose:

- `EvaluateDoughAttentionCandidatesRequest`
- `SearchDoughBatchQualityRecordsRequest`

The future controller can compose these without changing backend rules:

1. evaluate attention candidates by reference date
2. narrow or decorate the result using date filters and record search

### Access

- `Admin`
- `Manager`

## 3. POST Mark Attention

### Proposed route

`POST /prep/dough-quality/mark-attention`

### Primary purpose

Mark a batch as `Attention` while keeping it available.

### Proposed request body

```json
{
  "doughBatchQualityRecordId": "00000000-0000-0000-0000-000000000000",
  "statusReason": "Reached 3-day review window.",
  "attentionMarkedAtUtc": "2026-06-08T13:00:00Z",
  "managerNote": "Check smell and texture before service.",
  "updatedByUserId": "manager-user-id"
}
```

### Proposed response shape

Use the existing `DoughBatchQualityRecordResponse`.

### Access

- `Admin`
- `Manager`

### Validation notes

- batch id required
- status reason required
- acting user required

## 4. POST Correct Status

### Proposed route

`POST /prep/dough-quality/correct-status`

### Primary purpose

Allow Admin to fix an incorrect quality state.

### Proposed request body

```json
{
  "doughBatchQualityRecordId": "00000000-0000-0000-0000-000000000000",
  "newStatus": "Good",
  "statusReason": "Previous status was entered in error.",
  "effectiveAtUtc": "2026-06-08T13:05:00Z",
  "mustUseByDate": null,
  "discardReason": null,
  "managerNote": "Corrected after batch review.",
  "updatedByUserId": "admin-user-id"
}
```

### Proposed response shape

Use the existing `DoughBatchQualityRecordResponse`.

### Access

- `Admin` only

### Validation notes

- new status required
- discard reason required if corrected to `Discarded`
- controller should present a clear warning when this changes availability

## 5. POST Reball Dough

### Proposed route

`POST /prep/dough-quality/reball`

### Primary purpose

Record a reball event and move the recovered dough to `MustUseNextDay`.

### Proposed request body

```json
{
  "doughBatchQualityRecordId": "00000000-0000-0000-0000-000000000000",
  "quantityRecoveredBalls": 42,
  "reballDateUtc": "2026-06-08T14:10:00Z",
  "result": "PartialRecovered",
  "discardReason": "ManagerDecision",
  "managerNote": "Recovered most of the batch. Use first tomorrow.",
  "updatedByUserId": "pizza-maker-user-id"
}
```

### Proposed response shape

Use the existing `DoughBatchQualityRecordResponse`.

### Access

- `PizzaMaker` for `PartialRecovered`
- `Manager` and `Admin` for full authority, including discard outcome

### Validation notes

- record id required
- recovered quantity required
- recovered quantity must be less than original quantity in the kitchen flow
- discard reason required if the result is `Discarded`

### UX note

The screen should not ask the user to calculate the loss.
The system should derive:

- original quantity
- recovered quantity
- lost quantity

## 6. POST Discard Dough

### Proposed route

`POST /prep/dough-quality/discard`

### Primary purpose

Record a discard event with a mandatory reason and optional note.

### Proposed request body

```json
{
  "doughBatchQualityRecordId": "00000000-0000-0000-0000-000000000000",
  "discardReason": "OverFermented",
  "discardedAtUtc": "2026-06-08T15:00:00Z",
  "managerNote": "Batch left too long after event return.",
  "updatedByUserId": "manager-user-id"
}
```

### Proposed response shape

Use the existing `DoughBatchQualityRecordResponse`.

### Current backend constraint

The existing `DiscardDoughRequest` discards the full tracked record quantity.

That means the first MVC version should do one of these two things explicitly:

1. support full-batch discard only
2. add a future backend enhancement before promising partial discard quantity in the UI

### Access

- `Manager`
- `Admin`

### Allowed reason values

- `TooHot`
- `OverFermented`
- `StoredTooManyDays`
- `Contamination`
- `FifoNotFollowed`
- `NotSoldEnough`
- `OverProduced`
- `ManagerDecision`
- `Other`

### Validation notes

- reason required
- only authorized roles can submit
- controller should display the consequence that discarded dough stops counting as available

## 7. GET Loss Analytics

### Proposed route

`GET /prep/dough-quality/loss-analytics?fromDate=2026-06-01&toDate=2026-06-08&lossReason=OverProduced`

### Primary purpose

Support the Loss Analytics Preview screen and future dashboard expansion.

### Proposed query parameters

- `fromDate` optional
- `toDate` optional
- `lossReason` optional

### Proposed response shape

Start with the existing `DoughLossAnalyticsResponse` and optionally decorate it with lightweight summaries for the MVC screen.

Example:

```json
{
  "totalLostBalls": 84,
  "items": [
    {
      "lossDate": "2026-06-06",
      "lossReason": "OverProduced",
      "quantityLostBalls": 28
    },
    {
      "lossDate": "2026-06-07",
      "lossReason": "TooHot",
      "quantityLostBalls": 56
    }
  ]
}
```

### Access

- `Manager`
- `Admin`

## Recommended Controller Split

### Option A: New `DoughQualityController`

Recommended first choice.

Why:

- keeps Dough Quality routes focused
- avoids overgrowing `PrepController`
- supports incremental UI work later

### Option B: Extend `PrepController`

Possible, but only if the team wants fewer controllers and the action count stays manageable.

## Error Handling Expectations

All future MVC endpoints should translate service errors into calm user-facing outcomes.

Examples:

- missing record -> `404`
- invalid request -> validation message and `400`
- unauthorized role -> `403`
- concurrency or stale page issue -> reload prompt

## Important Non-Goals

This contract draft does not propose:

- frontend implementation
- background jobs
- AI predictions
- replacement of current Dough Prep calculations
