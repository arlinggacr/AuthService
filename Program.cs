using System.IdentityModel.Tokens.Jwt;
using System.Text;
using AuthService.DataContext;
using AuthService.Middlewares;
using AuthService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Clear default inbound claims
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

// Read Keycloak configuration from appsettings.json
var keycloakConfig = builder.Configuration.GetSection("Keycloak");

builder
    .Services
    .AddDbContext<ApplicationDbContext>(
        options => options.UseNpgsql(builder.Configuration.GetConnectionString("LocalConnection"))
    );

// Configure CORS policy
builder
    .Services
    .AddCors(options =>
    {
        options.AddPolicy(
            "AllowSpecificOrigins",
            policyBuilder =>
            {
                policyBuilder
                    .WithOrigins("http://localhost:4433", "http://localhost:8080")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            }
        );
    });

// Configure JWT authentication
builder
    .Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.Authority = $"{keycloakConfig["Url"]}/realms/{keycloakConfig["Realm"]}";
        options.Audience = keycloakConfig["ClientId"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = $"{keycloakConfig["Url"]}/realms/{keycloakConfig["Realm"]}",
            ValidateAudience = true,
            ValidAudience = keycloakConfig["ClientId"],
            ValidateLifetime = true
        };

        options.RequireHttpsMetadata = false;
    });

// Configure the Authorization
// builder
//     .Services
//     .AddAuthorizationBuilder()
//     // Configure the Authorization
//     .AddPolicy("AdminPolicy", roles => roles.RequireRole("realm_admin"));

// // Configure the Authorization
// .AddPolicy("UserPolicy", roles => roles.RequireRole("user"));

// Configure HTTP client for KeycloakService
builder
    .Services
    .AddHttpClient<KeycloakService>(client =>
    {
        var keycloakUrl = builder.Configuration["Keycloak:Url"];
        if (string.IsNullOrEmpty(keycloakUrl))
        {
            throw new ArgumentNullException("Keycloak URL is missing in configuration.");
        }
        client.BaseAddress = new Uri(keycloakUrl);
    });
builder.Services.AddSingleton<OtpService>();

// Register KeycloakService with DI container
var keycloakUrl =
    keycloakConfig["Url"]
    ?? throw new ArgumentNullException(
        nameof(keycloakConfig),
        "Keycloak URL is missing in configuration."
    );
var realm =
    keycloakConfig["Realm"]
    ?? throw new ArgumentNullException(
        nameof(keycloakConfig),
        "Keycloak realm is missing in configuration."
    );
var adminUsername =
    keycloakConfig["AdminUsername"]
    ?? throw new ArgumentNullException(
        nameof(keycloakConfig),
        "Keycloak admin username is missing in configuration."
    );
var adminPassword =
    keycloakConfig["AdminPassword"]
    ?? throw new ArgumentNullException(
        nameof(keycloakConfig),
        "Keycloak admin password is missing in configuration."
    );

builder
    .Services
    .AddSingleton(
        new KeycloakService(
            keycloakUrl: keycloakUrl,
            realm: realm,
            adminUsername: adminUsername,
            adminPassword: adminPassword
        )
    );

builder.Services.AddControllers().AddNewtonsoftJson();

var app = builder.Build();

var excludedPaths = new List<string> { "/api/login", "/api/register" };

app.UseMiddleware<AuthorizationMiddleware>(excludedPaths);

app.UseRouting();

// Apply the CORS policy
app.UseCors("AllowSpecificOrigins");

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
