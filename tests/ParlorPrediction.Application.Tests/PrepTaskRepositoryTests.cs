using Microsoft.EntityFrameworkCore;
using ParlorPrediction.Domain.Constants;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;
using ParlorPrediction.Persistence;
using ParlorPrediction.Persistence.Repositories;
using Xunit;

namespace ParlorPrediction.Application.Tests;

public sealed class PrepTaskRepositoryTests
{
    private static readonly Guid PizzaStationId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid DoughItemId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task GetDoughTasksByDateAsync_ExcludesCancelledTasksFromOperationalBoard()
    {
        await using var dbContext = CreateDbContext();
        await SeedCatalogAsync(dbContext);

        var activeTask = CreateTask(new DateOnly(2026, 6, 24));
        var cancelledTask = CreateTask(new DateOnly(2026, 6, 24));
        cancelledTask.Cancel("Superseded duplicate.");

        await dbContext.PrepTasks.AddRangeAsync(activeTask, cancelledTask);
        await dbContext.SaveChangesAsync();

        var repository = new PrepTaskRepository(dbContext);

        var tasks = await repository.GetDoughTasksByDateAsync(new DateOnly(2026, 6, 24));

        Assert.Single(tasks);
        Assert.Equal(activeTask.Id, tasks[0].Id);
    }

    [Fact]
    public async Task GetDoughTasksBetweenDatesAsync_ExcludesCancelledTasksFromPlanningWindows()
    {
        await using var dbContext = CreateDbContext();
        await SeedCatalogAsync(dbContext);

        var firstActive = CreateTask(new DateOnly(2026, 6, 23));
        var cancelledTask = CreateTask(new DateOnly(2026, 6, 24));
        var secondActive = CreateTask(new DateOnly(2026, 6, 25));
        cancelledTask.Cancel("Superseded duplicate.");
        secondActive.Complete("admin-user", 168, completedAtUtc: new DateTime(2026, 6, 25, 16, 0, 0, DateTimeKind.Utc));

        await dbContext.PrepTasks.AddRangeAsync(firstActive, cancelledTask, secondActive);
        await dbContext.SaveChangesAsync();

        var repository = new PrepTaskRepository(dbContext);

        var tasks = await repository.GetDoughTasksBetweenDatesAsync(
            new DateOnly(2026, 6, 23),
            new DateOnly(2026, 6, 25));

        Assert.Equal(2, tasks.Count);
        Assert.DoesNotContain(tasks, task => task.Status == PrepTaskStatus.Cancelled);
    }

    [Fact]
    public async Task SearchDoughTasksAsync_ExcludeCancelledByDefault_ButIncludeForAdminAudit()
    {
        await using var dbContext = CreateDbContext();
        await SeedCatalogAsync(dbContext);

        var activeTask = CreateTask(new DateOnly(2026, 6, 24));
        var cancelledTask = CreateTask(new DateOnly(2026, 6, 24));
        cancelledTask.Cancel("Superseded duplicate.");

        await dbContext.PrepTasks.AddRangeAsync(activeTask, cancelledTask);
        await dbContext.SaveChangesAsync();

        var repository = new PrepTaskRepository(dbContext);

        var operationalTasks = await repository.SearchDoughTasksAsync(
            taskDate: new DateOnly(2026, 6, 24),
            status: null,
            assignedRole: null,
            prepItemId: null);
        var adminTasks = await repository.SearchDoughTasksAsync(
            taskDate: new DateOnly(2026, 6, 24),
            status: null,
            assignedRole: null,
            prepItemId: null,
            includeCancelled: true);

        Assert.Single(operationalTasks);
        Assert.Equal(2, adminTasks.Count);
        Assert.Contains(adminTasks, task => task.Status == PrepTaskStatus.Cancelled);
    }

    [Fact]
    public async Task GetByDoughPrepRecommendationIdAsync_IgnoresCancelledTasks()
    {
        await using var dbContext = CreateDbContext();
        await SeedCatalogAsync(dbContext);

        var recommendationId = Guid.NewGuid();
        var cancelledTask = CreateTask(new DateOnly(2026, 6, 24), recommendationId);
        cancelledTask.Cancel("Cancelled recommendation task.");

        await dbContext.PrepTasks.AddAsync(cancelledTask);
        await dbContext.SaveChangesAsync();

        var repository = new PrepTaskRepository(dbContext);

        var task = await repository.GetByDoughPrepRecommendationIdAsync(recommendationId);

        Assert.Null(task);
    }

    private static ParlorPredictionDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ParlorPredictionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ParlorPredictionDbContext(options);
    }

    private static async Task SeedCatalogAsync(ParlorPredictionDbContext dbContext)
    {
        var station = new PrepStation(PizzaStationId, "Pizza", PrepCatalogCodes.PizzaStation);
        var doughItem = new PrepItem(DoughItemId, station.Id, "Dough", PrepCatalogCodes.DoughItem);

        await dbContext.PrepStations.AddAsync(station);
        await dbContext.PrepItems.AddAsync(doughItem);
        await dbContext.SaveChangesAsync();
    }

    private static PrepTask CreateTask(DateOnly taskDate, Guid? recommendationId = null)
    {
        return PrepTask.Create(
            taskDate: taskDate,
            prepItemId: DoughItemId,
            prepStationId: PizzaStationId,
            assignedRole: ApplicationRole.PizzaMaker,
            quantityRecommended: 168,
            doughPrepRecommendationId: recommendationId,
            taskType: PrepTaskType.BallDough,
            quantityUnit: DoughQuantityUnit.Balls);
    }
}
