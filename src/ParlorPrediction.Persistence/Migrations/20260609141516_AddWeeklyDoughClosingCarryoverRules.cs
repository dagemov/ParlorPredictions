using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParlorPrediction.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWeeklyDoughClosingCarryoverRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WeeklyDoughClosings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WeekStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    WeekEndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    NeededBalls = table.Column<int>(type: "int", nullable: false),
                    ProducedBalls = table.Column<int>(type: "int", nullable: false),
                    UsedBalls = table.Column<int>(type: "int", nullable: false),
                    LostBalls = table.Column<int>(type: "int", nullable: false),
                    LeftoverReadyBalls = table.Column<int>(type: "int", nullable: false),
                    LeftoverAttentionBalls = table.Column<int>(type: "int", nullable: false),
                    LeftoverMixedLoads = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_WeeklyDoughClosings", x => x.Id);
                    table.CheckConstraint("CK_WeeklyDoughClosings_LeftoverAttentionBalls_NonNegative", "[LeftoverAttentionBalls] >= 0");
                    table.CheckConstraint("CK_WeeklyDoughClosings_LeftoverMixedLoads_NonNegative", "[LeftoverMixedLoads] >= 0");
                    table.CheckConstraint("CK_WeeklyDoughClosings_LeftoverReadyBalls_NonNegative", "[LeftoverReadyBalls] >= 0");
                    table.CheckConstraint("CK_WeeklyDoughClosings_LostBalls_NonNegative", "[LostBalls] >= 0");
                    table.CheckConstraint("CK_WeeklyDoughClosings_NeededBalls_NonNegative", "[NeededBalls] >= 0");
                    table.CheckConstraint("CK_WeeklyDoughClosings_ProducedBalls_NonNegative", "[ProducedBalls] >= 0");
                    table.CheckConstraint("CK_WeeklyDoughClosings_UsedBalls_NonNegative", "[UsedBalls] >= 0");
                    table.CheckConstraint("CK_WeeklyDoughClosings_WeekWindow", "DATEDIFF(day, [WeekStartDate], [WeekEndDate]) = 5");
                    table.ForeignKey(
                        name: "FK_WeeklyDoughClosings_AspNetUsers_ClosedByUserId",
                        column: x => x.ClosedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WeeklyDoughClosings_AspNetUsers_CorrectedByUserId",
                        column: x => x.CorrectedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyDoughClosings_ClosedAtUtc",
                table: "WeeklyDoughClosings",
                column: "ClosedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyDoughClosings_ClosedByUserId",
                table: "WeeklyDoughClosings",
                column: "ClosedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyDoughClosings_CorrectedByUserId",
                table: "WeeklyDoughClosings",
                column: "CorrectedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyDoughClosings_WeekStartDate",
                table: "WeeklyDoughClosings",
                column: "WeekStartDate",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WeeklyDoughClosings");
        }
    }
}
