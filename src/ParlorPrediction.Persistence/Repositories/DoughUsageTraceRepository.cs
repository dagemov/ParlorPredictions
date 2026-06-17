using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;

namespace ParlorPrediction.Persistence.Repositories;

public sealed class DoughUsageTraceRepository : IDoughUsageTraceRepository
{
    private readonly ParlorPredictionDbContext _dbContext;

    public DoughUsageTraceRepository(ParlorPredictionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(DoughUsageTrace trace, CancellationToken cancellationToken = default)
    {
        await _dbContext.DoughUsageTraces.AddAsync(trace, cancellationToken);
    }

    public Task<DoughUsageTrace?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return QuerySingleAsync(
            "WHERE [Id] = @Id",
            command => command.Parameters.Add(new SqlParameter("@Id", id)),
            cancellationToken);
    }

    public async Task<IReadOnlyList<DoughUsageTrace>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await QueryAsync(string.Empty, null, cancellationToken);
    }

    public async Task<IReadOnlyList<DoughUsageTrace>> SearchAsync(
        DateOnly? usageDateFrom,
        DateOnly? usageDateTo,
        Guid? sourceDoughBatchQualityRecordId,
        CancellationToken cancellationToken = default)
    {
        var filters = new List<string>();
        var parameters = new List<SqlParameter>();

        if (usageDateFrom.HasValue)
        {
            filters.Add("[UsageDate] >= @UsageDateFrom");
            parameters.Add(new SqlParameter("@UsageDateFrom", usageDateFrom.Value.ToDateTime(TimeOnly.MinValue)));
        }

        if (usageDateTo.HasValue)
        {
            filters.Add("[UsageDate] <= @UsageDateTo");
            parameters.Add(new SqlParameter("@UsageDateTo", usageDateTo.Value.ToDateTime(TimeOnly.MinValue)));
        }

        if (sourceDoughBatchQualityRecordId.HasValue)
        {
            filters.Add("[SourceDoughBatchQualityRecordId] = @SourceDoughBatchQualityRecordId");
            parameters.Add(new SqlParameter("@SourceDoughBatchQualityRecordId", sourceDoughBatchQualityRecordId.Value));
        }

        var whereClause = filters.Count == 0
            ? string.Empty
            : $"WHERE {string.Join(" AND ", filters)}";

        return await QueryAsync(
            whereClause,
            command =>
            {
                foreach (var parameter in parameters)
                {
                    command.Parameters.Add(parameter);
                }
            },
            cancellationToken);
    }

    public void Update(DoughUsageTrace trace)
    {
        _dbContext.DoughUsageTraces.Update(trace);
    }

    public void Remove(DoughUsageTrace trace)
    {
        _dbContext.DoughUsageTraces.Remove(trace);
    }

    private async Task<DoughUsageTrace?> QuerySingleAsync(
        string whereClause,
        Action<SqlCommand> configureCommand,
        CancellationToken cancellationToken)
    {
        var items = await QueryAsync(whereClause, configureCommand, cancellationToken);
        return items.SingleOrDefault();
    }

    private async Task<IReadOnlyList<DoughUsageTrace>> QueryAsync(
        string whereClause,
        Action<SqlCommand>? configureCommand,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_dbContext.Database.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                [Id],
                [UsageDate],
                [SourceDoughBatchQualityRecordId],
                [SourceDate],
                [SourceType],
                [Destination],
                CAST([TrayCount] AS decimal(5,2)) AS [TrayCount],
                [BallsPerTray],
                [BallsUsed],
                [Notes],
                [CreatedByUserId],
                [UpdatedByUserId],
                [CreatedAtUtc],
                [UpdatedAtUtc]
            FROM [DoughUsageTraces]
            """ +
            (string.IsNullOrWhiteSpace(whereClause) ? string.Empty : Environment.NewLine + whereClause) +
            Environment.NewLine +
            """
            ORDER BY [UsageDate] DESC, [CreatedAtUtc] DESC
            """;

        configureCommand?.Invoke(command);

        var traces = new List<DoughUsageTrace>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            traces.Add(DoughUsageTrace.Rehydrate(
                reader.GetGuid(reader.GetOrdinal("Id")),
                DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("UsageDate"))),
                reader.GetGuid(reader.GetOrdinal("SourceDoughBatchQualityRecordId")),
                DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("SourceDate"))),
                Enum.Parse<DoughQualityStatus>(reader.GetString(reader.GetOrdinal("SourceType")), ignoreCase: true),
                Enum.Parse<DoughUsageDestination>(reader.GetString(reader.GetOrdinal("Destination")), ignoreCase: true),
                reader.GetDecimal(reader.GetOrdinal("TrayCount")),
                reader.GetInt32(reader.GetOrdinal("BallsPerTray")),
                reader.GetInt32(reader.GetOrdinal("BallsUsed")),
                reader.GetString(reader.GetOrdinal("CreatedByUserId")),
                reader.GetString(reader.GetOrdinal("UpdatedByUserId")),
                reader.GetDateTime(reader.GetOrdinal("CreatedAtUtc")),
                reader.GetDateTime(reader.GetOrdinal("UpdatedAtUtc")),
                reader.IsDBNull(reader.GetOrdinal("Notes"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("Notes"))));
        }

        return traces;
    }
}
