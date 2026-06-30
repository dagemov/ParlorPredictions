using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParlorPrediction.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationalProjectionLedgers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConsumptionLedgers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredOn = table.Column<DateOnly>(type: "date", nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SourceEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesBalls = table.Column<int>(type: "int", nullable: false),
                    EventBalls = table.Column<int>(type: "int", nullable: false),
                    ServiceUsageBalls = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumptionLedgers", x => x.Id);
                    table.CheckConstraint("CK_ConsumptionLedgers_EventBalls_NonNegative", "[EventBalls] >= 0");
                    table.CheckConstraint("CK_ConsumptionLedgers_SalesBalls_NonNegative", "[SalesBalls] >= 0");
                    table.CheckConstraint("CK_ConsumptionLedgers_ServiceUsageBalls_NonNegative", "[ServiceUsageBalls] >= 0");
                    table.CheckConstraint("CK_ConsumptionLedgers_SourceType_NotEmpty", "LEN(LTRIM(RTRIM([SourceType]))) > 0");
                });

            migrationBuilder.CreateTable(
                name: "InventoryTransformationLedgers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredOn = table.Column<DateOnly>(type: "date", nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SourceEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BallsRecovered = table.Column<int>(type: "int", nullable: false),
                    BallsDiscarded = table.Column<int>(type: "int", nullable: false),
                    BallsReclassified = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryTransformationLedgers", x => x.Id);
                    table.CheckConstraint("CK_InventoryTransformationLedgers_BallsDiscarded_NonNegative", "[BallsDiscarded] >= 0");
                    table.CheckConstraint("CK_InventoryTransformationLedgers_BallsReclassified_NonNegative", "[BallsReclassified] >= 0");
                    table.CheckConstraint("CK_InventoryTransformationLedgers_BallsRecovered_NonNegative", "[BallsRecovered] >= 0");
                    table.CheckConstraint("CK_InventoryTransformationLedgers_SourceType_NotEmpty", "LEN(LTRIM(RTRIM([SourceType]))) > 0");
                });

            migrationBuilder.CreateTable(
                name: "ProductionLedgers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredOn = table.Column<DateOnly>(type: "date", nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SourceEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalBallsCreated = table.Column<int>(type: "int", nullable: false),
                    BallsCompleted = table.Column<int>(type: "int", nullable: false),
                    BallsReballed = table.Column<int>(type: "int", nullable: false),
                    BallsDiscarded = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionLedgers", x => x.Id);
                    table.CheckConstraint("CK_ProductionLedgers_BallsCompleted_NonNegative", "[BallsCompleted] >= 0");
                    table.CheckConstraint("CK_ProductionLedgers_BallsDiscarded_NonNegative", "[BallsDiscarded] >= 0");
                    table.CheckConstraint("CK_ProductionLedgers_BallsReballed_NonNegative", "[BallsReballed] >= 0");
                    table.CheckConstraint("CK_ProductionLedgers_SourceType_NotEmpty", "LEN(LTRIM(RTRIM([SourceType]))) > 0");
                    table.CheckConstraint("CK_ProductionLedgers_TotalBallsCreated_NonNegative", "[TotalBallsCreated] >= 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsumptionLedgers_OccurredOn",
                table: "ConsumptionLedgers",
                column: "OccurredOn");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumptionLedgers_SourceType_SourceEntityId_CreatedAtUtc",
                table: "ConsumptionLedgers",
                columns: new[] { "SourceType", "SourceEntityId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransformationLedgers_OccurredOn",
                table: "InventoryTransformationLedgers",
                column: "OccurredOn");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransformationLedgers_SourceType_SourceEntityId_CreatedAtUtc",
                table: "InventoryTransformationLedgers",
                columns: new[] { "SourceType", "SourceEntityId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionLedgers_OccurredOn",
                table: "ProductionLedgers",
                column: "OccurredOn");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionLedgers_SourceType_SourceEntityId_CreatedAtUtc",
                table: "ProductionLedgers",
                columns: new[] { "SourceType", "SourceEntityId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsumptionLedgers");

            migrationBuilder.DropTable(
                name: "InventoryTransformationLedgers");

            migrationBuilder.DropTable(
                name: "ProductionLedgers");
        }
    }
}
