using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Application.Tests;

internal sealed class InMemoryDoughUsageTraceRepository : IDoughUsageTraceRepository
{
    public List<DoughUsageTrace> Items { get; } = [];

    public Task AddAsync(DoughUsageTrace trace, CancellationToken cancellationToken = default)
    {
        Items.Add(trace);
        return Task.CompletedTask;
    }

    public Task<DoughUsageTrace?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<DoughUsageTrace?>(Items.FirstOrDefault(trace => trace.Id == id));
    }

    public Task<IReadOnlyList<DoughUsageTrace>> ListAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<DoughUsageTrace>>(Items.ToArray());
    }

    public Task<IReadOnlyList<DoughUsageTrace>> SearchAsync(
        DateOnly? usageDateFrom,
        DateOnly? usageDateTo,
        Guid? sourceDoughBatchQualityRecordId,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<DoughUsageTrace> query = Items;

        if (usageDateFrom.HasValue)
        {
            query = query.Where(trace => trace.UsageDate >= usageDateFrom.Value);
        }

        if (usageDateTo.HasValue)
        {
            query = query.Where(trace => trace.UsageDate <= usageDateTo.Value);
        }

        if (sourceDoughBatchQualityRecordId.HasValue)
        {
            query = query.Where(trace => trace.SourceDoughBatchQualityRecordId == sourceDoughBatchQualityRecordId.Value);
        }

        return Task.FromResult<IReadOnlyList<DoughUsageTrace>>(
            query
                .OrderByDescending(trace => trace.UsageDate)
                .ThenByDescending(trace => trace.CreatedAtUtc)
                .ToArray());
    }

    public void Remove(DoughUsageTrace trace)
    {
        Items.Remove(trace);
    }
}
