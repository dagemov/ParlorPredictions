SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @AdminUserId nvarchar(450) = N'8aa785af-3dcb-4f5c-9109-578827fa4399';
DECLARE @WeeklyClosingId uniqueidentifier = '3BD7DD46-9C77-4F87-8FF9-7F6F1ACD54A1';
DECLARE @OldCarryoverBatchId uniqueidentifier = '2D2524F6-FF38-4D65-B63F-D73FCD3ABD54';
DECLARE @OldCarryoverBallTaskId uniqueidentifier = '5A54F242-5707-407E-A08B-AC4E90AE417E';
DECLARE @OldCarryoverQualityRecordId uniqueidentifier = '47245EAE-D845-4F09-804A-54C4928B862D';
DECLARE @ReballedCarryoverQualityRecordId uniqueidentifier = '44444444-4444-4444-4444-444444444444';

DECLARE @OldCarryoverBalledAtUtc datetime2(7) = '2026-06-15T17:30:00';
DECLARE @ReballedCarryoverCreatedUtc datetime2(7) = '2026-06-15T18:00:00';
DECLARE @CorrectionAppliedAtUtc datetime2(7) = SYSUTCDATETIME();

BEGIN TRANSACTION;

-- Keep the previous weekly closing aligned with the real carryover that physically existed
-- at the start of the Tue 2026-06-16 service week.
UPDATE dbo.WeeklyDoughClosings
SET LeftoverReadyBalls = 296,
    LeftoverAttentionBalls = 0,
    LeftoverMixedLoads = 1,
    CorrectedByUserId = @AdminUserId,
    CorrectedAtUtc = @CorrectionAppliedAtUtc,
    CorrectionNote = N'Admin correction 2026-06-17: carryover reset to 296 ready balls plus 1 mixed load to match physical count before Tue 2026-06-16 service.'
WHERE Id = @WeeklyClosingId;

IF @@ROWCOUNT <> 1
BEGIN
    THROW 50001, 'Expected to correct exactly one weekly closing row.', 1;
END;

-- Re-anchor the old 2026-06-11 batch as pre-week carryover instead of a new current-week balling event.
UPDATE dbo.DoughBatches
SET BalledAtUtc = @OldCarryoverBalledAtUtc,
    UpdatedAtUtc = @CorrectionAppliedAtUtc
WHERE Id = @OldCarryoverBatchId;

IF @@ROWCOUNT <> 1
BEGIN
    THROW 50002, 'Expected to correct exactly one dough batch row.', 1;
END;

UPDATE dbo.PrepTasks
SET CompletedAtUtc = @OldCarryoverBalledAtUtc,
    UpdatedAtUtc = @CorrectionAppliedAtUtc
WHERE Id = @OldCarryoverBallTaskId;

IF @@ROWCOUNT <> 1
BEGIN
    THROW 50003, 'Expected to correct exactly one BallDough prep task row.', 1;
END;

UPDATE dbo.DoughBatchQualityRecords
SET CreatedOrBalledAt = @OldCarryoverBalledAtUtc,
    ManagerNote = N'Admin correction 2026-06-17: old 2026-06-11 dough batch belongs to pre-week carryover, not new current-week production.',
    UpdatedByUserId = @AdminUserId,
    UpdatedAtUtc = @CorrectionAppliedAtUtc
WHERE Id = @OldCarryoverQualityRecordId;

IF @@ROWCOUNT <> 1
BEGIN
    THROW 50004, 'Expected to correct exactly one dough quality carryover row.', 1;
END;

-- Record the four reballed cases that physically remain on hand as a live carryover source.
IF NOT EXISTS (
    SELECT 1
    FROM dbo.DoughBatchQualityRecords
    WHERE Id = @ReballedCarryoverQualityRecordId)
BEGIN
    INSERT INTO dbo.DoughBatchQualityRecords
    (
        Id,
        SourceDate,
        OriginalDoughTaskId,
        CreatedOrBalledAt,
        QuantityBalls,
        CurrentStatus,
        StatusReason,
        AttentionMarkedAt,
        ReballedAt,
        MustUseByDate,
        DiscardedAt,
        DiscardReason,
        ManagerNote,
        CreatedByUserId,
        UpdatedByUserId,
        CreatedAtUtc,
        UpdatedAtUtc
    )
    VALUES
    (
        @ReballedCarryoverQualityRecordId,
        '2026-06-15',
        NULL,
        @ReballedCarryoverCreatedUtc,
        48,
        N'Good',
        N'Admin correction 2026-06-17: four reballed cases physically on hand.',
        NULL,
        @ReballedCarryoverCreatedUtc,
        NULL,
        NULL,
        NULL,
        N'Admin correction 2026-06-17: 4 reballed cases (48 balls) carried into the Tue 2026-06-16 service week.',
        @AdminUserId,
        @AdminUserId,
        @CorrectionAppliedAtUtc,
        @CorrectionAppliedAtUtc
    );
END;
ELSE
BEGIN
    UPDATE dbo.DoughBatchQualityRecords
    SET SourceDate = '2026-06-15',
        OriginalDoughTaskId = NULL,
        CreatedOrBalledAt = @ReballedCarryoverCreatedUtc,
        QuantityBalls = 48,
        CurrentStatus = N'Good',
        StatusReason = N'Admin correction 2026-06-17: four reballed cases physically on hand.',
        AttentionMarkedAt = NULL,
        ReballedAt = @ReballedCarryoverCreatedUtc,
        MustUseByDate = NULL,
        DiscardedAt = NULL,
        DiscardReason = NULL,
        ManagerNote = N'Admin correction 2026-06-17: 4 reballed cases (48 balls) carried into the Tue 2026-06-16 service week.',
        UpdatedByUserId = @AdminUserId,
        UpdatedAtUtc = @CorrectionAppliedAtUtc
    WHERE Id = @ReballedCarryoverQualityRecordId;
END;

COMMIT TRANSACTION;
