using ParlorPrediction.Mvc.Models.DoughQuality;
using Xunit;

namespace ParlorPrediction.Application.Tests;

public sealed class DoughQualityDisplayTextTests
{
    [Fact]
    public void MustUseNextDay_Appears_As_UseFirst_In_Kitchen_Copy()
    {
        var label = DoughQualityDisplayText.FormatKitchenPriority("MustUseNextDay");

        Assert.Equal("Use First", label);
    }
}
