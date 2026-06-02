using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParlorPrediction.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddManagerPrepRecommendations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ManagerPrepRecommendations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecommendationDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PrepItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecommendationText = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    RecommendedBalls = table.Column<int>(type: "int", nullable: false),
                    RecommendedCases = table.Column<int>(type: "int", nullable: false),
                    RecommendedLoads = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManagerPrepRecommendations", x => x.Id);
                    table.CheckConstraint("CK_ManagerPrepRecommendations_RecommendedBalls_NonNegative", "[RecommendedBalls] >= 0");
                    table.CheckConstraint("CK_ManagerPrepRecommendations_RecommendedCases_NonNegative", "[RecommendedCases] >= 0");
                    table.CheckConstraint("CK_ManagerPrepRecommendations_RecommendedLoads_NonNegative", "[RecommendedLoads] >= 0");
                    table.ForeignKey(
                        name: "FK_ManagerPrepRecommendations_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ManagerPrepRecommendations_PrepItems_PrepItemId",
                        column: x => x.PrepItemId,
                        principalTable: "PrepItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ManagerPrepRecommendations_CreatedByUserId",
                table: "ManagerPrepRecommendations",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ManagerPrepRecommendations_PrepItemId",
                table: "ManagerPrepRecommendations",
                column: "PrepItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ManagerPrepRecommendations_RecommendationDate",
                table: "ManagerPrepRecommendations",
                column: "RecommendationDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ManagerPrepRecommendations");
        }
    }
}
