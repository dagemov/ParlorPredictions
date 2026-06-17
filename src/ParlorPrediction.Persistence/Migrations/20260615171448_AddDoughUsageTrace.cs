using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParlorPrediction.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDoughUsageTrace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DoughUsageTraces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsageDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SourceDoughBatchQualityRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Destination = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TrayCount = table.Column<int>(type: "int", nullable: false),
                    BallsPerTray = table.Column<int>(type: "int", nullable: false),
                    BallsUsed = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoughUsageTraces", x => x.Id);
                    table.CheckConstraint("CK_DoughUsageTraces_BallsPerTray_Positive", "[BallsPerTray] > 0");
                    table.CheckConstraint("CK_DoughUsageTraces_BallsUsed_MatchesTrays", "[BallsUsed] = ([TrayCount] * [BallsPerTray])");
                    table.CheckConstraint("CK_DoughUsageTraces_SourceType_Allowed", "[SourceType] <> 'Discarded'");
                    table.CheckConstraint("CK_DoughUsageTraces_TrayCount_Positive", "[TrayCount] > 0");
                    table.ForeignKey(
                        name: "FK_DoughUsageTraces_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DoughUsageTraces_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DoughUsageTraces_DoughBatchQualityRecords_SourceDoughBatchQualityRecordId",
                        column: x => x.SourceDoughBatchQualityRecordId,
                        principalTable: "DoughBatchQualityRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DoughUsageTraces_CreatedByUserId",
                table: "DoughUsageTraces",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DoughUsageTraces_Destination",
                table: "DoughUsageTraces",
                column: "Destination");

            migrationBuilder.CreateIndex(
                name: "IX_DoughUsageTraces_SourceDate",
                table: "DoughUsageTraces",
                column: "SourceDate");

            migrationBuilder.CreateIndex(
                name: "IX_DoughUsageTraces_SourceDoughBatchQualityRecordId",
                table: "DoughUsageTraces",
                column: "SourceDoughBatchQualityRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_DoughUsageTraces_UpdatedByUserId",
                table: "DoughUsageTraces",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DoughUsageTraces_UsageDate",
                table: "DoughUsageTraces",
                column: "UsageDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DoughUsageTraces");
        }
    }
}
