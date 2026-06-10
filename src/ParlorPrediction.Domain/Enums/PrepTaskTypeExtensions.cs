namespace ParlorPrediction.Domain.Enums;

public static class PrepTaskTypeExtensions
{
    public static string Normalize(string? value)
    {
        var normalized = (value?.Trim() ?? string.Empty)
            .Replace("-", string.Empty)
            .Replace("_", string.Empty)
            .Replace(" ", string.Empty);

        return normalized.ToLowerInvariant() switch
        {
            "generic" or "genericdough" or "dough" => nameof(PrepTaskType.GenericDough),
            "makedoughload" or "makeload" or "doughload" or "doughbatch" => nameof(PrepTaskType.MakeDoughLoad),
            "balldough" or "doughballs" or "makeballs" => nameof(PrepTaskType.BallDough),
            _ => value?.Trim() ?? string.Empty
        };
    }

    public static bool TryParse(string? value, out PrepTaskType taskType)
    {
        return Enum.TryParse(Normalize(value), true, out taskType);
    }

    public static string GetDisplayText(this PrepTaskType taskType)
    {
        return taskType switch
        {
            PrepTaskType.GenericDough => "Dough Task",
            PrepTaskType.MakeDoughLoad => "Make Dough Load",
            PrepTaskType.BallDough => "Ball Dough",
            _ => taskType.ToString()
        };
    }
}
