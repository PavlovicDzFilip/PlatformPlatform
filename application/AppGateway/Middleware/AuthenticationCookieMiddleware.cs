using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using PlatformPlatform.SharedKernel.ApiCore;
using PlatformPlatform.SharedKernel.ApplicationCore.Authentication;

namespace PlatformPlatform.AppGateway.Middleware;

public class AuthenticationCookieMiddleware(
    AuthenticationTokenSettings authenticationTokenSettings,
    IHttpClientFactory httpClientFactory,
    ILogger<AuthenticationCookieMiddleware> logger
)
    : IMiddleware
{
    private const string? RefreshAuthenticationTokensEndpoint = "/api/account-management/authentication/refresh-authentication-tokens";

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.Request.Cookies.TryGetValue(AuthenticationTokenSettings.RefreshTokenCookieName, out var refreshTokenCookieValue))
        {
            context.Request.Cookies.TryGetValue(AuthenticationTokenSettings.AccessTokenCookieName, out var accessTokenCookieValue);
            await ValidateAuthenticationCookieAndConvertToHttpBearerHeader(context, refreshTokenCookieValue, accessTokenCookieValue);
        }

        await next(context);

        if (context.Response.Headers.TryGetValue(AuthenticationTokenSettings.RefreshTokenHttpHeaderKey, out var refreshToken) &&
            context.Response.Headers.TryGetValue(AuthenticationTokenSettings.AccessTokenHttpHeaderKey, out var accessToken))
        {
            ReplaceAuthenticationHeaderWithCookie(context, refreshToken.Single()!, accessToken.Single()!);
        }
    }

    private async Task ValidateAuthenticationCookieAndConvertToHttpBearerHeader(HttpContext context, string refreshToken, string? accessToken)
    {
        if (context.Request.Headers.ContainsKey(AuthenticationTokenSettings.RefreshTokenHttpHeaderKey) ||
            context.Request.Headers.ContainsKey(AuthenticationTokenSettings.AccessTokenHttpHeaderKey))
        {
            // The authentication token cookies is used by WebApp, but API requests should use tokens in the headers
            throw new InvalidOperationException("A request cannot contain both an authentication token cookies and security tokens in the headers.");
        }

        try
        {
            if (accessToken is null || ExtractExpirationFromToken(accessToken) < TimeProvider.System.GetUtcNow())
            {
                if (ExtractExpirationFromToken(refreshToken) < TimeProvider.System.GetUtcNow())
                {
                    context.Response.Cookies.Delete(AuthenticationTokenSettings.RefreshTokenCookieName);
                    context.Response.Cookies.Delete(AuthenticationTokenSettings.AccessTokenCookieName);
                    logger.LogDebug("The refresh-token has expired. The authentication token cookies are removed.");
                    return;
                }

                (refreshToken, accessToken) = await RefreshAuthenticationTokensAsync(refreshToken);

                // Update the authentication token cookies with the new tokens
                ReplaceAuthenticationHeaderWithCookie(context, refreshToken, accessToken);
            }

            context.Request.Headers["Authorization"] = $"Bearer {accessToken}";

            context.Request.Headers.Authorization = context.Request.Path.Value == RefreshAuthenticationTokensEndpoint
                ? $"Bearer {refreshToken}" // When calling the refresh endpoint, use the refresh token as Bearer
                : $"Bearer {accessToken}";
        }
        catch (SecurityTokenException ex)
        {
            context.Response.Cookies.Delete(AuthenticationTokenSettings.RefreshTokenCookieName);
            context.Response.Cookies.Delete(AuthenticationTokenSettings.AccessTokenCookieName);
            logger.LogWarning(ex, "Validating or refreshing the authentication token cookies failed. {Message}", ex.Message);
        }
    }

    private async Task<(string newRefreshToken, string newAccessToken)> RefreshAuthenticationTokensAsync(string refreshToken)
    {
        logger.LogDebug("The access-token has expired, attempting to refresh...");

        var request = new HttpRequestMessage(HttpMethod.Post, RefreshAuthenticationTokensEndpoint);

        // Use refresh Token as Bearer when refreshing Access Token
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshToken);

        var accountManagementHttpClient = httpClientFactory.CreateClient("AccountManagement");
        var response = await accountManagementHttpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            throw new SecurityTokenException($"Failed to refresh security tokens. Response status code: {response.StatusCode}");
        }

        var newRefreshToken = response.Headers.GetValues(AuthenticationTokenSettings.RefreshTokenHttpHeaderKey).SingleOrDefault();
        var newAccessToken = response.Headers.GetValues(AuthenticationTokenSettings.AccessTokenHttpHeaderKey).SingleOrDefault();

        if (newRefreshToken is null || newAccessToken is null)
        {
            throw new SecurityTokenException("Failed to get refreshed security tokens from the response.");
        }

        return (newRefreshToken, newAccessToken);
    }

    private void ReplaceAuthenticationHeaderWithCookie(HttpContext context, string refreshToken, string accessToken)
    {
        var refreshTokenExpires = ExtractExpirationFromToken(refreshToken);

        // The refresh token cookie is SameSiteMode.Lax, which makes the cookie available on the first request when redirected
        // from another site. This means we can redirect to the login page if the user is not authenticated without
        // having to first serve the SPA. This is only secure if iFrames are not allowed to host the site.
        var refreshTokenCookieOptions = new CookieOptions
        {
            HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax, Expires = refreshTokenExpires
        };
        context.Response.Cookies.Append(AuthenticationTokenSettings.RefreshTokenCookieName, refreshToken, refreshTokenCookieOptions);

        var accessTokenCookieOptions = new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict };
        context.Response.Cookies.Append(AuthenticationTokenSettings.AccessTokenCookieName, accessToken, accessTokenCookieOptions);

        context.Response.Headers.Remove(AuthenticationTokenSettings.RefreshTokenHttpHeaderKey);
        context.Response.Headers.Remove(AuthenticationTokenSettings.AccessTokenHttpHeaderKey);
    }

    private DateTimeOffset ExtractExpirationFromToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        if (!tokenHandler.CanReadToken(token))
        {
            throw new SecurityTokenMalformedException("The token is not a valid JWT.");
        }

        var validationParameters = ApiCoreConfiguration.GetTokenValidationParameters(
            authenticationTokenSettings,
            validateLifetime: false, // We validate the lifetime manually
            clockSkew: TimeSpan.FromSeconds(2) // In Azure, we don't need any clock skew, but this must be a lower value than in downstream APIs
        );

        // This will throw if the token is invalid
        var tokenClaims = tokenHandler.ValidateToken(token, validationParameters, out _);

        // The 'exp' claim is the number of seconds since Unix epoch (00:00:00 UTC on 1st January 1970)
        var expires = tokenClaims.FindFirstValue(JwtRegisteredClaimNames.Exp)!;

        return DateTimeOffset.FromUnixTimeSeconds(long.Parse(expires));
    }
}
