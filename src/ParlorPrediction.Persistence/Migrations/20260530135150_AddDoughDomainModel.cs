using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParlorPrediction.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDoughDomainModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DoughBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalCases = table.Column<int>(type: "int", nullable: false),
                    BallsPerCase = table.Column<int>(type: "int", nullable: false, defaultValue: 12),
                    TotalBalls = table.Column<int>(type: "int", nullable: false),
                    FermentationReadyDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsBalled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    BalledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsEventException = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoughBatches", x => x.Id);
                    table.CheckConstraint("CK_DoughBatches_BalledState", "([IsBalled] = 0 AND [BalledAtUtc] IS NULL) OR ([IsBalled] = 1 AND [BalledAtUtc] IS NOT NULL)");
                    table.CheckConstraint("CK_DoughBatches_BallsPerCase_Positive", "[BallsPerCase] > 0");
                    table.CheckConstraint("CK_DoughBatches_FermentationReadyDate", "DATEDIFF(day, [BatchDate], [FermentationReadyDate]) >= 2");
                    table.CheckConstraint("CK_DoughBatches_TotalBalls_Positive", "[TotalBalls] > 0");
                    table.CheckConstraint("CK_DoughBatches_TotalCases_Positive", "[TotalCases] > 0");
                });

            migrationBuilder.CreateTable(
                name: "DoughInventorySnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SnapshotDate = table.Column<DateOnly>(type: "date", nullable: false),
                    AvailableBalls = table.Column<int>(type: "int", nullable: false),
                    NewBalls = table.Column<int>(type: "int", nullable: false),
                    OldBalls = table.Column<int>(type: "int", nullable: false),
                    ReservedBalls = table.Column<int>(type: "int", nullable: false),
                    UsedBalls = table.Column<int>(type: "int", nullable: false),
                    WasteBalls = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoughInventorySnapshots", x => x.Id);
                    table.CheckConstraint("CK_DoughInventorySnapshots_AvailableBalls_NonNegative", "[AvailableBalls] >= 0");
                    table.CheckConstraint("CK_DoughInventorySnapshots_NewBalls_NonNegative", "[NewBalls] >= 0");
                    table.CheckConstraint("CK_DoughInventorySnapshots_OldBalls_NonNegative", "[OldBalls] >= 0");
                    table.CheckConstraint("CK_DoughInventorySnapshots_ReservedBalls_NonNegative", "[ReservedBalls] >= 0");
                    table.CheckConstraint("CK_DoughInventorySnapshots_UsedBalls_NonNegative", "[UsedBalls] >= 0");
                    table.CheckConstraint("CK_DoughInventorySnapshots_WasteBalls_NonNegative", "[WasteBalls] >= 0");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DoughBatches");

            migrationBuilder.DropTable(
                name: "DoughInventorySnapshots");
        }
    }
}
