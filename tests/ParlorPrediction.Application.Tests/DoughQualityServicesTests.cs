using Microsoft.AspNetCore.Identity;
using ParlorPrediction.Application.Interfaces.Auth;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Application.Interfaces.Persistence;
using ParlorPrediction.Application.Services.Dough;
using ParlorPrediction.Contracts.Requests.DoughQuality;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;
using Xunit;

namespace ParlorPrediction.Application.Tests;

public sealed class DoughQualityServicesTests
{
    [Fact]
    public async Task GetSummaryAsync_AttentionCountsAsAvailable()
    {
        var qualityRepository = new InMemoryDoughBatchQualityRepository();
        await qualityRepository.AddAsync(CreateRecord(120, DoughQualityStatus.Good, "admin-user"));
        await qualityRepository.AddAsync(CreateRecord(80, DoughQualityStatus.Attention, "admin-user"));

        var service = new DoughQualityReadService(qualityRepository, new InMemoryDoughLossRecordRepository());

        var summary = await service.GetSummaryAsync();

        Assert.Equal(80, summary.AttentionBalls);
        Assert.Equal(200, summary.TotalAvailableBalls);
    }

    [Fact]
    public async Task GetSummaryAsync_DiscardedDoesNotCountAsAvailable()
    {
        var qualityRepository = new InMemoryDoughBatchQualityRepository();
        await qualityRepository.AddAsync(CreateRecord(100, DoughQualityStatus.Good, "admin-user"));
        await qualityRepository.AddAsync(CreateRecord(40, DoughQualityStatus.Discarded, "admin-user", discardReason: DoughLossReason.ManagerDecision));

        var service = new DoughQualityReadService(qualityRepository, new InMemoryDoughLossRecordRepository());

        var summary = await service.GetSummaryAsync();

        Assert.Equal(40, summary.DiscardedBalls);
        Assert.Equal(100, summary.TotalAvailableBalls);
    }

    [Fact]
    public async Task ReballAsync_PartialRecoveryCreatesLossAndMustUseNextDay()
    {
        var qualityRepository = new InMemoryDoughBatchQualityRepository();
        var lossRepository = new InMemoryDoughLossRecordRepository();
        var reballRepository = new InMemoryDoughReballRecordRepository();
        var userRepository = new StubUserRepository(
            CreateUser("manager-user", ApplicationRole.Manager),
            CreateUser("pizzamaker-user", ApplicationRole.PizzaMaker));

        var record = CreateRecord(100, DoughQualityStatus.Good, "manager-user");
        await qualityRepository.AddAsync(record);

        var service = new DoughQualityManagementService(
            qualityRepository,
            lossRepository,
            reballRepository,
            new StubUnitOfWork(),
            userRepository);

        var reballDate = new DateTime(2026, 6, 8, 10, 0, 0, DateTimeKind.Utc);
        var response = await service.ReballAsync(new ReballDoughRequest
        {
            DoughBatchQualityRecordId = record.Id,
            QuantityRecoveredBalls = 70,
            ReballDateUtc = reballDate,
            Result = "PartialRecovered",
            UpdatedByUserId = "pizzamaker-user"
        });

        Assert.Equal("MustUseNextDay", response.CurrentStatus);
        Assert.Equal(DateOnly.FromDateTime(reballDate).AddDays(1), response.MustUseByDate);
        Assert.Equal(70, record.QuantityBalls);
        Assert.Single(lossRepository.Items);
        Assert.Equal(30, lossRepository.Items[0].QuantityLostBalls);
        Assert.Single(reballRepository.Items);
        Assert.Equal(70, reballRepository.Items[0].QuantityRecoveredBalls);
        Assert.Equal(30, reballRepository.Items[0].QuantityLostBalls);
    }

    [Fact]
    public async Task DiscardAsync_RequiresReason()
    {
        var qualityRepository = new InMemoryDoughBatchQualityRepository();
        var lossRepository = new InMemoryDoughLossRecordRepository();
        var reballRepository = new InMemoryDoughReballRecordRepository();
        var userRepository = new StubUserRepository(CreateUser("manager-user", ApplicationRole.Manager));

        var record = CreateRecord(50, DoughQualityStatus.Good, "manager-user");
        await qualityRepository.AddAsync(record);

        var service = new DoughQualityManagementService(
            qualityRepository,
            lossRepository,
            reballRepository,
            new StubUnitOfWork(),
            userRepository);

        await Assert.ThrowsAsync<ArgumentException>(() => service.DiscardAsync(new DiscardDoughRequest
        {
            DoughBatchQualityRecordId = record.Id,
            DiscardReason = string.Empty,
            UpdatedByUserId = "manager-user"
        }));
    }

    [Fact]
    public async Task CorrectStatusAsync_AdminCanCorrectStatus()
    {
        var qualityRepository = new InMemoryDoughBatchQualityRepository();
        var lossRepository = new InMemoryDoughLossRecordRepository();
        var reballRepository = new InMemoryDoughReballRecordRepository();
        var userRepository = new StubUserRepository(CreateUser("admin-user", ApplicationRole.Admin));

        var record = CreateRecord(60, DoughQualityStatus.Good, "admin-user");
        await qualityRepository.AddAsync(record);

        var service = new DoughQualityManagementService(
            qualityRepository,
            lossRepository,
            reballRepository,
            new StubUnitOfWork(),
            userRepository);

        var response = await service.CorrectStatusAsync(new CorrectDoughQualityStatusRequest
        {
            DoughBatchQualityRecordId = record.Id,
            NewStatus = "Attention",
            StatusReason = "Older dough needs review",
            UpdatedByUserId = "admin-user"
        });

        Assert.Equal("Attention", response.CurrentStatus);
        Assert.Equal("Older dough needs review", response.StatusReason);
    }

    private static DoughBatchQualityRecord CreateRecord(
        int quantityBalls,
        DoughQualityStatus status,
        string createdByUserId,
        DoughLossReason? discardReason = null)
    {
        return DoughBatchQualityRecord.Create(
            new DateOnly(2026, 6, 8),
            new DateTime(2026, 6, 5, 8, 0, 0, DateTimeKind.Utc),
            quantityBalls,
            createdByUserId,
            initialStatus: status,
            discardReason: discardReason);
    }

    private static User CreateUser(string id, ApplicationRole role)
    {
        return new User
        {
            Id = id,
            UserName = id,
            Email = $"{id}@parlor.local",
            FirstName = id,
            LastName = "Test",
            IsActive = true,
            Role = role
        };
    }

    private sealed class InMemoryDoughBatchQualityRepository : IDoughBatchQualityRepository
    {
        private readonly List<DoughBatchQualityRecord> _items = [];

        public Task AddAsync(DoughBatchQualityRecord record, CancellationToken cancellationToken = default)
        {
            _items.Add(record);
            return Task.CompletedTask;
        }

        public Task<DoughBatchQualityRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_items.FirstOrDefault(item => item.Id == id));
        }

        public Task<IReadOnlyList<DoughBatchQualityRecord>> ListAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<DoughBatchQualityRecord>>(_items.ToArray());
        }

        public Task<IReadOnlyList<DoughBatchQualityRecord>> SearchAsync(
            DateOnly? sourceDateFrom,
            DateOnly? sourceDateTo,
            DateOnly? createdOrBalledFromDate,
            DateOnly? createdOrBalledToDate,
            DateOnly? reballedFromDate,
            DateOnly? reballedToDate,
            DoughQualityStatus? currentStatus,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<DoughBatchQualityRecord> query = _items;

            if (sourceDateFrom.HasValue)
            {
                query = query.Where(item => item.SourceDate >= sourceDateFrom.Value);
            }

            if (sourceDateTo.HasValue)
            {
                query = query.Where(item => item.SourceDate <= sourceDateTo.Value);
            }

            if (createdOrBalledFromDate.HasValue)
            {
                query = query.Where(item => DateOnly.FromDateTime(item.CreatedOrBalledAt) >= createdOrBalledFromDate.Value);
            }

            if (createdOrBalledToDate.HasValue)
            {
                query = query.Where(item => DateOnly.FromDateTime(item.CreatedOrBalledAt) <= createdOrBalledToDate.Value);
            }

            if (reballedFromDate.HasValue)
            {
                query = query.Where(item => item.ReballedAt.HasValue && DateOnly.FromDateTime(item.ReballedAt.Value) >= reballedFromDate.Value);
            }

            if (reballedToDate.HasValue)
            {
                query = query.Where(item => item.ReballedAt.HasValue && DateOnly.FromDateTime(item.ReballedAt.Value) <= reballedToDate.Value);
            }

            if (currentStatus.HasValue)
            {
                query = query.Where(item => item.CurrentStatus == currentStatus.Value);
            }

            return Task.FromResult<IReadOnlyList<DoughBatchQualityRecord>>(query.ToArray());
        }
    }

    private sealed class InMemoryDoughLossRecordRepository : IDoughLossRecordRepository
    {
        public List<DoughLossRecord> Items { get; } = [];

        public Task AddAsync(DoughLossRecord record, CancellationToken cancellationToken = default)
        {
            Items.Add(record);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<DoughLossRecord>> SearchAsync(
            DateOnly? fromDate,
            DateOnly? toDate,
            DoughLossReason? lossReason,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<DoughLossRecord> query = Items;

            if (fromDate.HasValue)
            {
                query = query.Where(item => item.LossDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(item => item.LossDate <= toDate.Value);
            }

            if (lossReason.HasValue)
            {
                query = query.Where(item => item.LossReason == lossReason.Value);
            }

            return Task.FromResult<IReadOnlyList<DoughLossRecord>>(query.ToArray());
        }
    }

    private sealed class InMemoryDoughReballRecordRepository : IDoughReballRecordRepository
    {
        public List<DoughReballRecord> Items { get; } = [];

        public Task AddAsync(DoughReballRecord record, CancellationToken cancellationToken = default)
        {
            Items.Add(record);
            return Task.CompletedTask;
        }
    }

    private sealed class StubUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
        public Task BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CommitTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class StubUserRepository : IUserRepository
    {
        private readonly Dictionary<string, User> _users;

        public StubUserRepository(params User[] users)
        {
            _users = users.ToDictionary(user => user.Id);
        }

        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
            => Task.FromResult<User?>(_users.Values.FirstOrDefault(user => string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase)));

        public Task<User?> FindByIdAsync(string userId, CancellationToken cancellationToken = default)
            => Task.FromResult(_users.TryGetValue(userId, out var user) ? user : null);

        public Task<IdentityResult> CreateAsync(User user, string password) => throw new NotSupportedException();
        public Task<IdentityResult> UpdateAsync(User user) => throw new NotSupportedException();
        public Task<IReadOnlyList<User>> SearchAsync(string? term, ApplicationRole? role, bool activeOnly, bool pendingOnly, IReadOnlyCollection<ApplicationRole> allowedRoles, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IdentityResult> AddToRoleAsync(User user, string roleName) => throw new NotSupportedException();
        public Task<IdentityResult> RemoveFromRolesAsync(User user, IEnumerable<string> roleNames) => throw new NotSupportedException();
        public Task<IdentityResult> EnsureRoleExistsAsync(string roleName) => throw new NotSupportedException();
        public Task<IReadOnlyList<string>> GetRoleNamesAsync(User user) => throw new NotSupportedException();
        public Task ReloadAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<SignInResult> PasswordSignInAsync(string email, string password) => throw new NotSupportedException();
        public Task<string> GenerateEmailConfirmationTokenAsync(User user) => throw new NotSupportedException();
        public Task<IdentityResult> ConfirmEmailAsync(User user, string token) => throw new NotSupportedException();
        public Task<string> GeneratePasswordResetTokenAsync(User user) => throw new NotSupportedException();
        public Task<IdentityResult> ResetPasswordAsync(User user, string token, string newPassword) => throw new NotSupportedException();
        public Task<IdentityResult> ChangePasswordAsync(User user, string currentPassword, string newPassword) => throw new NotSupportedException();
        public Task StoreRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<RefreshToken?> FindRefreshTokenAsync(string token, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
