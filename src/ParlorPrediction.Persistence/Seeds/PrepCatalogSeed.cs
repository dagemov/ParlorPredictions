namespace ParlorPrediction.Persistence.Seeds;

internal static class PrepCatalogSeed
{
    internal static readonly DateTime SeededAtUtc = new(2026, 5, 30, 0, 0, 0, DateTimeKind.Utc);

    internal static readonly Guid PizzaStationId = Guid.Parse("8b1ccf87-1c7d-4f61-9d4c-511af6c37901");
    internal static readonly Guid BarStationId = Guid.Parse("3c0a95b2-f91c-4b4e-88c2-c4a3cc6c06f3");
    internal static readonly Guid SaladStationId = Guid.Parse("f60a7166-0b1d-40af-9d30-f2d8c3e8f5fb");
    internal static readonly Guid ExpoStationId = Guid.Parse("0a8f8653-bda7-42ee-baad-e9b2449d5eb4");
    internal static readonly Guid GeneralStationId = Guid.Parse("d23f69bc-b4a9-4380-98ce-52d8fe74d2f8");

    internal static readonly Guid DoughItemId = Guid.Parse("db143624-2528-4b32-9f57-a6946440c2dc");
}
