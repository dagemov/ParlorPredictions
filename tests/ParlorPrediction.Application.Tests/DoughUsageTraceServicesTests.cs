using Microsoft.AspNetCore.Identity;
using ParlorPrediction.Application.Interfaces.Auth;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Application.Interfaces.Persistence;
using ParlorPrediction.Application.Services.Dough;
using ParlorPrediction.Contracts.Requests.DoughUsage;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;
using Xunit;

namespace ParlorPrediction.Application.Tests;

public sealed class DoughUsageTraceServicesTests
{
    [Fact]
    public async Task CreateAsync_OneTrayRecordsTwelveBalls()
    {
        var fixture = CreateFixture();
        var source = fixture.AddSourceRecord(quantityBalls: 48, status: DoughQualityStatus.Good);

        var response = await fixture.ManagementService.CreateAsync(new CreateDoughUsageTraceRequest
        {
            UsageDate = new DateOnly(2026, 6, 16),
            SourceDoughBatchQualityRecordId = source.Id,
            Destination = "Restaurant",
            TrayCount = 1,
            CreatedByUserId = "manager-user"
        });

        Assert.Equal(12, response.BallsUsed);
        Assert.Single(fixture.UsageTraces.Items);
        Assert.Equal(12, fixture.UsageTraces.Items[0].BallsUsed);
    }

    [Fact]
    public async Task CreateAsync_HalfCaseRecordsSixBalls()
    {
        var fixture = CreateFixture();
        var source = fixture.AddSourceRecord(quantityBalls: 48, status: DoughQualityStatus.Good);

        var response = await fixture.ManagementService.CreateAsync(new CreateDoughUsageTraceRequest
        {
            UsageDate = new DateOnly(2026, 6, 16),
            SourceDoughBatchQualityRecordId = source.Id,
            Destination = "Restaurant",
            TrayCount = 0.5m,
            CreatedByUserId = "manager-user"
        });

        Assert.Equal(6, response.BallsUsed);
        Assert.Equal(6, fixture.UsageTraces.Items[0].BallsUsed);
    }

    [Fact]
    public async Task CreateAsync_ThreeTraysRecordsThirtySixBallsUsed()
    {
        var fixture = CreateFixture();
        var source = fixture.AddSourceRecord(quantityBalls: 60, status: DoughQualityStatus.Good);

        var response = await fixture.ManagementService.CreateAsync(new CreateDoughUsageTraceRequest
        {
            UsageDate = new DateOnly(2026, 6, 16),
            SourceDoughBatchQualityRecordId = source.Id,
            Destination = "Restaurant",
            TrayCount = 3,
            CreatedByUserId = "manager-user"
        });

        Assert.Equal(36, response.BallsUsed);
    }

    [Fact]
    public async Task GetRemainingBySourceAsync_UsageTraceReducesRemainingBallsFromSelectedSource()
    {
        var fixture = CreateFixture();
        var source = fixture.AddSourceRecord(quantityBalls: 60, status: DoughQualityStatus.Good);

        await fixture.ManagementService.CreateAsync(new CreateDoughUsageTraceRequest
        {
            UsageDate = new DateOnly(2026, 6, 16),
            SourceDoughBatchQualityRecordId = source.Id,
            Destination = "Restaurant",
            TrayCount = 2,
            CreatedByUserId = "manager-user"
        });

        var remaining = await fixture.ReadService.GetRemainingBySourceAsync(new GetDoughRemainingBySourceRequest
        {
            ReferenceDate = new DateOnly(2026, 6, 16)
        });

        var selectedSource = Assert.Single(remaining, item => item.SourceDoughBatchQualityRecordId == source.Id);
        Assert.Equal(24, selectedSource.UsedBalls);
        Assert.Equal(36, selectedSource.RemainingBalls);
    }

    [Fact]
    public async Task CreateAsync_CannotUseMoreThanRemain()
    {
        var fixture = CreateFixture();
        var source = fixture.AddSourceRecord(quantityBalls: 24, status: DoughQualityStatus.Good);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.ManagementService.CreateAsync(
            new CreateDoughUsageTraceRequest
            {
                UsageDate = new DateOnly(2026, 6, 16),
                SourceDoughBatchQualityRecordId = source.Id,
                Destination = "Restaurant",
                TrayCount = 3,
                CreatedByUserId = "manager-user"
            }));

        Assert.Contains("only 24 remain", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetAvailableSourcesForRestaurant_PutsMustUseNextDayFirst()
    {
        var fixture = CreateFixture();
        fixture.AddSourceRecord(quantityBalls: 36, status: DoughQualityStatus.Good, sourceDate: new DateOnly(2026, 6, 14), createdAtUtc: new DateTime(2026, 6, 14, 12, 0, 0, DateTimeKind.Utc));
        fixture.AddSourceRecord(quantityBalls: 24, status: DoughQualityStatus.MustUseNextDay, sourceDate: new DateOnly(2026, 6, 13), createdAtUtc: new DateTime(2026, 6, 13, 12, 0, 0, DateTimeKind.Utc), mustUseByDate: new DateOnly(2026, 6, 17));

        var sources = await fixture.ReadService.GetAvailableSourcesForDateAsync(new GetAvailableDoughSourcesRequest
        {
            UsageDate = new DateOnly(2026, 6, 16),
            Destination = "Restaurant"
        });

        Assert.NotEmpty(sources);
        Assert.Equal("MustUseNextDay", sources[0].SourceType);
    }

    [Fact]
    public async Task GetAvailableSourcesForSummerEvent_WarnsOnOldOrReballedDough()
    {
        var fixture = CreateFixture();
        fixture.AddSourceRecord(quantityBalls: 24, status: DoughQualityStatus.Reballed, sourceDate: new DateOnly(2026, 7, 10), createdAtUtc: new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc));
        fixture.AddSourceRecord(quantityBalls: 24, status: DoughQualityStatus.Good, sourceDate: new DateOnly(2026, 7, 14), createdAtUtc: new DateTime(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc));

        var sources = await fixture.ReadService.GetAvailableSourcesForDateAsync(new GetAvailableDoughSourcesRequest
        {
            UsageDate = new DateOnly(2026, 7, 15),
            Destination = "Event"
        });

        Assert.Contains(sources, source => source.SourceType == "Reballed" && source.HasWarning);
    }

    [Fact]
    public async Task GetAvailableSourcesForFarmersMarketInSummer_WarnsOnMustUseAndOlderDough()
    {
        var fixture = CreateFixture();
        fixture.AddSourceRecord(
            quantityBalls: 24,
            status: DoughQualityStatus.MustUseNextDay,
            sourceDate: new DateOnly(2026, 7, 12),
            createdAtUtc: new DateTime(2026, 7, 12, 12, 0, 0, DateTimeKind.Utc),
            mustUseByDate: new DateOnly(2026, 7, 16));

        var sources = await fixture.ReadService.GetAvailableSourcesForDateAsync(new GetAvailableDoughSourcesRequest
        {
            UsageDate = new DateOnly(2026, 7, 15),
            Destination = "FarmersMarket"
        });

        Assert.Contains(sources, source => source.SourceType == "MustUseNextDay" && source.HasWarning);
    }

    [Fact]
    public async Task GetAvailableSourcesForRestaurant_AllowsReballedAndMustUseNextDay()
    {
        var fixture = CreateFixture();
        fixture.AddSourceRecord(quantityBalls: 24, status: DoughQualityStatus.Reballed, sourceDate: new DateOnly(2026, 6, 13), createdAtUtc: new DateTime(2026, 6, 13, 12, 0, 0, DateTimeKind.Utc));
        fixture.AddSourceRecord(quantityBalls: 24, status: DoughQualityStatus.MustUseNextDay, sourceDate: new DateOnly(2026, 6, 14), createdAtUtc: new DateTime(2026, 6, 14, 12, 0, 0, DateTimeKind.Utc), mustUseByDate: new DateOnly(2026, 6, 17));

        var sources = await fixture.ReadService.GetAvailableSourcesForDateAsync(new GetAvailableDoughSourcesRequest
        {
            UsageDate = new DateOnly(2026, 6, 16),
            Destination = "Restaurant"
        });

        Assert.Contains(sources, source => source.SourceType == "Reballed");
        Assert.Contains(sources, source => source.SourceType == "MustUseNextDay");
    }

    [Fact]
    public async Task GetReballPlanningForDate_UsesRemainingDoughAfterUsageTracesForReballCandidates()
    {
        var fixture = CreateFixture();
        var source = fixture.AddSourceRecord(
            quantityBalls: 48,
            status: DoughQualityStatus.Good,
            sourceDate: new DateOnly(2026, 6, 10),
            createdAtUtc: new DateTime(2026, 6, 10, 12, 0, 0, DateTimeKind.Utc));

        await fixture.ManagementService.CreateAsync(new CreateDoughUsageTraceRequest
        {
            UsageDate = new DateOnly(2026, 6, 15),
            SourceDoughBatchQualityRecordId = source.Id,
            Destination = "Restaurant",
            TrayCount = 2,
            CreatedByUserId = "manager-user"
        });

        var plan = await fixture.ReadService.GetReballPlanningForDateAsync(new GetDoughReballPlanningRequest
        {
            ReferenceDate = new DateOnly(2026, 6, 15)
        });

        var candidate = Assert.Single(plan.ReballCandidates, item => item.SourceDoughBatchQualityRecordId == source.Id);
        Assert.Equal(24, candidate.RemainingBalls);
    }

    [Fact]
    public async Task GetAvailableSourcesForDate_ExcludesDiscardedDough()
    {
        var fixture = CreateFixture();
        fixture.AddSourceRecord(quantityBalls: 24, status: DoughQualityStatus.Discarded, sourceDate: new DateOnly(2026, 6, 14), createdAtUtc: new DateTime(2026, 6, 14, 12, 0, 0, DateTimeKind.Utc), discardReason: DoughLossReason.ManagerDecision);
        fixture.AddSourceRecord(quantityBalls: 24, status: DoughQualityStatus.Good, sourceDate: new DateOnly(2026, 6, 15), createdAtUtc: new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc));

        var sources = await fixture.ReadService.GetAvailableSourcesForDateAsync(new GetAvailableDoughSourcesRequest
        {
            UsageDate = new DateOnly(2026, 6, 16),
            Destination = "Restaurant"
        });

        Assert.DoesNotContain(sources, source => source.SourceType == "Discarded");
        Assert.Single(sources);
    }

    private static Fixture CreateFixture()
    {
        var qualityRepository = new InMemoryDoughBatchQualityRepository();
        var usageRepository = new InMemoryDoughUsageTraceRepository();
        var sourceProjectionService = new DoughSourceProjectionService(qualityRepository, usageRepository);
        var userRepository = new StubUserRepository(
            CreateUser("manager-user", ApplicationRole.Manager),
            CreateUser("admin-user", ApplicationRole.Admin),
            CreateUser("pizzamaker-user", ApplicationRole.PizzaMaker));

        return new Fixture(
            qualityRepository,
            usageRepository,
            sourceProjectionService,
            new DoughUsageTraceManagementService(
                qualityRepository,
                usageRepository,
                new StubUnitOfWork(),
                userRepository),
            new DoughUsageTraceReadService(
                sourceProjectionService,
                usageRepository));
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

    private sealed record Fixture(
        InMemoryDoughBatchQualityRepository QualityRecords,
        InMemoryDoughUsageTraceRepository UsageTraces,
        DoughSourceProjectionService SourceProjectionService,
        DoughUsageTraceManagementService ManagementService,
        DoughUsageTraceReadService ReadService)
    {
        public DoughBatchQualityRecord AddSourceRecord(
            int quantityBalls,
            DoughQualityStatus status,
            DateOnly? sourceDate = null,
            DateTime? createdAtUtc = null,
            DateOnly? mustUseByDate = null,
            DoughLossReason? discardReason = null)
        {
            var record = DoughBatchQualityRecord.Create(
                sourceDate ?? new DateOnly(2026, 6, 15),
                createdAtUtc ?? new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc),
                quantityBalls,
                "manager-user",
                initialStatus: status,
                mustUseByDate: mustUseByDate,
                discardReason: discardReason);

            QualityRecords.Records.Add(record);
            return record;
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
            return Task.FromResult<DoughBatchQualityRecord?>(Records.FirstOrDefault(record => record.Id == id));
        }

        public Task<IReadOnlyList<DoughBatchQualityRecord>> ListAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<DoughBatchQualityRecord>>(Records.ToArray());
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
            IEnumerable<DoughBatchQualityRecord> query = Records;

            if (currentStatus.HasValue)
            {
                query = query.Where(record => record.CurrentStatus == currentStatus.Value);
            }

            return Task.FromResult<IReadOnlyList<DoughBatchQualityRecord>>(query.ToArray());
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
