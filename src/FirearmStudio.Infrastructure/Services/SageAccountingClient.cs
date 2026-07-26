using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Common;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace FirearmStudio.Infrastructure.Services;

public sealed class SageAccountingClient(
    HttpClient httpClient,
    ILogger<SageAccountingClient> logger) : ISageAccountingClient
{
    private static readonly JsonSerializerOptions SageJsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = null,
    };

    public async Task<ErrorOr<SageCompanySummary>> ValidateConnectionAsync(
        SageCredentials credentials,
        CancellationToken cancellationToken)
    {
        try
        {
            var loginResult = await ValidateLoginAsync(credentials, cancellationToken);
            if (loginResult.IsError)
            {
                return loginResult.Errors;
            }

            return await ValidateCompanyAsync(credentials, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Error.Custom(UpstreamErrorTypes.UpstreamFailure, ErrorCodes.Unavailable, "Sage Accounting did not respond in time.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Sage Accounting request failed while validating credentials.");
            return Error.Custom(UpstreamErrorTypes.UpstreamFailure, ErrorCodes.Unavailable, "Sage Accounting could not be reached.");
        }
    }

    private async Task<ErrorOr<Success>> ValidateLoginAsync(
        SageCredentials credentials,
        CancellationToken cancellationToken)
    {
        var path = QueryHelpers.AddQueryString(
            "Login/Validate",
            "apikey",
            credentials.ApiKey);

        using var response = await httpClient.PostAsJsonAsync(
            path,
            new AuthenticationCredentials(credentials.Username, credentials.Password),
            SageJsonOptions,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return MapFailure(response.StatusCode);
        }

        var isValid = await response.Content.ReadFromJsonAsync<bool?>(cancellationToken);
        return isValid is true
            ? Result.Success
            : Error.Validation(ErrorCodes.InvalidCredentials, "Sage rejected the supplied username or password.");
    }

    private async Task<ErrorOr<SageCompanySummary>> ValidateCompanyAsync(
        SageCredentials credentials,
        CancellationToken cancellationToken)
    {
        var path = QueryHelpers.AddQueryString(
            "Company/Get",
            new Dictionary<string, string?>
            {
                ["includeStatus"] = "true",
                ["apikey"] = credentials.ApiKey,
            });

        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = BuildBasicAuthorizationHeader(credentials);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return MapFailure(response.StatusCode);
        }

        var companies = await response.Content.ReadFromJsonAsync<SagePagedResponse<SageCompany>>(
            cancellationToken);

        var company = companies?.Results.FirstOrDefault(c => c.ID == credentials.SageCompanyId);
        if (company is null)
        {
            return Error.Validation(
                ErrorCodes.CompanyNotFound,
                "Sage company ID is not available for the supplied Sage credentials.");
        }

        if (string.IsNullOrWhiteSpace(company.Name))
        {
            return Error.Validation(
                ErrorCodes.CompanyNotFound,
                "Sage returned the company without a valid name.");
        }

        return new SageCompanySummary(company.ID, company.Name);
    }

    private static AuthenticationHeaderValue BuildBasicAuthorizationHeader(SageCredentials credentials)
    {
        var rawCredentials = $"{credentials.Username}:{credentials.Password}";
        var encodedCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(rawCredentials));
        return new AuthenticationHeaderValue("Basic", encodedCredentials);
    }

    private static Error MapFailure(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
            Error.Validation(ErrorCodes.InvalidCredentials, "Sage rejected the supplied credentials."),
        HttpStatusCode.TooManyRequests =>
            Error.Custom(UpstreamErrorTypes.UpstreamFailure, ErrorCodes.RateLimited, "Sage Accounting rate limit was reached. Try again later."),
        _ when statusCode >= HttpStatusCode.InternalServerError =>
            Error.Custom(UpstreamErrorTypes.UpstreamFailure, ErrorCodes.Unavailable, "Sage Accounting is temporarily unavailable."),
        _ =>
            Error.Validation(ErrorCodes.InvalidRequest, "Sage rejected the registration request."),
    };

    private sealed record AuthenticationCredentials(string Username, string Password);

    private sealed record SagePagedResponse<T>(
        int TotalResults,
        int ReturnedResults,
        IReadOnlyList<T> Results);

    private sealed record SageCompany(int ID, string? Name);

    public static class ErrorCodes
    {
        public const string InvalidCredentials = "SageAccountingClient.InvalidCredentials";
        public const string CompanyNotFound = "SageAccountingClient.CompanyNotFound";
        public const string InvalidRequest = "SageAccountingClient.InvalidRequest";
        public const string RateLimited = "SageAccountingClient.RateLimited";
        public const string Unavailable = "SageAccountingClient.Unavailable";
    }
}
