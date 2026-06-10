using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParlorPrediction.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPrepTaskLoadBallWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "QuantityUnit",
                table: "PrepTasks",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Balls");

            migrationBuilder.AddColumn<Guid>(
                name: "SourceDoughBatchId",
                table: "PrepTasks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourcePrepTaskId",
                table: "PrepTasks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaskType",
                table: "PrepTasks",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "GenericDough");

            migrationBuilder.Sql(
                "UPDATE [PrepTasks] SET [TaskType] = 'GenericDough', [QuantityUnit] = 'Balls' WHERE [TaskType] = '' OR [QuantityUnit] = '';");

            migrationBuilder.CreateIndex(
                name: "IX_PrepTasks_SourceDoughBatchId",
                table: "PrepTasks",
                column: "SourceDoughBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_PrepTasks_SourcePrepTaskId",
                table: "PrepTasks",
                column: "SourcePrepTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_PrepTasks_TaskType",
                table: "PrepTasks",
                column: "TaskType");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PrepTasks_TaskTypeUnit",
                table: "PrepTasks",
                sql: "([TaskType] = 'GenericDough' AND [QuantityUnit] = 'Balls') OR ([TaskType] = 'MakeDoughLoad' AND [QuantityUnit] = 'FullLoads') OR ([TaskType] = 'BallDough' AND [QuantityUnit] = 'Balls')");

            migrationBuilder.AddForeignKey(
                name: "FK_PrepTasks_DoughBatches_SourceDoughBatchId",
                table: "PrepTasks",
                column: "SourceDoughBatchId",
                principalTable: "DoughBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PrepTasks_PrepTasks_SourcePrepTaskId",
                table: "PrepTasks",
                column: "SourcePrepTaskId",
                principalTable: "PrepTasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PrepTasks_DoughBatches_SourceDoughBatchId",
                table: "PrepTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_PrepTasks_PrepTasks_SourcePrepTaskId",
                table: "PrepTasks");

            migrationBuilder.DropIndex(
                name: "IX_PrepTasks_SourceDoughBatchId",
                table: "PrepTasks");

            migrationBuilder.DropIndex(
                name: "IX_PrepTasks_SourcePrepTaskId",
                table: "PrepTasks");

            migrationBuilder.DropIndex(
                name: "IX_PrepTasks_TaskType",
                table: "PrepTasks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PrepTasks_TaskTypeUnit",
                table: "PrepTasks");

            migrationBuilder.DropColumn(
                name: "QuantityUnit",
                table: "PrepTasks");

            migrationBuilder.DropColumn(
                name: "SourceDoughBatchId",
                table: "PrepTasks");

            migrationBuilder.DropColumn(
                name: "SourcePrepTaskId",
                table: "PrepTasks");

            migrationBuilder.DropColumn(
                name: "TaskType",
                table: "PrepTasks");
        }
    }
}
