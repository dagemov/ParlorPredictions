using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParlorPrediction.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPrepTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PrepTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PrepItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrepStationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoughPrepRecommendationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedRole = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    QuantityRecommended = table.Column<int>(type: "int", nullable: false),
                    QuantityCompleted = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CompletedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrepTasks", x => x.Id);
                    table.CheckConstraint("CK_PrepTasks_CompletionState", "([Status] <> 'Completed' AND [CompletedAtUtc] IS NULL AND [CompletedByUserId] IS NULL) OR ([Status] = 'Completed' AND [CompletedAtUtc] IS NOT NULL AND [CompletedByUserId] IS NOT NULL AND [QuantityCompleted] > 0)");
                    table.CheckConstraint("CK_PrepTasks_QuantityCompleted_NonNegative", "[QuantityCompleted] >= 0");
                    table.CheckConstraint("CK_PrepTasks_QuantityRecommended_NonNegative", "[QuantityRecommended] >= 0");
                    table.ForeignKey(
                        name: "FK_PrepTasks_AspNetUsers_CompletedByUserId",
                        column: x => x.CompletedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrepTasks_DoughPrepRecommendations_DoughPrepRecommendationId",
                        column: x => x.DoughPrepRecommendationId,
                        principalTable: "DoughPrepRecommendations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrepTasks_PrepItems_PrepItemId",
                        column: x => x.PrepItemId,
                        principalTable: "PrepItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrepTasks_PrepStations_PrepStationId",
                        column: x => x.PrepStationId,
                        principalTable: "PrepStations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrepTasks_AssignedRole",
                table: "PrepTasks",
                column: "AssignedRole");

            migrationBuilder.CreateIndex(
                name: "IX_PrepTasks_CompletedByUserId",
                table: "PrepTasks",
                column: "CompletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PrepTasks_DoughPrepRecommendationId",
                table: "PrepTasks",
                column: "DoughPrepRecommendationId",
                unique: true,
                filter: "[DoughPrepRecommendationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PrepTasks_PrepItemId",
                table: "PrepTasks",
                column: "PrepItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PrepTasks_PrepStationId",
                table: "PrepTasks",
                column: "PrepStationId");

            migrationBuilder.CreateIndex(
                name: "IX_PrepTasks_Status",
                table: "PrepTasks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PrepTasks_TaskDate",
                table: "PrepTasks",
                column: "TaskDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrepTasks");
        }
    }
}
