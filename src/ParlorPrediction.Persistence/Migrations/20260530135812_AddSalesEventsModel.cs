using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParlorPrediction.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesEventsModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RestaurantEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    EventDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EstimatedPizzas = table.Column<int>(type: "int", nullable: false),
                    EstimatedDoughBalls = table.Column<int>(type: "int", nullable: false),
                    AllowShortFermentation = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ExternalCalendarEventId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantEvents", x => x.Id);
                    table.CheckConstraint("CK_RestaurantEvents_EstimatedDoughBalls_NonNegative", "[EstimatedDoughBalls] >= 0");
                    table.CheckConstraint("CK_RestaurantEvents_EstimatedPizzas_NonNegative", "[EstimatedPizzas] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "SalesHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SaleDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    QuantitySold = table.Column<int>(type: "int", nullable: false),
                    DoughBallsUsed = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesHistories", x => x.Id);
                    table.CheckConstraint("CK_SalesHistories_DoughBallsUsed_NonNegative", "[DoughBallsUsed] >= 0");
                    table.CheckConstraint("CK_SalesHistories_QuantitySold_NonNegative", "[QuantitySold] >= 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantEvents_EventDate",
                table: "RestaurantEvents",
                column: "EventDate");

            migrationBuilder.CreateIndex(
                name: "IX_SalesHistories_ProductName",
                table: "SalesHistories",
                column: "ProductName");

            migrationBuilder.CreateIndex(
                name: "IX_SalesHistories_SaleDate",
                table: "SalesHistories",
                column: "SaleDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RestaurantEvents");

            migrationBuilder.DropTable(
                name: "SalesHistories");
        }
    }
}
