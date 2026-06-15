using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParlorPrediction.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignWeeklyClosingWeekWindow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_WeeklyDoughClosings_WeekWindow",
                table: "WeeklyDoughClosings");

            migrationBuilder.Sql(
                """
                UPDATE [WeeklyDoughClosings]
                SET [WeekStartDate] = DATEADD(day, -1, [WeekStartDate])
                WHERE DATEDIFF(day, [WeekStartDate], [WeekEndDate]) = 5
                  AND (DATEDIFF(day, CONVERT(date, '19000102'), [WeekStartDate]) % 7) = 0
                  AND (DATEDIFF(day, CONVERT(date, '19000107'), [WeekEndDate]) % 7) = 0;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_WeeklyDoughClosings_WeekWindow",
                table: "WeeklyDoughClosings",
                sql: "DATEDIFF(day, [WeekStartDate], [WeekEndDate]) = 6 AND (DATEDIFF(day, CONVERT(date, '19000101'), [WeekStartDate]) % 7) = 0 AND (DATEDIFF(day, CONVERT(date, '19000107'), [WeekEndDate]) % 7) = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_WeeklyDoughClosings_WeekWindow",
                table: "WeeklyDoughClosings");

            migrationBuilder.Sql(
                """
                UPDATE [WeeklyDoughClosings]
                SET [WeekStartDate] = DATEADD(day, 1, [WeekStartDate])
                WHERE DATEDIFF(day, [WeekStartDate], [WeekEndDate]) = 6
                  AND (DATEDIFF(day, CONVERT(date, '19000101'), [WeekStartDate]) % 7) = 0
                  AND (DATEDIFF(day, CONVERT(date, '19000107'), [WeekEndDate]) % 7) = 0;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_WeeklyDoughClosings_WeekWindow",
                table: "WeeklyDoughClosings",
                sql: "DATEDIFF(day, [WeekStartDate], [WeekEndDate]) = 5");
        }
    }
}
