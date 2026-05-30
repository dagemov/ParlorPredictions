using ParlorPrediction.Contracts.Common;
using ParlorPrediction.Contracts.Requests.Auth;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Application.Interfaces.Auth;

public interface IEmailConfirmationService
{
    Task<ApiResponse<bool>> ConfirmEmailAsync(ConfirmEmailRequest request, CancellationToken cancellationToken = default);

    Task<ApiResponse<string>> SendConfirmationEmailAsync(
        User user,
        string fallbackBaseUrl,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<string>> ResendConfirmationEmailAsync(
        ResendEmailRequest request,
        string fallbackBaseUrl,
        CancellationToken cancellationToken = default);
}
