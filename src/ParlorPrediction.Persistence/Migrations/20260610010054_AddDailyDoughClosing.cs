using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParlorPrediction.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyDoughClosing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyDoughClosings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClosingDate = table.Column<DateOnly>(type: "date", nullable: false),
                    WeekStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ForecastNeededBalls = table.Column<int>(type: "int", nullable: false),
                    ActualUsedBalls = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ClosedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ClosedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CorrectedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CorrectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CorrectionNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyDoughClosings", x => x.Id);
                    table.CheckConstraint("CK_DailyDoughClosings_ActualUsedBalls_NonNegative", "[ActualUsedBalls] >= 0");
                    table.CheckConstraint("CK_DailyDoughClosings_ForecastNeededBalls_NonNegative", "[ForecastNeededBalls] >= 0");
                    table.ForeignKey(
                        name: "FK_DailyDoughClosings_AspNetUsers_ClosedByUserId",
                        column: x => x.ClosedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DailyDoughClosings_AspNetUsers_CorrectedByUserId",
                        column: x => x.CorrectedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyDoughClosings_ClosedAtUtc",
                table: "DailyDoughClosings",
                column: "ClosedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_DailyDoughClosings_ClosedByUserId",
                table: "DailyDoughClosings",
                column: "ClosedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyDoughClosings_ClosingDate",
                table: "DailyDoughClosings",
                column: "ClosingDate",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyDoughClosings_CorrectedByUserId",
                table: "DailyDoughClosings",
                column: "CorrectedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyDoughClosings_WeekStartDate",
                table: "DailyDoughClosings",
                column: "WeekStartDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyDoughClosings");
        }
    }
}
