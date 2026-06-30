using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using ParlorPrediction.Application.Interfaces.Auth;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Application.Interfaces.Persistence;
using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Application.Services.Dough;
using ParlorPrediction.Contracts.Requests.Dough;
using ParlorPrediction.Contracts.Requests.Prep;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;
using ParlorPrediction.Mvc.Controllers;
using Xunit;

namespace ParlorPrediction.Application.Tests;

public sealed class AdminDoughCorrectionToolsTests
{
    [Fact]
    public async Task CorrectPrepTaskAsync_AdminCanEditCompletedPrepTask()
    {
        var task = PrepTask.Create(
            taskDate: new DateOnly(2026, 6, 16),
            prepItemId: Guid.NewGuid(),
            prepStationId: Guid.NewGuid(),
            assignedRole: ApplicationRole.PizzaMaker,
            quantityRecommended: 1,
            taskType: PrepTaskType.MakeDoughLoad,
            quantityUnit: DoughQuantityUnit.FullLoads,
            notes: "original");
        task.Complete("pizza-maker", 1, "finished");

        var prepTasks = new InMemoryPrepTaskRepository(task);
        var service = CreateService(
            prepTaskRepository: prepTasks,
            actingUser: CreateUser("admin-user", ApplicationRole.Admin));

        await service.CorrectPrepTaskAsync(new AdminCorrectPrepTaskRequest
        {
            PrepTaskId = task.Id,
            TaskDate = new DateOnly(2026, 6, 17),
            TaskType = nameof(PrepTaskType.BallDough),
            QuantityUnit = nameof(DoughQuantityUnit.Balls),
            QuantityRecommended = 168,
            Status = nameof(PrepTaskStatus.Completed),
            QuantityCompleted = 168,
            CompletedAtUtc = new DateTime(2026, 6, 17, 13, 0, 0, DateTimeKind.Utc),
            CompletedByUserId = "admin-user",
            SourcePrepTaskId = Guid.NewGuid(),
            SourceDoughBatchId = Guid.NewGuid(),
            Notes = "corrected",
            UpdatedByUserId = "admin-user"
        });

        Assert.Equal(new DateOnly(2026, 6, 17), task.TaskDate);
        Assert.Equal(PrepTaskType.BallDough, task.TaskType);
        Assert.Equal(DoughQuantityUnit.Balls, task.QuantityUnit);
        Assert.Equal(168, task.QuantityRecommended);
        Assert.Equal(PrepTaskStatus.Completed, task.Status);
        Assert.Equal(168, task.QuantityCompleted);
        Assert.Equal("admin-user", task.CompletedByUserId);
        Assert.Equal("corrected", task.Notes);
    }

    [Fact]
    public async Task CorrectDoughBatchAsync_AdminCanVoidOrphanBatch()
    {
        var batch = new DoughBatch(
            Guid.NewGuid(),
            batchDate: new DateOnly(2026, 6, 11),
            totalCases: DoughBatch.StandardLoadCases,
            notes: "orphan");

        var batches = new InMemoryDoughBatchRepository(batch);
        var service = CreateService(
            doughBatchRepository: batches,
            actingUser: CreateUser("admin-user", ApplicationRole.Admin));

        await service.CorrectDoughBatchAsync(new AdminCorrectDoughBatchRequest
        {
            DoughBatchId = batch.Id,
            BatchDate = batch.BatchDate,
            TotalCases = batch.TotalCases,
            IsBalled = false,
            IsEventException = false,
            IsVoided = true,
            VoidReason = "orphan batch correction",
            Notes = "do not count in planning",
            UpdatedByUserId = "admin-user"
        });

        Assert.True(batch.IsVoided);
        Assert.Equal("orphan batch correction", batch.VoidReason);
        Assert.False(batch.IsBalled);
        Assert.Equal("do not count in planning", batch.Notes);
    }

    [Fact]
    public async Task CorrectPrepTaskAsync_ManagerCannotPerformAdminOnlyCorrection()
    {
        var task = PrepTask.Create(
            taskDate: new DateOnly(2026, 6, 16),
            prepItemId: Guid.NewGuid(),
            prepStationId: Guid.NewGuid(),
            assignedRole: ApplicationRole.PizzaMaker,
            quantityRecommended: 1,
            taskType: PrepTaskType.MakeDoughLoad,
            quantityUnit: DoughQuantityUnit.FullLoads);

        var service = CreateService(
            prepTaskRepository: new InMemoryPrepTaskRepository(task),
            actingUser: CreateUser("manager-user", ApplicationRole.Manager));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CorrectPrepTaskAsync(
            new AdminCorrectPrepTaskRequest
            {
                PrepTaskId = task.Id,
                TaskDate = task.TaskDate,
                TaskType = nameof(PrepTaskType.MakeDoughLoad),
                QuantityUnit = nameof(DoughQuantityUnit.FullLoads),
                QuantityRecommended = 1,
                Status = nameof(PrepTaskStatus.Pending),
                QuantityCompleted = 0,
                UpdatedByUserId = "manager-user"
            }));

        Assert.Contains("Only admin users", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AdminDoughCorrectionsController_RequiresAdminOrManagerDashboard_AndAdminOnlyEdits()
    {
        var controllerType = typeof(AdminDoughCorrectionsController);
        var classAuthorize = controllerType.GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(classAuthorize);
        Assert.Contains(nameof(ApplicationRole.Admin), classAuthorize!.Roles ?? string.Empty, StringComparison.Ordinal);
        Assert.Contains(nameof(ApplicationRole.Manager), classAuthorize.Roles ?? string.Empty, StringComparison.Ordinal);
        Assert.DoesNotContain(nameof(ApplicationRole.PizzaMaker), classAuthorize.Roles ?? string.Empty, StringComparison.Ordinal);

        AssertEditActionRequiresAdmin(nameof(AdminDoughCorrectionsController.EditPrepTask), typeof(Guid), typeof(DateOnly?), typeof(CancellationToken));
        AssertEditActionRequiresAdmin(nameof(AdminDoughCorrectionsController.EditPrepTask), typeof(Guid), typeof(ParlorPrediction.Mvc.Models.AdminDoughCorrections.AdminPrepTaskCorrectionFormViewModel), typeof(CancellationToken));
        AssertEditActionRequiresAdmin(nameof(AdminDoughCorrectionsController.EditDoughBatch), typeof(Guid), typeof(DateOnly?), typeof(CancellationToken));
        AssertEditActionRequiresAdmin(nameof(AdminDoughCorrectionsController.EditDoughBatch), typeof(Guid), typeof(ParlorPrediction.Mvc.Models.AdminDoughCorrections.AdminDoughBatchCorrectionFormViewModel), typeof(CancellationToken));
    }

    private static void AssertEditActionRequiresAdmin(string methodName, params Type[] parameterTypes)
    {
        var method = typeof(AdminDoughCorrectionsController).GetMethod(methodName, parameterTypes);
        Assert.NotNull(method);

        var authorize = method!.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorize);
        Assert.Equal(nameof(ApplicationRole.Admin), authorize!.Roles);
    }

    private static DoughCorrectionAdminService CreateService(
        IDoughBatchRepository? doughBatchRepository = null,
        IPrepTaskRepository? prepTaskRepository = null,
        User? actingUser = null)
    {
        return new DoughCorrectionAdminService(
            doughBatchRepository ?? new InMemoryDoughBatchRepository(),
            prepTaskRepository ?? new InMemoryPrepTaskRepository(),
            new NoOpUnitOfWork(),
            new StubUserRepository(actingUser ?? CreateUser("admin-user", ApplicationRole.Admin)));
    }

    private static User CreateUser(string id, ApplicationRole role)
    {
        return new User
        {
            Id = id,
            Email = $"{id}@example.com",
            UserName = id,
            FirstName = "Test",
            LastName = "User",
            Role = role,
            IsActive = true
        };
    }

    private sealed class InMemoryDoughBatchRepository : IDoughBatchRepository
    {
        private readonly List<DoughBatch> _batches = [];

        public InMemoryDoughBatchRepository(params DoughBatch[] batches)
        {
            _batches.AddRange(batches);
        }

        public Task AddAsync(DoughBatch batch, CancellationToken cancellationToken = default)
        {
            _batches.Add(batch);
            return Task.CompletedTask;
        }

        public Task<DoughBatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<DoughBatch?>(_batches.SingleOrDefault(batch => batch.Id == id));
        }
    }

    private sealed class InMemoryPrepTaskRepository : IPrepTaskRepository
    {
        private readonly List<PrepTask> _tasks = [];

        public InMemoryPrepTaskRepository(params PrepTask[] tasks)
        {
            _tasks.AddRange(tasks);
        }

        public Task AddAsync(PrepTask task, CancellationToken cancellationToken = default)
        {
            _tasks.Add(task);
            return Task.CompletedTask;
        }

        public Task<PrepTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<PrepTask?>(_tasks.SingleOrDefault(task => task.Id == id));
        }

        public Task<PrepTask?> GetByDoughPrepRecommendationIdAsync(Guid doughPrepRecommendationId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<PrepTask?>(_tasks.SingleOrDefault(task => task.DoughPrepRecommendationId == doughPrepRecommendationId));
        }

        public Task<IReadOnlyList<PrepTask>> GetDoughTasksByDateAsync(DateOnly taskDate, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<PrepTask>>(_tasks.Where(task => task.TaskDate == taskDate).ToArray());
        }

        public Task<IReadOnlyList<PrepTask>> GetDoughTasksBetweenDatesAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<PrepTask>>(
                _tasks.Where(task => task.TaskDate >= startDate && task.TaskDate <= endDate).ToArray());
        }

        public Task<IReadOnlyList<PrepTask>> SearchDoughTasksAsync(DateOnly? taskDate, PrepTaskStatus? status, ApplicationRole? assignedRole, Guid? prepItemId, bool includeCancelled = false, CancellationToken cancellationToken = default)
        {
            IEnumerable<PrepTask> query = _tasks;

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

            if (prepItemId.HasValue && prepItemId.Value != Guid.Empty)
            {
                query = query.Where(task => task.PrepItemId == prepItemId.Value);
            }

            return Task.FromResult<IReadOnlyList<PrepTask>>(query.ToArray());
        }

        public void Remove(PrepTask task)
        {
            _tasks.Remove(task);
        }
    }

    private sealed class NoOpUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);

        public Task BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task CommitTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class StubUserRepository : IUserRepository
    {
        private readonly User _user;

        public StubUserRepository(User user)
        {
            _user = user;
        }

        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default) => Task.FromResult<User?>(null);

        public Task<User?> FindByIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<User?>(string.Equals(_user.Id, userId, StringComparison.Ordinal) ? _user : null);
        }

        public Task<IdentityResult> CreateAsync(User user, string password) => Task.FromResult(IdentityResult.Success);

        public Task<IdentityResult> UpdateAsync(User user) => Task.FromResult(IdentityResult.Success);

        public Task<IReadOnlyList<User>> SearchAsync(string? term, ApplicationRole? role, bool activeOnly, bool pendingOnly, IReadOnlyCollection<ApplicationRole> allowedRoles, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<User>>([_user]);
        }

        public Task<IdentityResult> AddToRoleAsync(User user, string roleName) => Task.FromResult(IdentityResult.Success);

        public Task<IdentityResult> RemoveFromRolesAsync(User user, IEnumerable<string> roleNames) => Task.FromResult(IdentityResult.Success);

        public Task<IdentityResult> EnsureRoleExistsAsync(string roleName) => Task.FromResult(IdentityResult.Success);

        public Task<IReadOnlyList<string>> GetRoleNamesAsync(User user) => Task.FromResult<IReadOnlyList<string>>([_user.Role.ToString()]);

        public Task ReloadAsync(User user, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<Microsoft.AspNetCore.Identity.SignInResult> PasswordSignInAsync(string email, string password)
            => Task.FromResult(Microsoft.AspNetCore.Identity.SignInResult.Success);

        public Task<string> GenerateEmailConfirmationTokenAsync(User user) => Task.FromResult(string.Empty);

        public Task<IdentityResult> ConfirmEmailAsync(User user, string token) => Task.FromResult(IdentityResult.Success);

        public Task<string> GeneratePasswordResetTokenAsync(User user) => Task.FromResult(string.Empty);

        public Task<IdentityResult> ResetPasswordAsync(User user, string token, string newPassword) => Task.FromResult(IdentityResult.Success);

        public Task<IdentityResult> ChangePasswordAsync(User user, string currentPassword, string newPassword) => Task.FromResult(IdentityResult.Success);

        public Task StoreRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<RefreshToken?> FindRefreshTokenAsync(string token, CancellationToken cancellationToken = default) => Task.FromResult<RefreshToken?>(null);

        public Task UpdateRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
