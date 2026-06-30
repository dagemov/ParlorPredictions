using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParlorPrediction.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AllowFractionalDoughUsageTraces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE [DoughUsageTraces] DROP CONSTRAINT [CK_DoughUsageTraces_BallsUsed_MatchesTrays];
                ALTER TABLE [DoughUsageTraces] DROP CONSTRAINT [CK_DoughUsageTraces_TrayCount_Positive];
                """);

            migrationBuilder.AlterColumn<decimal>(
                name: "TrayCount",
                table: "DoughUsageTraces",
                type: "decimal(5,2)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.Sql("""
                ALTER TABLE [DoughUsageTraces]
                ADD CONSTRAINT [CK_DoughUsageTraces_TrayCount_Positive] CHECK ([TrayCount] > 0);

                ALTER TABLE [DoughUsageTraces]
                ADD CONSTRAINT [CK_DoughUsageTraces_BallsUsed_MatchesTrays] CHECK ([BallsUsed] = ([TrayCount] * [BallsPerTray]));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE [DoughUsageTraces] DROP CONSTRAINT [CK_DoughUsageTraces_BallsUsed_MatchesTrays];
                ALTER TABLE [DoughUsageTraces] DROP CONSTRAINT [CK_DoughUsageTraces_TrayCount_Positive];
                """);

            migrationBuilder.AlterColumn<int>(
                name: "TrayCount",
                table: "DoughUsageTraces",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)");

            migrationBuilder.Sql("""
                ALTER TABLE [DoughUsageTraces]
                ADD CONSTRAINT [CK_DoughUsageTraces_TrayCount_Positive] CHECK ([TrayCount] > 0);

                ALTER TABLE [DoughUsageTraces]
                ADD CONSTRAINT [CK_DoughUsageTraces_BallsUsed_MatchesTrays] CHECK ([BallsUsed] = ([TrayCount] * [BallsPerTray]));
                """);
        }
    }
}
