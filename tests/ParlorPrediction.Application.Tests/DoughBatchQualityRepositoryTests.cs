using Microsoft.EntityFrameworkCore;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;
using ParlorPrediction.Persistence;
using ParlorPrediction.Persistence.Repositories;
using Xunit;

namespace ParlorPrediction.Application.Tests;

public sealed class DoughBatchQualityRepositoryTests
{
    [Fact]
    public async Task ListAsync_ExcludesQualityRecordsFromCancelledPrepTasks()
    {
        await using var dbContext = CreateDbContext();

        var visibleTask = CreateCancelledOrActiveTask(cancelled: false);
        var cancelledTask = CreateCancelledOrActiveTask(cancelled: true);

        await dbContext.PrepTasks.AddRangeAsync(visibleTask, cancelledTask);
        await dbContext.DoughBatchQualityRecords.AddRangeAsync(
            CreateQualityRecord(visibleTask.Id, new DateOnly(2026, 6, 24)),
            CreateQualityRecord(cancelledTask.Id, new DateOnly(2026, 6, 25)));
        await dbContext.SaveChangesAsync();

        var repository = new DoughBatchQualityRepository(dbContext);

        var records = await repository.ListAsync();

        Assert.Single(records);
        Assert.Equal(visibleTask.Id, records[0].OriginalDoughTaskId);
    }

    [Fact]
    public async Task SearchAsync_ExcludesQualityRecordsFromCancelledPrepTasks()
    {
        await using var dbContext = CreateDbContext();

        var visibleTask = CreateCancelledOrActiveTask(cancelled: false);
        var cancelledTask = CreateCancelledOrActiveTask(cancelled: true);

        await dbContext.PrepTasks.AddRangeAsync(visibleTask, cancelledTask);
        await dbContext.DoughBatchQualityRecords.AddRangeAsync(
            CreateQualityRecord(visibleTask.Id, new DateOnly(2026, 6, 24)),
            CreateQualityRecord(cancelledTask.Id, new DateOnly(2026, 6, 25)));
        await dbContext.SaveChangesAsync();

        var repository = new DoughBatchQualityRepository(dbContext);

        var records = await repository.SearchAsync(
            sourceDateFrom: new DateOnly(2026, 6, 24),
            sourceDateTo: new DateOnly(2026, 6, 25),
            createdOrBalledFromDate: null,
            createdOrBalledToDate: null,
            reballedFromDate: null,
            reballedToDate: null,
            currentStatus: null);

        Assert.Single(records);
        Assert.Equal(visibleTask.Id, records[0].OriginalDoughTaskId);
    }

    private static ParlorPredictionDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ParlorPredictionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ParlorPredictionDbContext(options);
    }

    private static PrepTask CreateCancelledOrActiveTask(bool cancelled)
    {
        var task = PrepTask.Create(
            taskDate: new DateOnly(2026, 6, 24),
            prepItemId: Guid.NewGuid(),
            prepStationId: Guid.NewGuid(),
            assignedRole: ApplicationRole.PizzaMaker,
            quantityRecommended: 168,
            taskType: PrepTaskType.BallDough,
            quantityUnit: DoughQuantityUnit.Balls);

        task.Complete(
            completedByUserId: "admin-user",
            quantityCompleted: 168,
            completedAtUtc: new DateTime(2026, 6, 24, 11, 0, 0, DateTimeKind.Utc));

        if (cancelled)
        {
            task.AdminCorrect(
                taskDate: task.TaskDate,
                taskType: task.TaskType,
                quantityUnit: task.QuantityUnit,
                quantityRecommended: task.QuantityRecommended,
                status: PrepTaskStatus.Cancelled,
                quantityCompleted: 0,
                completedAtUtc: null,
                completedByUserId: null,
                sourcePrepTaskId: task.SourcePrepTaskId,
                sourceDoughBatchId: task.SourceDoughBatchId,
                notes: "Superseded duplicate for repository visibility test.");
        }

        return task;
    }

    private static DoughBatchQualityRecord CreateQualityRecord(Guid prepTaskId, DateOnly sourceDate)
    {
        return DoughBatchQualityRecord.Create(
            sourceDate: sourceDate,
            createdOrBalledAt: sourceDate.ToDateTime(new TimeOnly(10, 0), DateTimeKind.Utc),
            quantityBalls: 168,
            createdByUserId: "admin-user",
            originalDoughTaskId: prepTaskId);
    }
}
