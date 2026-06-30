using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Persistence.Repositories;

internal static class DoughBatchSqlCompatibility
{
    public static async Task<DoughBatch?> GetByIdTrackedAsync(
        ParlorPredictionDbContext dbContext,
        Guid id,
        CancellationToken cancellationToken)
    {
        var trackedBatch = dbContext.DoughBatches.Local.FirstOrDefault(batch => batch.Id == id);
        if (trackedBatch is not null)
        {
            return trackedBatch;
        }

        var batches = await QueryAsync(
            dbContext,
            filters: ["[Id] = @Id"],
            configureCommand: command => command.Parameters.Add(new SqlParameter("@Id", id)),
            orderByClause: "ORDER BY [CreatedAtUtc] DESC",
            cancellationToken);

        var batch = batches.SingleOrDefault();
        return batch is null
            ? null
            : dbContext.Attach(batch).Entity;
    }

    public static Task<IReadOnlyCollection<DoughBatch>> GetProducedOnOrBeforeAsync(
        ParlorPredictionDbContext dbContext,
        DateOnly productionDate,
        CancellationToken cancellationToken)
    {
        return QueryAsync(
            dbContext,
            filters: ["[BatchDate] <= @ProductionDate"],
            configureCommand: command => command.Parameters.Add(new SqlParameter("@ProductionDate", productionDate.ToDateTime(TimeOnly.MinValue))),
            orderByClause: "ORDER BY [BatchDate], [FermentationReadyDate]",
            cancellationToken);
    }

    public static Task<IReadOnlyCollection<DoughBatch>> SearchForCorrectionAsync(
        ParlorPredictionDbContext dbContext,
        DateOnly? batchDateFrom,
        DateOnly? batchDateTo,
        bool includeVoided,
        CancellationToken cancellationToken)
    {
        var filters = new List<string>();
        var parameters = new List<SqlParameter>();

        if (batchDateFrom.HasValue)
        {
            filters.Add("[BatchDate] >= @BatchDateFrom");
            parameters.Add(new SqlParameter("@BatchDateFrom", batchDateFrom.Value.ToDateTime(TimeOnly.MinValue)));
        }

        if (batchDateTo.HasValue)
        {
            filters.Add("[BatchDate] <= @BatchDateTo");
            parameters.Add(new SqlParameter("@BatchDateTo", batchDateTo.Value.ToDateTime(TimeOnly.MinValue)));
        }

        return QueryAsync(
            dbContext,
            filters,
            command =>
            {
                foreach (var parameter in parameters)
                {
                    command.Parameters.Add(parameter);
                }
            },
            "ORDER BY [BatchDate] DESC, [CreatedAtUtc] DESC",
            cancellationToken,
            includeVoided);
    }

    private static async Task<IReadOnlyCollection<DoughBatch>> QueryAsync(
        ParlorPredictionDbContext dbContext,
        IReadOnlyCollection<string> filters,
        Action<SqlCommand>? configureCommand,
        string orderByClause,
        CancellationToken cancellationToken,
        bool includeVoided = false)
    {
        await using var connection = new SqlConnection(dbContext.Database.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        var schema = await LoadSchemaAsync(connection, cancellationToken);
        var whereFilters = filters.ToList();

        if (!includeVoided && schema.HasIsVoided)
        {
            whereFilters.Add("[IsVoided] = 0");
        }

        var whereClause = whereFilters.Count == 0
            ? string.Empty
            : $"WHERE {string.Join(" AND ", whereFilters)}";

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                [Id],
                [BatchDate],
                [TotalCases],
                [BallsPerCase],
                [TotalBalls],
                [FermentationReadyDate],
                [IsBalled],
                [BalledAtUtc],
                [IsEventException],
                [Notes],
            """ +
            Environment.NewLine +
            $"{SelectExpression(schema.HasIsVoided, "[IsVoided]", "CAST(0 AS bit)")}," +
            Environment.NewLine +
            $"{SelectExpression(schema.HasVoidedAtUtc, "[VoidedAtUtc]", "CAST(NULL AS datetime2)")}," +
            Environment.NewLine +
            $"{SelectExpression(schema.HasVoidReason, "[VoidReason]", "CAST(NULL AS nvarchar(500))")}," +
            Environment.NewLine +
            """
                [CreatedAtUtc],
                [UpdatedAtUtc]
            FROM [DoughBatches]
            """ +
            (string.IsNullOrWhiteSpace(whereClause) ? string.Empty : Environment.NewLine + whereClause) +
            Environment.NewLine +
            orderByClause;

        configureCommand?.Invoke(command);

        var batches = new List<DoughBatch>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            batches.Add(DoughBatch.Rehydrate(
                reader.GetGuid(reader.GetOrdinal("Id")),
                DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("BatchDate"))),
                reader.GetInt32(reader.GetOrdinal("TotalCases")),
                reader.GetInt32(reader.GetOrdinal("BallsPerCase")),
                reader.GetInt32(reader.GetOrdinal("TotalBalls")),
                DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("FermentationReadyDate"))),
                reader.GetBoolean(reader.GetOrdinal("IsBalled")),
                reader.IsDBNull(reader.GetOrdinal("BalledAtUtc"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("BalledAtUtc")),
                reader.GetBoolean(reader.GetOrdinal("IsEventException")),
                reader.GetBoolean(reader.GetOrdinal("IsVoided")),
                reader.IsDBNull(reader.GetOrdinal("VoidedAtUtc"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("VoidedAtUtc")),
                reader.IsDBNull(reader.GetOrdinal("Notes"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("Notes")),
                reader.IsDBNull(reader.GetOrdinal("VoidReason"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("VoidReason")),
                reader.GetDateTime(reader.GetOrdinal("CreatedAtUtc")),
                reader.GetDateTime(reader.GetOrdinal("UpdatedAtUtc"))));
        }

        return batches;
    }

    private static async Task<DoughBatchSchema> LoadSchemaAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT [COLUMN_NAME]
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE [TABLE_NAME] = 'DoughBatches'
            """;

        var columnNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            columnNames.Add(reader.GetString(0));
        }

        return new DoughBatchSchema(
            columnNames.Contains("IsVoided"),
            columnNames.Contains("VoidedAtUtc"),
            columnNames.Contains("VoidReason"));
    }

    private static string SelectExpression(bool hasColumn, string columnName, string fallbackExpression)
    {
        var expression = hasColumn ? columnName : fallbackExpression;
        return $"{expression} AS {columnName}";
    }

    private sealed record DoughBatchSchema(
        bool HasIsVoided,
        bool HasVoidedAtUtc,
        bool HasVoidReason);
}
