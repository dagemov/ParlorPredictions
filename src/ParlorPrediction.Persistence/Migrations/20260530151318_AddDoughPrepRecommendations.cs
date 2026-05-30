using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParlorPrediction.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDoughPrepRecommendations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DoughPrepRecommendations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecommendationDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RequiredBalls = table.Column<int>(type: "int", nullable: false),
                    HistoricalAverageBalls = table.Column<int>(type: "int", nullable: false),
                    EventEstimatedBalls = table.Column<int>(type: "int", nullable: false),
                    AvailableBalls = table.Column<int>(type: "int", nullable: false),
                    MissingBalls = table.Column<int>(type: "int", nullable: false),
                    RecommendedCases = table.Column<int>(type: "int", nullable: false),
                    RecommendedLoads = table.Column<int>(type: "int", nullable: false),
                    ShouldMakeDough = table.Column<bool>(type: "bit", nullable: false),
                    ShouldBallDough = table.Column<bool>(type: "bit", nullable: false),
                    UsesShortFermentationException = table.Column<bool>(type: "bit", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoughPrepRecommendations", x => x.Id);
                    table.CheckConstraint("CK_DoughPrepRecommendations_AvailableBalls_NonNegative", "[AvailableBalls] >= 0");
                    table.CheckConstraint("CK_DoughPrepRecommendations_EventEstimatedBalls_NonNegative", "[EventEstimatedBalls] >= 0");
                    table.CheckConstraint("CK_DoughPrepRecommendations_HistoricalAverageBalls_NonNegative", "[HistoricalAverageBalls] >= 0");
                    table.CheckConstraint("CK_DoughPrepRecommendations_MissingBalls_NonNegative", "[MissingBalls] >= 0");
                    table.CheckConstraint("CK_DoughPrepRecommendations_RecommendedCases_NonNegative", "[RecommendedCases] >= 0");
                    table.CheckConstraint("CK_DoughPrepRecommendations_RecommendedLoads_NonNegative", "[RecommendedLoads] >= 0");
                    table.CheckConstraint("CK_DoughPrepRecommendations_RequiredBalls_NonNegative", "[RequiredBalls] >= 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DoughPrepRecommendations_RecommendationDate",
                table: "DoughPrepRecommendations",
                column: "RecommendationDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DoughPrepRecommendations");
        }
    }
}
