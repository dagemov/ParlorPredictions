using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParlorPrediction.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationalDraftAuditPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OperationalAuditEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ActorUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    SourceText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NormalizedIntentJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BeforeSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AfterPreviewJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ValidationWarningsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DraftId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TimestampUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationalAuditEntries", x => x.Id);
                    table.CheckConstraint("CK_OperationalAuditEntries_ActionType_NotEmpty", "LEN(LTRIM(RTRIM([ActionType]))) > 0");
                    table.CheckConstraint("CK_OperationalAuditEntries_ActorUserId_NotEmpty", "LEN(LTRIM(RTRIM([ActorUserId]))) > 0");
                    table.CheckConstraint("CK_OperationalAuditEntries_SourceText_NotEmpty", "LEN(LTRIM(RTRIM([SourceText]))) > 0");
                });

            migrationBuilder.CreateTable(
                name: "OperationalDrafts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DraftType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SourceText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NormalizedIntentJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BeforeSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AfterPreviewJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ValidationWarningsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DraftPayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false, defaultValue: "Pending"),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ReviewedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StatusReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ApprovedEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationalDrafts", x => x.Id);
                    table.CheckConstraint("CK_OperationalDrafts_CreatedBy_NotEmpty", "LEN(LTRIM(RTRIM([CreatedBy]))) > 0");
                    table.CheckConstraint("CK_OperationalDrafts_DraftType_NotEmpty", "LEN(LTRIM(RTRIM([DraftType]))) > 0");
                    table.CheckConstraint("CK_OperationalDrafts_SourceText_NotEmpty", "LEN(LTRIM(RTRIM([SourceText]))) > 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_OperationalAuditEntries_CorrelationId",
                table: "OperationalAuditEntries",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_OperationalAuditEntries_DraftId",
                table: "OperationalAuditEntries",
                column: "DraftId");

            migrationBuilder.CreateIndex(
                name: "IX_OperationalAuditEntries_TimestampUtc",
                table: "OperationalAuditEntries",
                column: "TimestampUtc");

            migrationBuilder.CreateIndex(
                name: "IX_OperationalDrafts_CorrelationId",
                table: "OperationalDrafts",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_OperationalDrafts_CreatedAtUtc",
                table: "OperationalDrafts",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_OperationalDrafts_Status",
                table: "OperationalDrafts",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OperationalAuditEntries");

            migrationBuilder.DropTable(
                name: "OperationalDrafts");
        }
    }
}
