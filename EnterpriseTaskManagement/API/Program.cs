using System.Reflection;
using System.Security.Claims;
using System.Text;

using EnterpriseTaskManagement.API.Middleware;
using EnterpriseTaskManagement.Application;
using EnterpriseTaskManagement.Infrastructure;
using EnterpriseTaskManagement.Infrastructure.Authentication;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Composition root. Each layer exposes exactly one registration entry point,
// so the API never touches EF Core, MediatR or FluentValidation directly.
// ---------------------------------------------------------------------------
builder.Services.AddControllers();

builder.Services.AddApplication();                       // MediatR + FluentValidation + validation pipeline
builder.Services.AddInfrastructure(builder.Configuration); // EF Core 10 + SQL Server + repositories + auth services

// ---------------------------------------------------------------------------
// JWT bearer authentication. Issuer, audience, signature and lifetime are all
// validated; unsigned tokens are never accepted. The signing key comes from
// configuration only (User Secrets or the Jwt__SecretKey environment variable).
// ---------------------------------------------------------------------------
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException(
        "Missing 'Jwt' configuration section (Issuer, Audience, SecretKey, ExpirationMinutes).");

if (string.IsNullOrWhiteSpace(jwtSettings.SecretKey)
    || Encoding.UTF8.GetByteCount(jwtSettings.SecretKey) < JwtSettings.MinimumSecretKeyBytes)
{
    throw new InvalidOperationException(
        "Missing or too short 'Jwt:SecretKey' (minimum 32 bytes). Configure it with " +
        "'dotnet user-secrets set \"Jwt:SecretKey\" \"<random-value>\"' or the " +
        "Jwt__SecretKey environment variable. It must never be committed.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ValidateLifetime = true,
            RequireSignedTokens = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            // MapInboundClaims is off, so the role claim type must be stated
            // explicitly for [Authorize(Roles = "...")] to resolve it.
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = "name",
        };
    });

builder.Services.AddAuthorization();

// ---------------------------------------------------------------------------
// CORS. The React dev server runs on a different origin than the API, so the
// browser needs an explicit allow-list. Origins come from configuration only
// (Cors:AllowedOrigins) — there is no AllowAnyOrigin, and credentials are not
// allowed because the frontend sends the JWT in the Authorization header.
// ---------------------------------------------------------------------------
const string frontendCorsPolicy = "FrontendDevOrigins";
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
    options.AddPolicy(frontendCorsPolicy, policy =>
    {
        if (allowedOrigins.Length == 0)
        {
            return; // No origins configured: cross-origin calls stay blocked.
        }

        policy.WithOrigins(allowedOrigins)
            .WithHeaders("Authorization", "Content-Type")
            .WithMethods("GET", "POST", "PUT", "DELETE");
    }));

// Single global exception -> RFC 7807 ProblemDetails translator.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Swagger / OpenAPI — configured once.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Enterprise Task Management API",
        Version = "v1",
        Description = "Clean Architecture + DDD + CQRS API backed by EF Core 10 and SQL Server.",
    });

    // Bearer token workflow so protected endpoints can be exercised from Swagger UI.
    const string securitySchemeId = "Bearer";
    options.AddSecurityDefinition(securitySchemeId, new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the accessToken returned by POST /api/Auth/login.",
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = securitySchemeId,
                },
            },
            Array.Empty<string>()
        },
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }
});

var app = builder.Build();

// ---------------------------------------------------------------------------
// Middleware order: exception handling first so everything below is covered,
// then Swagger (Development only), then HTTPS, routing and endpoints.
// ---------------------------------------------------------------------------
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Enterprise Task Management API v1");
        options.RoutePrefix = "swagger"; // browse http://localhost:5000/swagger
    });
}

app.UseHttpsRedirection();

app.UseCors(frontendCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
