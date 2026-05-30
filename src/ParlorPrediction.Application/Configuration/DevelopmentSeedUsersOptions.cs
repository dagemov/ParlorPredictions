namespace ParlorPrediction.Application.Configuration;

public sealed class DevelopmentSeedUsersOptions
{
    public IList<DevelopmentSeedUserOptions> Users { get; set; } = [];
}
