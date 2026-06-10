using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParlorPrediction.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDoughQualityTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DoughBatchQualityRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    OriginalDoughTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedOrBalledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    QuantityBalls = table.Column<int>(type: "int", nullable: false),
                    CurrentStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    StatusReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AttentionMarkedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReballedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MustUseByDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DiscardedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DiscardReason = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    ManagerNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoughBatchQualityRecords", x => x.Id);
                    table.CheckConstraint("CK_DoughBatchQualityRecords_AttentionState", "([CurrentStatus] <> 'Attention' OR [AttentionMarkedAt] IS NOT NULL)");
                    table.CheckConstraint("CK_DoughBatchQualityRecords_DiscardState", "([CurrentStatus] <> 'Discarded' OR ([DiscardedAt] IS NOT NULL AND [DiscardReason] IS NOT NULL))");
                    table.CheckConstraint("CK_DoughBatchQualityRecords_MustUseState", "([CurrentStatus] <> 'MustUseNextDay' OR [MustUseByDate] IS NOT NULL)");
                    table.CheckConstraint("CK_DoughBatchQualityRecords_QuantityBalls_Positive", "[QuantityBalls] > 0");
                    table.CheckConstraint("CK_DoughBatchQualityRecords_ReballedState", "([CurrentStatus] NOT IN ('Reballed', 'MustUseNextDay') OR [ReballedAt] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_DoughBatchQualityRecords_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DoughBatchQualityRecords_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DoughBatchQualityRecords_PrepTasks_OriginalDoughTaskId",
                        column: x => x.OriginalDoughTaskId,
                        principalTable: "PrepTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DoughLossRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoughBatchQualityRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuantityLostBalls = table.Column<int>(type: "int", nullable: false),
                    LossReason = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    LossDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ManagerNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoughLossRecords", x => x.Id);
                    table.CheckConstraint("CK_DoughLossRecords_QuantityLostBalls_Positive", "[QuantityLostBalls] > 0");
                    table.ForeignKey(
                        name: "FK_DoughLossRecords_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DoughLossRecords_DoughBatchQualityRecords_DoughBatchQualityRecordId",
                        column: x => x.DoughBatchQualityRecordId,
                        principalTable: "DoughBatchQualityRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DoughReballRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoughBatchQualityRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuantityBeforeReball = table.Column<int>(type: "int", nullable: false),
                    QuantityRecoveredBalls = table.Column<int>(type: "int", nullable: false),
                    QuantityLostBalls = table.Column<int>(type: "int", nullable: false),
                    ReballDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Result = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MustUseByDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ManagerNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoughReballRecords", x => x.Id);
                    table.CheckConstraint("CK_DoughReballRecords_LossMath", "[QuantityLostBalls] = [QuantityBeforeReball] - [QuantityRecoveredBalls]");
                    table.CheckConstraint("CK_DoughReballRecords_MustUseByDate", "([Result] <> 'PartialRecovered' OR [MustUseByDate] IS NOT NULL)");
                    table.CheckConstraint("CK_DoughReballRecords_QuantityBeforeReball_Positive", "[QuantityBeforeReball] > 0");
                    table.CheckConstraint("CK_DoughReballRecords_QuantityLostBalls_NonNegative", "[QuantityLostBalls] >= 0");
                    table.CheckConstraint("CK_DoughReballRecords_QuantityRecoveredBalls_NonNegative", "[QuantityRecoveredBalls] >= 0");
                    table.CheckConstraint("CK_DoughReballRecords_RecoveredNotGreaterThanBefore", "[QuantityBeforeReball] >= [QuantityRecoveredBalls]");
                    table.ForeignKey(
                        name: "FK_DoughReballRecords_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DoughReballRecords_DoughBatchQualityRecords_DoughBatchQualityRecordId",
                        column: x => x.DoughBatchQualityRecordId,
                        principalTable: "DoughBatchQualityRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DoughBatchQualityRecords_CreatedByUserId",
                table: "DoughBatchQualityRecords",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DoughBatchQualityRecords_CreatedOrBalledAt",
                table: "DoughBatchQualityRecords",
                column: "CreatedOrBalledAt");

            migrationBuilder.CreateIndex(
                name: "IX_DoughBatchQualityRecords_CurrentStatus",
                table: "DoughBatchQualityRecords",
                column: "CurrentStatus");

            migrationBuilder.CreateIndex(
                name: "IX_DoughBatchQualityRecords_MustUseByDate",
                table: "DoughBatchQualityRecords",
                column: "MustUseByDate");

            migrationBuilder.CreateIndex(
                name: "IX_DoughBatchQualityRecords_OriginalDoughTaskId",
                table: "DoughBatchQualityRecords",
                column: "OriginalDoughTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_DoughBatchQualityRecords_ReballedAt",
                table: "DoughBatchQualityRecords",
                column: "ReballedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DoughBatchQualityRecords_SourceDate",
                table: "DoughBatchQualityRecords",
                column: "SourceDate");

            migrationBuilder.CreateIndex(
                name: "IX_DoughBatchQualityRecords_UpdatedByUserId",
                table: "DoughBatchQualityRecords",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DoughLossRecords_CreatedByUserId",
                table: "DoughLossRecords",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DoughLossRecords_DoughBatchQualityRecordId",
                table: "DoughLossRecords",
                column: "DoughBatchQualityRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_DoughLossRecords_LossDate",
                table: "DoughLossRecords",
                column: "LossDate");

            migrationBuilder.CreateIndex(
                name: "IX_DoughLossRecords_LossReason",
                table: "DoughLossRecords",
                column: "LossReason");

            migrationBuilder.CreateIndex(
                name: "IX_DoughReballRecords_CreatedByUserId",
                table: "DoughReballRecords",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DoughReballRecords_DoughBatchQualityRecordId",
                table: "DoughReballRecords",
                column: "DoughBatchQualityRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_DoughReballRecords_ReballDate",
                table: "DoughReballRecords",
                column: "ReballDate");

            migrationBuilder.CreateIndex(
                name: "IX_DoughReballRecords_Result",
                table: "DoughReballRecords",
                column: "Result");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DoughLossRecords");

            migrationBuilder.DropTable(
                name: "DoughReballRecords");

            migrationBuilder.DropTable(
                name: "DoughBatchQualityRecords");
        }
    }
}
