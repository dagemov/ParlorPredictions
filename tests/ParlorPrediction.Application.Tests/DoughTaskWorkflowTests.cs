using System.Reflection;
using Microsoft.AspNetCore.Identity;
using ParlorPrediction.Application.Interfaces.Auth;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Application.Interfaces.Persistence;
using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Application.Services.Dough;
using ParlorPrediction.Application.Services.Prep;
using ParlorPrediction.Contracts.Requests.DoughClosing;
using ParlorPrediction.Contracts.Requests.Prep;
using ParlorPrediction.Contracts.Responses.DoughClosing;
using ParlorPrediction.Domain.Constants;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;
using ParlorPrediction.Domain.Rules;
using Xunit;

namespace ParlorPrediction.Application.Tests;

public sealed class DoughTaskWorkflowTests
{
    [Fact]
    public void One_Load_Equals_168_Potential_Balls()
    {
        var task = CreateTask(
            date: new DateOnly(2026, 6, 9),
            taskType: PrepTaskType.MakeDoughLoad,
            quantityUnit: DoughQuantityUnit.FullLoads,
            quantityRecommended: 1);

        Assert.Equal(168, task.RecommendedBallsEquivalent);
    }

    [Fact]
    public async Task Completing_MakeDoughLoad_Does_Not_Increase_Available_Dough_Balls()
    {
        var fixture = CreateFixture();
        var inventorySnapshot = new DoughInventorySnapshot(
            Guid.NewGuid(),
            new DateOnly(2026, 6, 9),
            availableBalls: 48,
            newBalls: 12,
            oldBalls: 36,
            reservedBalls: 0,
            usedBalls: 0,
            wasteBalls: 0);
        fixture.InventorySnapshots.Snapshots.Add(inventorySnapshot);

        var loadTask = CreateTask(
            date: new DateOnly(2026, 6, 9),
            taskType: PrepTaskType.MakeDoughLoad,
            quantityUnit: DoughQuantityUnit.FullLoads,
            quantityRecommended: 1,
            prepItem: fixture.DoughPrepItem);
        fixture.Tasks.Tasks.Add(loadTask);

        await fixture.Service.CompleteAsync(new CompletePrepTaskRequest
        {
            PrepTaskId = loadTask.Id,
            CompletedByUserId = fixture.ActiveUser.Id,
            QuantityUnit = nameof(DoughQuantityUnit.FullLoads),
            QuantityValue = 1
        });

        Assert.Equal(48, fixture.InventorySnapshots.Snapshots.Single().AvailableBalls);
        Assert.Single(fixture.Batches.Batches);
    }

    [Fact]
    public async Task Completing_MakeDoughLoad_Creates_Pending_BallDough_For_Next_Day()
    {
        var fixture = CreateFixture();
        var loadTask = CreateTask(
            date: new DateOnly(2026, 6, 9),
            taskType: PrepTaskType.MakeDoughLoad,
            quantityUnit: DoughQuantityUnit.FullLoads,
            quantityRecommended: 1,
            prepItem: fixture.DoughPrepItem);
        fixture.Tasks.Tasks.Add(loadTask);

        await fixture.Service.CompleteAsync(new CompletePrepTaskRequest
        {
            PrepTaskId = loadTask.Id,
            CompletedByUserId = fixture.ActiveUser.Id,
            QuantityUnit = nameof(DoughQuantityUnit.FullLoads),
            QuantityValue = 1
        });

        var followUpTask = fixture.Tasks.Tasks.Single(task => task.Id != loadTask.Id);

        Assert.Equal(PrepTaskType.BallDough, followUpTask.TaskType);
        Assert.Equal(new DateOnly(2026, 6, 10), followUpTask.TaskDate);
        Assert.Equal(PrepTaskStatus.Pending, followUpTask.Status);
        Assert.Equal(DoughQuantityUnit.Balls, followUpTask.QuantityUnit);
        Assert.Equal(168, followUpTask.QuantityRecommended);
        Assert.Equal(loadTask.Id, followUpTask.SourcePrepTaskId);
        Assert.NotNull(followUpTask.SourceDoughBatchId);
    }

    [Fact]
    public async Task Completing_BallDough_Increases_Available_Balls()
    {
        var fixture = CreateFixture();
        fixture.InventorySnapshots.Snapshots.Add(new DoughInventorySnapshot(
            Guid.NewGuid(),
            new DateOnly(2026, 6, 10),
            availableBalls: 20,
            newBalls: 0,
            oldBalls: 20,
            reservedBalls: 0,
            usedBalls: 0,
            wasteBalls: 0));

        var batch = new DoughBatch(Guid.NewGuid(), new DateOnly(2026, 6, 9), DoughBatch.StandardLoadCases);
        fixture.Batches.Batches.Add(batch);

        var ballTask = CreateTask(
            date: new DateOnly(2026, 6, 10),
            taskType: PrepTaskType.BallDough,
            quantityUnit: DoughQuantityUnit.Balls,
            quantityRecommended: 168,
            sourcePrepTaskId: Guid.NewGuid(),
            sourceDoughBatchId: batch.Id);
        fixture.Tasks.Tasks.Add(ballTask);

        await fixture.Service.CompleteAsync(new CompletePrepTaskRequest
        {
            PrepTaskId = ballTask.Id,
            CompletedByUserId = fixture.ActiveUser.Id,
            QuantityUnit = nameof(DoughQuantityUnit.Balls),
            QuantityValue = 150
        });

        Assert.Equal(170, fixture.InventorySnapshots.Snapshots.Single().AvailableBalls);
        Assert.True(batch.IsBalled);
        Assert.Single(fixture.QualityRecords.Records);
        Assert.Equal(150, fixture.QualityRecords.Records.Single().QuantityBalls);
    }

    [Fact]
    public async Task Pending_BallDough_Work_Is_Visible_As_Actionable_Next_Day_Work()
    {
        var fixture = CreateFixture();
        var loadTask = CreateTask(
            date: new DateOnly(2026, 6, 9),
            taskType: PrepTaskType.MakeDoughLoad,
            quantityUnit: DoughQuantityUnit.FullLoads,
            quantityRecommended: 1,
            prepItem: fixture.DoughPrepItem);
        fixture.Tasks.Tasks.Add(loadTask);

        await fixture.Service.CompleteAsync(new CompletePrepTaskRequest
        {
            PrepTaskId = loadTask.Id,
            CompletedByUserId = fixture.ActiveUser.Id,
            QuantityUnit = nameof(DoughQuantityUnit.FullLoads),
            QuantityValue = 1
        });

        var nextDayTasks = await fixture.Tasks.GetDoughTasksByDateAsync(new DateOnly(2026, 6, 10));
        var ballTask = nextDayTasks.Single();

        Assert.Equal(PrepTaskType.BallDough, ballTask.TaskType);
        Assert.Equal(PrepTaskStatus.Pending, ballTask.Status);
        Assert.Equal(168, ballTask.RecommendedBallsEquivalent);
    }

    [Fact]
    public async Task Tuesday_Scenario_New_Load_Does_Not_Change_Ready_Balls_But_Schedules_Wednesday_Ball_Work()
    {
        var fixture = CreateFixture();
        fixture.InventorySnapshots.Snapshots.Add(new DoughInventorySnapshot(
            Guid.NewGuid(),
            new DateOnly(2026, 6, 9),
            availableBalls: 168,
            newBalls: 0,
            oldBalls: 168,
            reservedBalls: 0,
            usedBalls: 0,
            wasteBalls: 0));

        var loadTask = CreateTask(
            date: new DateOnly(2026, 6, 9),
            taskType: PrepTaskType.MakeDoughLoad,
            quantityUnit: DoughQuantityUnit.FullLoads,
            quantityRecommended: 1,
            prepItem: fixture.DoughPrepItem);
        fixture.Tasks.Tasks.Add(loadTask);

        await fixture.Service.CompleteAsync(new CompletePrepTaskRequest
        {
            PrepTaskId = loadTask.Id,
            CompletedByUserId = fixture.ActiveUser.Id,
            QuantityUnit = nameof(DoughQuantityUnit.FullLoads),
            QuantityValue = 1
        });

        var nextDayBallTask = fixture.Tasks.Tasks.Single(task => task.Id != loadTask.Id);

        Assert.Equal(168, fixture.InventorySnapshots.Snapshots.Single().AvailableBalls);
        Assert.Equal(new DateOnly(2026, 6, 10), nextDayBallTask.TaskDate);
        Assert.Equal(168, nextDayBallTask.RecommendedBallsEquivalent);
        Assert.Equal(PrepTaskType.BallDough, nextDayBallTask.TaskType);
    }

    [Fact]
    public async Task Wednesday_Scenario_Balling_Tuesday_Load_Adds_168_Available_Balls()
    {
        var fixture = CreateFixture();
        fixture.InventorySnapshots.Snapshots.Add(new DoughInventorySnapshot(
            Guid.NewGuid(),
            new DateOnly(2026, 6, 10),
            availableBalls: 168,
            newBalls: 0,
            oldBalls: 168,
            reservedBalls: 0,
            usedBalls: 0,
            wasteBalls: 0));

        var batch = new DoughBatch(Guid.NewGuid(), new DateOnly(2026, 6, 9), DoughBatch.StandardLoadCases);
        fixture.Batches.Batches.Add(batch);

        var ballTask = CreateTask(
            date: new DateOnly(2026, 6, 10),
            taskType: PrepTaskType.BallDough,
            quantityUnit: DoughQuantityUnit.Balls,
            quantityRecommended: 168,
            sourcePrepTaskId: Guid.NewGuid(),
            sourceDoughBatchId: batch.Id);
        fixture.Tasks.Tasks.Add(ballTask);

        await fixture.Service.CompleteAsync(new CompletePrepTaskRequest
        {
            PrepTaskId = ballTask.Id,
            CompletedByUserId = fixture.ActiveUser.Id,
            QuantityUnit = nameof(DoughQuantityUnit.Balls),
            QuantityValue = 168
        });

        Assert.Equal(336, fixture.InventorySnapshots.Snapshots.Single().AvailableBalls);
        Assert.True(batch.IsBalled);
    }

    [Fact]
    public async Task Existing_Generic_Dough_Task_Behavior_Remains_Intact()
    {
        var taskRepository = new InMemoryPrepTaskRepository();
        taskRepository.Tasks.Add(CreateCompletedTask(
            date: new DateOnly(2026, 6, 10),
            taskType: PrepTaskType.GenericDough,
            quantityUnit: DoughQuantityUnit.Balls,
            quantityRecommended: 30,
            quantityCompleted: 30));

        taskRepository.Tasks.Add(CreateCompletedTask(
            date: new DateOnly(2026, 6, 10),
            taskType: PrepTaskType.MakeDoughLoad,
            quantityUnit: DoughQuantityUnit.FullLoads,
            quantityRecommended: 1,
            quantityCompleted: 1));

        var service = new DoughPrepCalculationService(
            new FixedDoughAvailabilityProjectionService(),
            new FixedDemandPlanRepository(new DoughDemandPlan(Guid.NewGuid(), DayOfWeek.Wednesday, "baseline", 100, 100)),
            new InMemoryInventoryReadRepository(),
            taskRepository,
            new EmptyRestaurantEventRepository(),
            new EmptySalesHistoryRepository());

        var result = await service.CalculateAsync(new Contracts.Requests.Dough.CalculateDoughPrepRequest
        {
            TargetDate = new DateOnly(2026, 6, 10),
            HistoricalWeeksToUse = 8
        });

        Assert.Equal(30, result.CompletedBalls);
        Assert.Equal(70, result.MissingBalls);
    }

    private static TestFixture CreateFixture()
    {
        var station = new PrepStation(Guid.NewGuid(), "Pizza", PrepCatalogCodes.PizzaStation);
        var prepItem = new PrepItem(Guid.NewGuid(), station.Id, "Dough", PrepCatalogCodes.DoughItem);
        SetPrivateProperty(prepItem, nameof(PrepItem.PrepStation), station);

        var user = new User
        {
            Id = "pizza-user",
            FirstName = "Pizza",
            LastName = "Maker",
            Email = "pizza@example.com",
            UserName = "pizza@example.com",
            Role = ApplicationRole.PizzaMaker,
            IsActive = true
        };

        var tasks = new InMemoryPrepTaskRepository();
        var batches = new InMemoryDoughBatchRepository();
        var inventorySnapshots = new InMemoryDoughInventorySnapshotRepository();
        var qualityRecords = new InMemoryDoughBatchQualityRepository();

        return new TestFixture(
            prepItem,
            user,
            tasks,
            batches,
            inventorySnapshots,
            qualityRecords,
            new PrepTaskService(
                batches,
                qualityRecords,
                inventorySnapshots,
                new EmptyDoughPrepRecommendationReadRepository(),
                new SinglePrepItemReadRepository(prepItem),
                tasks,
                new InMemoryUnitOfWork(),
                new SingleUserRepository(user)));
    }

    private static PrepTask CreateTask(
        DateOnly date,
        PrepTaskType taskType,
        DoughQuantityUnit quantityUnit,
        int quantityRecommended,
        PrepItem? prepItem = null,
        Guid? sourcePrepTaskId = null,
        Guid? sourceDoughBatchId = null)
    {
        var task = PrepTask.Create(
            date,
            prepItem?.Id ?? Guid.NewGuid(),
            prepItem?.PrepStationId ?? Guid.NewGuid(),
            ApplicationRole.PizzaMaker,
            quantityRecommended,
            taskType: taskType,
            quantityUnit: quantityUnit,
            sourcePrepTaskId: sourcePrepTaskId,
            sourceDoughBatchId: sourceDoughBatchId);

        if (prepItem is not null)
        {
            SetPrivateProperty(task, nameof(PrepTask.PrepItem), prepItem);
            SetPrivateProperty(task, nameof(PrepTask.PrepStation), prepItem.PrepStation);
        }

        return task;
    }

    private static PrepTask CreateCompletedTask(
        DateOnly date,
        PrepTaskType taskType,
        DoughQuantityUnit quantityUnit,
        int quantityRecommended,
        int quantityCompleted)
    {
        var task = PrepTask.Create(
            date,
            Guid.NewGuid(),
            Guid.NewGuid(),
            ApplicationRole.PizzaMaker,
            quantityRecommended,
            taskType: taskType,
            quantityUnit: quantityUnit);
        task.Complete("user-1", quantityCompleted, completedAtUtc: date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        return task;
    }

    private static void SetPrivateProperty(object target, string propertyName, object? value)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Property {propertyName} was not found.");

        property.SetValue(target, value);
    }

    private sealed record TestFixture(
        PrepItem DoughPrepItem,
        User ActiveUser,
        InMemoryPrepTaskRepository Tasks,
        InMemoryDoughBatchRepository Batches,
        InMemoryDoughInventorySnapshotRepository InventorySnapshots,
        InMemoryDoughBatchQualityRepository QualityRecords,
        PrepTaskService Service);

    private sealed class FixedDoughAvailabilityProjectionService : IDoughAvailabilityProjectionService
    {
        public Task<Contracts.Responses.Dough.DoughAvailabilityProjectionResponse> GetWeeklyAvailabilityAsync(
            DateOnly referenceDate,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new Contracts.Responses.Dough.DoughAvailabilityProjectionResponse
            {
                ReferenceDate = referenceDate,
                WeekStartDate = referenceDate,
                WeekEndDate = referenceDate,
                AvailableBalls = 0
            });
        }
    }

    private sealed class InMemoryPrepTaskRepository : IPrepTaskRepository
    {
        public List<PrepTask> Tasks { get; } = [];

        public Task AddAsync(PrepTask task, CancellationToken cancellationToken = default)
        {
            Tasks.Add(task);
            return Task.CompletedTask;
        }

        public Task<PrepTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<PrepTask?>(Tasks.SingleOrDefault(task => task.Id == id));
        }

        public Task<PrepTask?> GetByDoughPrepRecommendationIdAsync(Guid doughPrepRecommendationId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<PrepTask?>(Tasks.SingleOrDefault(task => task.DoughPrepRecommendationId == doughPrepRecommendationId));
        }

        public Task<IReadOnlyList<PrepTask>> GetDoughTasksByDateAsync(DateOnly taskDate, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<PrepTask>>(Tasks.Where(task => task.TaskDate == taskDate).ToArray());
        }

        public Task<IReadOnlyList<PrepTask>> GetDoughTasksBetweenDatesAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<PrepTask>>(Tasks.Where(task => task.TaskDate >= startDate && task.TaskDate <= endDate).ToArray());
        }

        public Task<IReadOnlyList<PrepTask>> SearchDoughTasksAsync(DateOnly? taskDate, PrepTaskStatus? status, ApplicationRole? assignedRole, Guid? prepItemId, bool includeCancelled = false, CancellationToken cancellationToken = default)
        {
            IEnumerable<PrepTask> query = Tasks;

            if (taskDate.HasValue)
            {
                query = query.Where(task => task.TaskDate == taskDate.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(task => task.Status == status.Value);
            }

            if (assignedRole.HasValue)
            {
                query = query.Where(task => task.AssignedRole == assignedRole.Value);
            }

            if (prepItemId.HasValue && prepItemId != Guid.Empty)
            {
                query = query.Where(task => task.PrepItemId == prepItemId.Value);
            }

            return Task.FromResult<IReadOnlyList<PrepTask>>(query.ToArray());
        }

        public void Remove(PrepTask task)
        {
            Tasks.Remove(task);
        }
    }

    private sealed class InMemoryDoughBatchRepository : IDoughBatchRepository
    {
        public List<DoughBatch> Batches { get; } = [];

        public Task AddAsync(DoughBatch batch, CancellationToken cancellationToken = default)
        {
            Batches.Add(batch);
            return Task.CompletedTask;
        }

        public Task<DoughBatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<DoughBatch?>(Batches.SingleOrDefault(batch => batch.Id == id));
        }
    }

    private sealed class InMemoryDoughInventorySnapshotRepository : IDoughInventorySnapshotRepository, IDoughInventoryReadRepository
    {
        public List<DoughInventorySnapshot> Snapshots { get; } = [];

        public Task AddAsync(DoughInventorySnapshot snapshot, CancellationToken cancellationToken = default)
        {
            Snapshots.Add(snapshot);
            return Task.CompletedTask;
        }

        public Task<DoughInventorySnapshot?> GetLatestOnOrBeforeForUpdateAsync(DateOnly targetDate, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<DoughInventorySnapshot?>(
                Snapshots
                    .Where(snapshot => snapshot.SnapshotDate <= targetDate)
                    .OrderByDescending(snapshot => snapshot.SnapshotDate)
                    .ThenByDescending(snapshot => snapshot.UpdatedAtUtc)
                    .FirstOrDefault());
        }

        public Task<DoughInventorySnapshot?> GetLatestSnapshotOnOrBeforeAsync(DateOnly targetDate, CancellationToken cancellationToken = default)
        {
            return GetLatestOnOrBeforeForUpdateAsync(targetDate, cancellationToken);
        }
    }

    private sealed class InMemoryDoughBatchQualityRepository : IDoughBatchQualityRepository
    {
        public List<DoughBatchQualityRecord> Records { get; } = [];

        public Task AddAsync(DoughBatchQualityRecord record, CancellationToken cancellationToken = default)
        {
            Records.Add(record);
            return Task.CompletedTask;
        }

        public Task<DoughBatchQualityRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<DoughBatchQualityRecord?>(Records.SingleOrDefault(record => record.Id == id));
        }

        public Task<IReadOnlyList<DoughBatchQualityRecord>> ListAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<DoughBatchQualityRecord>>(Records.ToArray());
        }

        public Task<IReadOnlyList<DoughBatchQualityRecord>> SearchAsync(DateOnly? sourceDateFrom, DateOnly? sourceDateTo, DateOnly? createdOrBalledFromDate, DateOnly? createdOrBalledToDate, DateOnly? reballedFromDate, DateOnly? reballedToDate, DoughQualityStatus? currentStatus, CancellationToken cancellationToken = default)
        {
            IEnumerable<DoughBatchQualityRecord> query = Records;

            if (sourceDateFrom.HasValue)
            {
                query = query.Where(record => record.SourceDate >= sourceDateFrom.Value);
            }

            if (sourceDateTo.HasValue)
            {
                query = query.Where(record => record.SourceDate <= sourceDateTo.Value);
            }

            if (currentStatus.HasValue)
            {
                query = query.Where(record => record.CurrentStatus == currentStatus.Value);
            }

            return Task.FromResult<IReadOnlyList<DoughBatchQualityRecord>>(query.ToArray());
        }
    }

    private sealed class SinglePrepItemReadRepository : IPrepItemReadRepository
    {
        private readonly PrepItem _prepItem;

        public SinglePrepItemReadRepository(PrepItem prepItem)
        {
            _prepItem = prepItem;
        }

        public Task<IReadOnlyList<PrepItem>> GetActiveAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<PrepItem>>([_prepItem]);
        }

        public Task<PrepItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<PrepItem?>(_prepItem.Id == id ? _prepItem : null);
        }

        public Task<PrepItem?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<PrepItem?>(string.Equals(_prepItem.Code, code, StringComparison.OrdinalIgnoreCase) ? _prepItem : null);
        }
    }

    private sealed class EmptyDoughPrepRecommendationReadRepository : IDoughPrepRecommendationReadRepository
    {
        public Task<DoughPrepRecommendation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<DoughPrepRecommendation?>(null);
        }

        public Task<DoughPrepRecommendation?> GetLatestByDateAsync(DateOnly targetDate, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<DoughPrepRecommendation?>(null);
        }

        public Task<IReadOnlyList<DoughPrepRecommendation>> GetLatestBetweenDatesAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<DoughPrepRecommendation>>(Array.Empty<DoughPrepRecommendation>());
        }
    }

    private sealed class SingleUserRepository : IUserRepository
    {
        private readonly User _user;

        public SingleUserRepository(User user)
        {
            _user = user;
        }

        public Task<User?> FindEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<User?>(string.Equals(_user.Email, email, StringComparison.OrdinalIgnoreCase) ? _user : null);
        }

        public Task<User?> FindByIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<User?>(_user.Id == userId ? _user : null);
        }

        public Task<IdentityResult> CreateAsync(User user, string password) => throw new NotImplementedException();

        public Task<IdentityResult> UpdateAsync(User user) => throw new NotImplementedException();

        public Task<IReadOnlyList<User>> SearchAsync(string? term, ApplicationRole? role, bool activeOnly, bool pendingOnly, IReadOnlyCollection<ApplicationRole> allowedRoles, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<IdentityResult> AddToRoleAsync(User user, string roleName) => throw new NotImplementedException();

        public Task<IdentityResult> RemoveFromRolesAsync(User user, IEnumerable<string> roleNames) => throw new NotImplementedException();

        public Task<IdentityResult> EnsureRoleExistsAsync(string roleName) => throw new NotImplementedException();

        public Task<IReadOnlyList<string>> GetRoleNamesAsync(User user) => throw new NotImplementedException();

        public Task ReloadAsync(User user, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<SignInResult> PasswordSignInAsync(string email, string password) => throw new NotImplementedException();

        public Task<string> GenerateEmailConfirmationTokenAsync(User user) => throw new NotImplementedException();

        public Task<IdentityResult> ConfirmEmailAsync(User user, string token) => throw new NotImplementedException();

        public Task<string> GeneratePasswordResetTokenAsync(User user) => throw new NotImplementedException();

        public Task<IdentityResult> ResetPasswordAsync(User user, string token, string newPassword) => throw new NotImplementedException();

        public Task<IdentityResult> ChangePasswordAsync(User user, string currentPassword, string newPassword) => throw new NotImplementedException();

        public Task StoreRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<RefreshToken?> FindRefreshTokenAsync(string token, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task UpdateRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class InMemoryUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);

        public Task BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task CommitTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FixedDemandPlanRepository : IDoughDemandPlanReadRepository
    {
        private readonly DoughDemandPlan _plan;

        public FixedDemandPlanRepository(DoughDemandPlan plan)
        {
            _plan = plan;
        }

        public Task<IReadOnlyCollection<DoughDemandPlan>> GetActiveByDayOfWeekAsync(DayOfWeek dayOfWeek, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<DoughDemandPlan>>(_plan.DayOfWeek == dayOfWeek ? [_plan] : Array.Empty<DoughDemandPlan>());
        }
    }

    private sealed class InMemoryInventoryReadRepository : IDoughInventoryReadRepository
    {
        public Task<DoughInventorySnapshot?> GetLatestSnapshotOnOrBeforeAsync(DateOnly targetDate, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<DoughInventorySnapshot?>(null);
        }
    }

    private sealed class EmptyRestaurantEventRepository : IRestaurantEventReadRepository
    {
        public Task<IReadOnlyCollection<RestaurantEvent>> GetByDateAsync(DateOnly targetDate, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<RestaurantEvent>>(Array.Empty<RestaurantEvent>());
        }

        public Task<IReadOnlyCollection<RestaurantEvent>> GetBetweenDatesAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<RestaurantEvent>>(Array.Empty<RestaurantEvent>());
        }
    }

    private sealed class EmptySalesHistoryRepository : ISalesHistoryReadRepository
    {
        public Task<IReadOnlyCollection<SalesHistory>> GetRecentByDayOfWeekAsync(DateOnly targetDate, int historicalWeeksToUse, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<SalesHistory>>(Array.Empty<SalesHistory>());
        }
    }
}
