using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ParlorPrediction.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDoughDemandPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RestaurantEvents_EventDate",
                table: "RestaurantEvents");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "RestaurantEvents",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "DoughDemandPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayOfWeek = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    SourceName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    MinDoughBalls = table.Column<int>(type: "int", nullable: false),
                    MaxDoughBalls = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoughDemandPlans", x => x.Id);
                    table.CheckConstraint("CK_DoughDemandPlans_MaxDoughBalls_NonNegative", "[MaxDoughBalls] >= 0");
                    table.CheckConstraint("CK_DoughDemandPlans_MaxGreaterThanMin", "[MaxDoughBalls] >= [MinDoughBalls]");
                    table.CheckConstraint("CK_DoughDemandPlans_MinDoughBalls_NonNegative", "[MinDoughBalls] >= 0");
                });

            migrationBuilder.InsertData(
                table: "DoughDemandPlans",
                columns: new[] { "Id", "CreatedAtUtc", "DayOfWeek", "IsActive", "MaxDoughBalls", "MinDoughBalls", "Notes", "SourceName", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { new Guid("09d8b6c8-33df-4e7f-b1e8-e7de54370306"), new DateTime(2026, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), "Friday", true, 250, 170, null, "Restaurant", new DateTime(2026, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("1fd39395-730e-45ff-bfbb-5200fd9a4e01"), new DateTime(2026, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), "Tuesday", true, 60, 60, null, "Restaurant", new DateTime(2026, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("61a2fa90-e538-4af7-8c63-ef10e5d2ee03"), new DateTime(2026, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), "Thursday", true, 100, 80, null, "Restaurant", new DateTime(2026, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("6de48f1d-a319-4d43-b98f-82561fe7ab02"), new DateTime(2026, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), "Wednesday", true, 80, 60, null, "Restaurant", new DateTime(2026, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("7d30f506-b486-40f2-9a38-8e3e6e16c908"), new DateTime(2026, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), "Saturday", true, 120, 80, null, "Saturday Night", new DateTime(2026, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("825e3150-7db6-42d2-8d37-b881622c8e07"), new DateTime(2026, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), "Saturday", true, 120, 100, null, "Ridgefield Farmers Market", new DateTime(2026, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("8375c417-4f0c-45c0-b9c5-24d1b2ebaa09"), new DateTime(2026, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), "Sunday", true, 95, 60, null, "Restaurant", new DateTime(2026, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("d2855c63-f748-4a8d-a4b8-d35f6c645a05"), new DateTime(2026, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), "Friday", true, 120, 80, null, "Rowayton Farmers Market", new DateTime(2026, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("f2ea9a3e-5980-4d0f-bc0e-f0f576e69d04"), new DateTime(2026, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), "Thursday", true, 150, 100, null, "Westport Farmers Market", new DateTime(2026, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantEvents_EventDate_IsActive",
                table: "RestaurantEvents",
                columns: new[] { "EventDate", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_DoughDemandPlans_DayOfWeek_IsActive",
                table: "DoughDemandPlans",
                columns: new[] { "DayOfWeek", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_DoughDemandPlans_DayOfWeek_SourceName",
                table: "DoughDemandPlans",
                columns: new[] { "DayOfWeek", "SourceName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DoughDemandPlans");

            migrationBuilder.DropIndex(
                name: "IX_RestaurantEvents_EventDate_IsActive",
                table: "RestaurantEvents");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "RestaurantEvents");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantEvents_EventDate",
                table: "RestaurantEvents",
                column: "EventDate");
        }
    }
}
