using System.ComponentModel.DataAnnotations;

namespace ParlorPrediction.Contracts.Requests.Prep;

public sealed class CompletePrepTaskRequest
{
    public Guid PrepTaskId { get; set; }

    [Required]
    public string CompletedByUserId { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int QuantityCompleted { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}
