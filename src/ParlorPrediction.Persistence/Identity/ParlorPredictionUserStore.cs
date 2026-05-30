using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Persistence.Identity;

public sealed class ParlorPredictionUserStore : UserStore<User, IdentityRole, ParlorPredictionDbContext, string>
{
    public ParlorPredictionUserStore(ParlorPredictionDbContext context, IdentityErrorDescriber? describer = null)
        : base(context, describer)
    {
        AutoSaveChanges = false;
    }
}
