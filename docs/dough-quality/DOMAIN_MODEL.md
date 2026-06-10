# Dough Quality Domain Model

## Main aggregate

### `DoughBatchQualityRecord`

Represents an operational dough quantity being tracked for quality and availability decisions.

Core fields:

- `Id`
- `SourceDate`
- `OriginalDoughTaskId` nullable
- `CreatedOrBalledAt`
- `QuantityBalls`
- `CurrentStatus`
- `StatusReason`
- `AttentionMarkedAt`
- `ReballedAt`
- `MustUseByDate`
- `DiscardedAt`
- `DiscardReason`
- `ManagerNote`
- `CreatedByUserId`
- `UpdatedByUserId`
- `CreatedAtUtc`
- `UpdatedAtUtc`

Behavior:

- create a new tracked dough record
- mark as attention
- correct status
- reball partially
- discard
- report whether the current status counts as available

## Supporting records

### `DoughLossRecord`

Represents measured dough loss for analytics and future forecasting inputs.

Fields:

- `Id`
- `DoughBatchQualityRecordId`
- `QuantityLostBalls`
- `LossReason`
- `LossDate`
- `ManagerNote`
- `CreatedByUserId`
- `CreatedAtUtc`

### `DoughReballRecord`

Represents the operational outcome of a reball event.

Fields:

- `Id`
- `DoughBatchQualityRecordId`
- `QuantityBeforeReball`
- `QuantityRecoveredBalls`
- `QuantityLostBalls`
- `ReballDate`
- `Result`
- `MustUseByDate`
- `ManagerNote`
- `CreatedByUserId`
- `CreatedAtUtc`

## Enums

### `DoughQualityStatus`

- `Good`
- `Attention`
- `Reballed`
- `MustUseNextDay`
- `Discarded`

### `DoughLossReason`

- `TooHot`
- `OverFermented`
- `StoredTooManyDays`
- `Contamination`
- `FifoNotFollowed`
- `NotSoldEnough`
- `OverProduced`
- `ManagerDecision`
- `Other`

### `ReballResult`

- `PartialRecovered`
- `Discarded`
- `ManagerCancelled`

## Domain rules

- `Attention` counts as available.
- `Discarded` does not count as available.
- Partial reball reduces the tracked dough quantity to the recovered amount.
- Reball loss is stored separately in `DoughLossRecord`.
- Successful partial reball sets `MustUseByDate` to the next day.
- Discard stores both the discarded status and a loss record.

## Deliberate boundary

This model does not replace:

- dough demand planning
- current dough calculation
- current weekly calendar logic

It is a parallel operational layer that can later feed better planning and UX.
