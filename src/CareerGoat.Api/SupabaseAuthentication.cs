using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

namespace CareerGoat.Api;

public sealed record SupabaseUser(Guid Id, string? Email);

public interface ISupabaseTokenValidator
{
    Task<SupabaseUser?> ValidateAsync(string accessToken, CancellationToken cancellationToken);
}

public sealed class SupabaseTokenValidator(
    HttpClient httpClient,
    IConfiguration configuration,
    IHostEnvironment environment)
    : ISupabaseTokenValidator
{
    public async Task<SupabaseUser?> ValidateAsync(
        string accessToken,
        CancellationToken cancellationToken)
    {
        var supabaseUrl = configuration["Supabase:Url"];
        var anonKey = configuration["Supabase:AnonKey"];
        if (!Uri.TryCreate(supabaseUrl, UriKind.Absolute, out var baseUri)
            || string.IsNullOrWhiteSpace(anonKey)
            || !string.IsNullOrEmpty(baseUri.UserInfo)
            || !string.IsNullOrEmpty(baseUri.Query)
            || !string.IsNullOrEmpty(baseUri.Fragment)
            || baseUri.AbsolutePath != "/"
            || (baseUri.Scheme != Uri.UriSchemeHttps
                && !(environment.IsDevelopment()
                    && baseUri.Scheme == Uri.UriSchemeHttp
                    && baseUri.IsLoopback)))
        {
            throw new InvalidOperationException(
                "Configure Supabase:Url and Supabase:AnonKey to validate access tokens.");
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            new Uri($"{baseUri.ToString().TrimEnd('/')}/auth/v1/user"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("apikey", anonKey);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (!json.RootElement.TryGetProperty("id", out var idElement)
            || !Guid.TryParse(idElement.GetString(), out var id))
        {
            throw new JsonException("Supabase Auth returned a user without a valid id.");
        }

        var email = json.RootElement.TryGetProperty("email", out var emailElement)
            ? emailElement.GetString()
            : null;
        return new SupabaseUser(id, email);
    }
}

public sealed class SupabaseAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ISupabaseTokenValidator tokenValidator)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (!AuthenticationHeaderValue.TryParse(authorization, out var header)
            || !string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(header.Parameter))
        {
            return AuthenticateResult.NoResult();
        }

        var user = await tokenValidator.ValidateAsync(header.Parameter, Context.RequestAborted);
        if (user is null)
        {
            return AuthenticateResult.Fail("The Supabase access token is invalid or expired.");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        };
        if (user.Email is not null)
        {
            claims.Add(new Claim(ClaimTypes.Email, user.Email));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        return AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }
}
