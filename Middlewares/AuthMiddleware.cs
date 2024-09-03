using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;

namespace AuthService.Middlewares
{
    public class AuthorizationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly List<string> _excludedPaths;

        public AuthorizationMiddleware(RequestDelegate next, List<string> excludedPaths)
        {
            _next = next;
            _excludedPaths = excludedPaths;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Check if the request path is in the excluded paths list
            if (_excludedPaths.Any(path => context.Request.Path.StartsWithSegments(path)))
            {
                await _next(context);
                return;
            }

            // Extract the access token from the Authorization header
            var accessToken = context
                .Request
                .Headers["Authorization"]
                .FirstOrDefault()
                ?.Split(" ")
                .Last();

            if (string.IsNullOrEmpty(accessToken))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context
                    .Response
                    .WriteAsJsonAsync(new { message = "Access token is missing." });
                return;
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            JwtSecurityToken? jwtToken;

            try
            {
                jwtToken = tokenHandler.ReadToken(accessToken) as JwtSecurityToken;

                if (jwtToken == null || jwtToken.ValidTo < DateTime.UtcNow)
                {
                    throw new SecurityTokenExpiredException();
                }

                // Extract user roles
                var resourceAccessClaim = jwtToken
                    .Claims
                    .FirstOrDefault(c => c.Type == "resource_access")
                    ?.Value;

                if (string.IsNullOrEmpty(resourceAccessClaim))
                {
                    throw new SecurityTokenException("User roles not found in token.");
                }

                var resourceAccess = JObject.Parse(resourceAccessClaim);

                bool hasRequiredRole = false;
                var realmManagementRoles = resourceAccess["realm-management"]?["roles"]?.ToObject<
                    List<string>
                >();
                var accountRoles = resourceAccess["account"]?["roles"]?.ToObject<List<string>>();

                hasRequiredRole =
                    realmManagementRoles?.Contains("realm-admin") == true
                    || realmManagementRoles?.Contains("manage-users") == true
                    || accountRoles?.Contains("realm-admin") == true
                    || accountRoles?.Contains("manage-users") == true;

                if (!hasRequiredRole)
                {
                    throw new SecurityTokenException("User lacks required roles.");
                }

                // Store the access token in HttpContext.Items
                context.Items["AccessToken"] = accessToken;
            }
            catch (SecurityTokenExpiredException)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context
                    .Response
                    .WriteAsJsonAsync(new { message = "Access token has expired." });
                return;
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { message = ex.Message });
                return;
            }

            // Call the next middleware in the pipeline
            await _next(context);
        }
    }
}
