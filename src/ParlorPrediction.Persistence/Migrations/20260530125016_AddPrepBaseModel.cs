using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ParlorPrediction.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPrepBaseModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PrepStations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrepStations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PrepItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrepStationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrepItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrepItems_PrepStations_PrepStationId",
                        column: x => x.PrepStationId,
                        principalTable: "PrepStations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "PrepStations",
                columns: new[] { "Id", "Code", "CreatedAtUtc", "Description", "IsActive", "Name", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { new Guid("0a8f8653-bda7-42ee-baad-e9b2449d5eb4"), "EXPO", new DateTime(2026, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), "Expo preparation station.", true, "Expo", new DateTime(2026, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("3c0a95b2-f91c-4b4e-88c2-c4a3cc6c06f3"), "BAR", new DateTime(2026, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), "Bar preparation station.", true, "Bar", new DateTime(2026, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("8b1ccf87-1c7d-4f61-9d4c-511af6c37901"), "PIZZA", new DateTime(2026, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), "Primary pizza preparation station.", true, "Pizza", new DateTime(2026, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("d23f69bc-b4a9-4380-98ce-52d8fe74d2f8"), "GENERAL", new DateTime(2026, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), "General restaurant preparation station.", true, "General", new DateTime(2026, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("f60a7166-0b1d-40af-9d30-f2d8c3e8f5fb"), "SALAD", new DateTime(2026, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), "Salad preparation station.", true, "Salad", new DateTime(2026, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "PrepItems",
                columns: new[] { "Id", "Code", "CreatedAtUtc", "Description", "IsActive", "Name", "PrepStationId", "UpdatedAtUtc" },
                values: new object[] { new Guid("db143624-2528-4b32-9f57-a6946440c2dc"), "DOUGH", new DateTime(2026, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), "Base dough preparation item for pizza service.", true, "Dough", new Guid("8b1ccf87-1c7d-4f61-9d4c-511af6c37901"), new DateTime(2026, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.CreateIndex(
                name: "IX_PrepItems_Code",
                table: "PrepItems",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrepItems_PrepStationId",
                table: "PrepItems",
                column: "PrepStationId");

            migrationBuilder.CreateIndex(
                name: "IX_PrepStations_Code",
                table: "PrepStations",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrepItems");

            migrationBuilder.DropTable(
                name: "PrepStations");
        }
    }
}
