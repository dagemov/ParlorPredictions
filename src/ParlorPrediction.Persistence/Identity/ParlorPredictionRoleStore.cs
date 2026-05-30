using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace ParlorPrediction.Persistence.Identity;

public sealed class ParlorPredictionRoleStore : RoleStore<IdentityRole, ParlorPredictionDbContext, string>
{
    public ParlorPredictionRoleStore(ParlorPredictionDbContext context, IdentityErrorDescriber? describer = null)
        : base(context, describer)
    {
        AutoSaveChanges = false;
    }
}
