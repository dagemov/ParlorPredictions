using Microsoft.AspNetCore.Identity;
using ParlorPrediction.Application.Interfaces.Auth;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Application.Interfaces.Persistence;
using ParlorPrediction.Application.Services.Dough;
using ParlorPrediction.Contracts.Requests.DoughClosing;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;
using Xunit;

namespace ParlorPrediction.Application.Tests;

public sealed class WeeklyDoughClosingServicesTests
{
    [Fact]
    public async Task LeftoverReadyBallsCarryIntoNextWeekAsAvailable()
    {
        var repository = new InMemoryWeeklyDoughClosingRepository();
        var managementService = CreateManagementService(repository);
        var readService = new WeeklyDoughClosingReadService(repository);

        await managementService.CreateWeeklyClosingAsync(new CreateWeeklyDoughClosingRequest
        {
            WeekStartDate = new DateOnly(2026, 6, 2),
            NeededBalls = 900,
            ProducedBalls = 860,
            UsedBalls = 520,
            LostBalls = 40,
            LeftoverReadyBalls = 300,
            LeftoverAttentionBalls = 25,
            LeftoverMixedLoads = 1,
            ClosedByUserId = "manager-user"
        });

        var carryover = await readService.GetCarryoverForWeekAsync(new GetWeeklyDoughCarryoverRequest
        {
            WeekStartDate = new DateOnly(2026, 6, 9)
        });

        Assert.True(carryover.HasClosingCarryover);
        Assert.Equal(300, carryover.CarryoverReadyBalls);
        Assert.Equal(25, carryover.CarryoverAttentionBalls);
        Assert.Equal(325, carryover.CarryoverAvailableBalls);
    }

    [Fact]
    public async Task LeftoverMixedLoadsCarryIntoNextWeekAsMixedButNotBalled()
    {
        var repository = new InMemoryWeeklyDoughClosingRepository();
        var managementService = CreateManagementService(repository);
        var readService = new WeeklyDoughClosingReadService(repository);

        await managementService.CreateWeeklyClosingAsync(new CreateWeeklyDoughClosingRequest
        {
            WeekStartDate = new DateOnly(2026, 6, 2),
            NeededBalls = 700,
            ProducedBalls = 650,
            UsedBalls = 500,
            LostBalls = 20,
            LeftoverReadyBalls = 40,
            LeftoverAttentionBalls = 10,
            LeftoverMixedLoads = 2,
            ClosedByUserId = "manager-user"
        });

        var carryover = await readService.GetCarryoverForWeekAsync(new GetWeeklyDoughCarryoverRequest
        {
            WeekStartDate = new DateOnly(2026, 6, 9)
        });

        Assert.Equal(2, carryover.MixedButNotBalledLoads);
    }

    [Fact]
    public async Task MixedLoadsDoNotCountAsAvailableBalls()
    {
        var repository = new InMemoryWeeklyDoughClosingRepository();
        var managementService = CreateManagementService(repository);
        var readService = new WeeklyDoughClosingReadService(repository);

        await managementService.CreateWeeklyClosingAsync(new CreateWeeklyDoughClosingRequest
        {
            WeekStartDate = new DateOnly(2026, 6, 2),
            NeededBalls = 650,
            ProducedBalls = 610,
            UsedBalls = 480,
            LostBalls = 15,
            LeftoverReadyBalls = 24,
            LeftoverAttentionBalls = 6,
            LeftoverMixedLoads = 3,
            ClosedByUserId = "manager-user"
        });

        var carryover = await readService.GetCarryoverForWeekAsync(new GetWeeklyDoughCarryoverRequest
        {
            WeekStartDate = new DateOnly(2026, 6, 9)
        });

        Assert.Equal(30, carryover.CarryoverAvailableBalls);
        Assert.Equal(3, carryover.MixedButNotBalledLoads);
    }

    [Fact]
    public async Task ClosingSameWeekTwiceDoesNotDuplicateCarryover()
    {
        var repository = new InMemoryWeeklyDoughClosingRepository();
        var managementService = CreateManagementService(repository);
        var readService = new WeeklyDoughClosingReadService(repository);

        await managementService.CreateWeeklyClosingAsync(new CreateWeeklyDoughClosingRequest
        {
            WeekStartDate = new DateOnly(2026, 6, 2),
            NeededBalls = 800,
            ProducedBalls = 780,
            UsedBalls = 610,
            LostBalls = 30,
            LeftoverReadyBalls = 120,
            LeftoverAttentionBalls = 12,
            LeftoverMixedLoads = 1,
            ClosedByUserId = "manager-user"
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => managementService.CreateWeeklyClosingAsync(new CreateWeeklyDoughClosingRequest
        {
            WeekStartDate = new DateOnly(2026, 6, 2),
            NeededBalls = 820,
            ProducedBalls = 790,
            UsedBalls = 630,
            LostBalls = 35,
            LeftoverReadyBalls = 140,
            LeftoverAttentionBalls = 14,
            LeftoverMixedLoads = 2,
            ClosedByUserId = "manager-user"
        }));

        var carryover = await readService.GetCarryoverForWeekAsync(new GetWeeklyDoughCarryoverRequest
        {
            WeekStartDate = new DateOnly(2026, 6, 9)
        });

        Assert.Single(repository.Items);
        Assert.Equal(132, carryover.CarryoverAvailableBalls);
        Assert.Equal(1, carryover.MixedButNotBalledLoads);
    }

    [Fact]
    public async Task PreviousWeekUsedFinishedIsDisplayedSeparately()
    {
        var repository = new InMemoryWeeklyDoughClosingRepository();
        var managementService = CreateManagementService(repository);
        var readService = new WeeklyDoughClosingReadService(repository);

        await managementService.CreateWeeklyClosingAsync(new CreateWeeklyDoughClosingRequest
        {
            WeekStartDate = new DateOnly(2026, 6, 2),
            NeededBalls = 1100,
            ProducedBalls = 1080,
            UsedBalls = 723,
            LostBalls = 18,
            LeftoverReadyBalls = 24,
            LeftoverAttentionBalls = 0,
            LeftoverMixedLoads = 1,
            ClosedByUserId = "manager-user"
        });

        var carryover = await readService.GetCarryoverForWeekAsync(new GetWeeklyDoughCarryoverRequest
        {
            WeekStartDate = new DateOnly(2026, 6, 9)
        });

        Assert.Equal(723, carryover.PreviousWeekUsedBalls);
        Assert.Equal(24, carryover.CarryoverAvailableBalls);
        Assert.Equal(18, carryover.PreviousWeekLostBalls);
    }

    private static WeeklyDoughClosingManagementService CreateManagementService(InMemoryWeeklyDoughClosingRepository repository)
    {
        return new WeeklyDoughClosingManagementService(
            repository,
            new StubUnitOfWork(),
            new StubUserRepository(
                CreateUser("manager-user", ApplicationRole.Manager),
                CreateUser("admin-user", ApplicationRole.Admin),
                CreateUser("pizzamaker-user", ApplicationRole.PizzaMaker)));
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

    private sealed class InMemoryWeeklyDoughClosingRepository : IWeeklyDoughClosingRepository
    {
        public List<WeeklyDoughClosing> Items { get; } = [];

        public Task AddAsync(WeeklyDoughClosing closing, CancellationToken cancellationToken = default)
        {
            Items.Add(closing);
            return Task.CompletedTask;
        }

        public Task<WeeklyDoughClosing?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Items.FirstOrDefault(item => item.Id == id));
        }

        public Task<WeeklyDoughClosing?> GetByWeekStartDateAsync(DateOnly weekStartDate, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Items.FirstOrDefault(item => item.WeekStartDate == weekStartDate));
        }

        public Task<IReadOnlyList<WeeklyDoughClosing>> ListAsync(
            DateOnly? fromWeekStartDate,
            DateOnly? toWeekStartDate,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<WeeklyDoughClosing> query = Items;

            if (fromWeekStartDate.HasValue)
            {
                query = query.Where(item => item.WeekStartDate >= fromWeekStartDate.Value);
            }

            if (toWeekStartDate.HasValue)
            {
                query = query.Where(item => item.WeekStartDate <= toWeekStartDate.Value);
            }

            return Task.FromResult<IReadOnlyList<WeeklyDoughClosing>>(
                query.OrderByDescending(item => item.WeekStartDate).ToArray());
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
