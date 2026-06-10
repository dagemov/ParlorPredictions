using Microsoft.AspNetCore.Identity;
using ParlorPrediction.Application.Interfaces.Auth;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Contracts.Responses.Dough;
using ParlorPrediction.Application.Interfaces.Persistence;
using ParlorPrediction.Application.Services.Dough;
using ParlorPrediction.Contracts.Requests.DoughClosing;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;
using Xunit;

namespace ParlorPrediction.Application.Tests;

public sealed class DailyDoughClosingServicesTests
{
    [Fact]
    public async Task DailyVarianceEqualsForecastMinusActual()
    {
        var repository = new InMemoryDailyDoughClosingRepository();
        var managementService = CreateManagementService(repository);

        var response = await managementService.CreateDailyClosingAsync(new CreateDailyDoughClosingRequest
        {
            ClosingDate = new DateOnly(2026, 6, 3),
            ForecastNeededBalls = 80,
            ActualUsedBalls = 40,
            ClosedByUserId = "manager-user"
        });

        Assert.Equal(40, response.DailyVariance);
    }

    [Fact]
    public async Task ClosingSameDayTwiceIsRejected()
    {
        var repository = new InMemoryDailyDoughClosingRepository();
        var managementService = CreateManagementService(repository);

        await managementService.CreateDailyClosingAsync(new CreateDailyDoughClosingRequest
        {
            ClosingDate = new DateOnly(2026, 6, 3),
            ForecastNeededBalls = 80,
            ActualUsedBalls = 40,
            ClosedByUserId = "manager-user"
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => managementService.CreateDailyClosingAsync(new CreateDailyDoughClosingRequest
        {
            ClosingDate = new DateOnly(2026, 6, 3),
            ForecastNeededBalls = 85,
            ActualUsedBalls = 50,
            ClosedByUserId = "manager-user"
        }));

        Assert.Single(repository.Items);
    }

    [Fact]
    public async Task AccumulatedVarianceSumsClosedDaysInWeek()
    {
        var repository = new InMemoryDailyDoughClosingRepository();
        var managementService = CreateManagementService(repository);
        var readService = CreateReadService(repository);

        await managementService.CreateDailyClosingAsync(new CreateDailyDoughClosingRequest
        {
            ClosingDate = new DateOnly(2026, 6, 3),
            ForecastNeededBalls = 80,
            ActualUsedBalls = 40,
            ClosedByUserId = "manager-user"
        });

        await managementService.CreateDailyClosingAsync(new CreateDailyDoughClosingRequest
        {
            ClosingDate = new DateOnly(2026, 6, 4),
            ForecastNeededBalls = 90,
            ActualUsedBalls = 20,
            ClosedByUserId = "manager-user"
        });

        var summary = await readService.GetWeekSummaryAsync(new GetDailyClosingWeekSummaryRequest
        {
            ReferenceDate = new DateOnly(2026, 6, 4),
            HistoricalWeeksToUse = 8
        });

        Assert.Equal(2, summary.ClosedDaysCount);
        Assert.Equal(110, summary.AccumulatedVariance);
        Assert.Equal(110, summary.AccumulatedSurplus);
        Assert.Equal(60, summary.TotalActualUsedBalls);
    }

    [Fact]
    public async Task DailySurplus_AdjustsRemainingForecast_ButNotInventory()
    {
        var repository = new InMemoryDailyDoughClosingRepository();
        var managementService = CreateManagementService(repository);
        var readService = CreateReadService(repository);

        await managementService.CreateDailyClosingAsync(new CreateDailyDoughClosingRequest
        {
            ClosingDate = new DateOnly(2026, 6, 9),
            ForecastNeededBalls = 80,
            ActualUsedBalls = 65,
            ClosedByUserId = "manager-user"
        });

        var insights = await readService.GetOperationalInsightsAsync(new GetDailyClosingWeekSummaryRequest
        {
            ReferenceDate = new DateOnly(2026, 6, 9),
            HistoricalWeeksToUse = 8
        });

        Assert.Equal(15, insights.AccumulatedSurplus);
        Assert.Equal(420, insights.CurrentAvailableBalls);
        Assert.Equal(insights.RemainingForecastNeed - 15, insights.AdjustedRemainingForecastNeed);
    }

    [Fact]
    public async Task PizzaMakerCannotCloseDailyDough()
    {
        var repository = new InMemoryDailyDoughClosingRepository();
        var managementService = CreateManagementService(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(() => managementService.CreateDailyClosingAsync(new CreateDailyDoughClosingRequest
        {
            ClosingDate = new DateOnly(2026, 6, 3),
            ForecastNeededBalls = 80,
            ActualUsedBalls = 40,
            ClosedByUserId = "pizzamaker-user"
        }));
    }

    private static DailyDoughClosingManagementService CreateManagementService(InMemoryDailyDoughClosingRepository repository)
    {
        return new DailyDoughClosingManagementService(
            repository,
            new StubUnitOfWork(),
            new StubUserRepository(
                CreateUser("manager-user", ApplicationRole.Manager),
                CreateUser("admin-user", ApplicationRole.Admin),
                CreateUser("pizzamaker-user", ApplicationRole.PizzaMaker)));
    }

    private static DailyDoughClosingReadService CreateReadService(InMemoryDailyDoughClosingRepository repository)
    {
        return new DailyDoughClosingReadService(
            repository,
            new StubDoughPrepCalculationService(),
            new StubPrepWeeklyDoughCalendarService());
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

    private sealed class InMemoryDailyDoughClosingRepository : IDailyDoughClosingRepository
    {
        public List<DailyDoughClosing> Items { get; } = [];

        public Task AddAsync(DailyDoughClosing closing, CancellationToken cancellationToken = default)
        {
            Items.Add(closing);
            return Task.CompletedTask;
        }

        public Task<DailyDoughClosing?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Items.FirstOrDefault(item => item.Id == id));
        }

        public Task<DailyDoughClosing?> GetByClosingDateAsync(DateOnly closingDate, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Items.FirstOrDefault(item => item.ClosingDate == closingDate));
        }

        public Task<IReadOnlyList<DailyDoughClosing>> ListByWeekStartDateAsync(
            DateOnly weekStartDate,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<DailyDoughClosing>>(
                Items.Where(item => item.WeekStartDate == weekStartDate).OrderBy(item => item.ClosingDate).ToArray());
        }
    }

    private sealed class StubDoughPrepCalculationService : IDoughPrepCalculationService
    {
        public Task<DoughPrepCalculationResult> CalculateAsync(
            Contracts.Requests.Dough.CalculateDoughPrepRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new DoughPrepCalculationResult
            {
                TargetDate = request.TargetDate,
                RequiredBalls = 100
            });
        }
    }

    private sealed class StubPrepWeeklyDoughCalendarService : IPrepWeeklyDoughCalendarService
    {
        public Task<Contracts.Responses.Prep.WeeklyDoughCalendarResponse> GetWeekAsync(
            DateOnly referenceDate,
            int historicalWeeksToUse,
            CancellationToken cancellationToken = default)
        {
            var weekStart = GetOperationalWeekStart(referenceDate);
            return Task.FromResult(new Contracts.Responses.Prep.WeeklyDoughCalendarResponse
            {
                WeekStartDate = weekStart,
                WeekEndDate = weekStart.AddDays(5),
                ReadyNowBalls = 420,
                StillFermentingBalls = 168,
                MixedButNotBalledBalls = 168,
                WeekTotalNeededBalls = 780
            });
        }

        private static DateOnly GetOperationalWeekStart(DateOnly referenceDate)
        {
            var diff = ((int)referenceDate.DayOfWeek - (int)DayOfWeek.Tuesday + 7) % 7;
            return referenceDate.AddDays(-diff);
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
