using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Application.Interfaces.Dough;

public interface IDoughReballRecordRepository
{
    Task AddAsync(DoughReballRecord record, CancellationToken cancellationToken = default);
}
