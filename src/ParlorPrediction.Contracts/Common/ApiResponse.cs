using System.Net;

namespace ParlorPrediction.Contracts.Common;

public sealed class ApiResponse<T>
{
    public bool IsSuccessful { get; init; }

    public HttpStatusCode StatusCode { get; init; }

    public string Message { get; init; } = string.Empty;

    public T? Result { get; init; }

    public IReadOnlyCollection<string> Errors { get; init; } = Array.Empty<string>();

    public static ApiResponse<T> Success(
        T? result,
        string message,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new ApiResponse<T>
        {
            IsSuccessful = true,
            StatusCode = statusCode,
            Message = message,
            Result = result
        };
    }

    public static ApiResponse<T> Failure(
        string message,
        HttpStatusCode statusCode,
        params string[] errors)
    {
        return new ApiResponse<T>
        {
            IsSuccessful = false,
            StatusCode = statusCode,
            Message = message,
            Errors = errors.Length == 0 ? Array.Empty<string>() : errors
        };
    }
}
