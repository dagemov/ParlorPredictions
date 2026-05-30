namespace ParlorPrediction.Persistence.Seeds;

internal static class DoughDemandPlanSeed
{
    private static readonly DateTime SeededAtUtc = new(2026, 5, 30, 0, 0, 0, DateTimeKind.Utc);

    internal static readonly object[] Values =
    [
        Create("1fd39395-730e-45ff-bfbb-5200fd9a4e01", DayOfWeek.Tuesday, "Restaurant", 60, 60),
        Create("6de48f1d-a319-4d43-b98f-82561fe7ab02", DayOfWeek.Wednesday, "Restaurant", 60, 80),
        Create("61a2fa90-e538-4af7-8c63-ef10e5d2ee03", DayOfWeek.Thursday, "Restaurant", 80, 100),
        Create("f2ea9a3e-5980-4d0f-bc0e-f0f576e69d04", DayOfWeek.Thursday, "Westport Farmers Market", 100, 150),
        Create("d2855c63-f748-4a8d-a4b8-d35f6c645a05", DayOfWeek.Friday, "Rowayton Farmers Market", 80, 120),
        Create("09d8b6c8-33df-4e7f-b1e8-e7de54370306", DayOfWeek.Friday, "Restaurant", 170, 250),
        Create("825e3150-7db6-42d2-8d37-b881622c8e07", DayOfWeek.Saturday, "Ridgefield Farmers Market", 100, 120),
        Create("7d30f506-b486-40f2-9a38-8e3e6e16c908", DayOfWeek.Saturday, "Saturday Night", 80, 120),
        Create("8375c417-4f0c-45c0-b9c5-24d1b2ebaa09", DayOfWeek.Sunday, "Restaurant", 60, 95)
    ];

    private static object Create(
        string id,
        DayOfWeek dayOfWeek,
        string sourceName,
        int minDoughBalls,
        int maxDoughBalls)
    {
        return new
        {
            Id = Guid.Parse(id),
            DayOfWeek = dayOfWeek,
            SourceName = sourceName,
            MinDoughBalls = minDoughBalls,
            MaxDoughBalls = maxDoughBalls,
            Notes = (string?)null,
            IsActive = true,
            CreatedAtUtc = SeededAtUtc,
            UpdatedAtUtc = SeededAtUtc
        };
    }
}
