using System.Reflection;
using System.Text.Json;

using EnterpriseTaskManagement.API.Middleware;
using EnterpriseTaskManagement.Application.Common.Exceptions;
using EnterpriseTaskManagement.Domain.Entities.Departments;
using EnterpriseTaskManagement.Domain.Entities.Projects;
using EnterpriseTaskManagement.Domain.Entities.Tasks;
using EnterpriseTaskManagement.Domain.Entities.Users;
using EnterpriseTaskManagement.Domain.Exceptions;
using EnterpriseTaskManagement.Infrastructure.Authentication;
using EnterpriseTaskManagement.Infrastructure.Persistence;
using EnterpriseTaskManagement.Tests.TestDoubles;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

using ValidationException = EnterpriseTaskManagement.Application.Common.Exceptions.ValidationException;

namespace EnterpriseTaskManagement.Tests.Api;

public sealed class GlobalExceptionHandlerTests
{
    public static TheoryData<Exception, int> Cases() => new()
    {
        { NotFoundException.For("Task", Guid.Empty), 404 },
        { new AuthenticationFailedException("leaky detail"), 401 },
        { new ForbiddenAccessException(), 403 },
        { new ConflictException("dup"), 409 },
        { new DbUpdateConcurrencyException("stale"), 409 },
        { new ValidationException(), 400 },
        { new DomainException("rule"), 400 },
        { new ArgumentException("arg"), 400 },
        { new InvalidOperationException("secret internals"), 500 },
    };

    private static async Task<(int Status, JsonElement Body)> Run(Exception exception)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/Tasks";
        context.Response.Body = new MemoryStream();

        var handled = await new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance)
            .TryHandleAsync(context, exception, default);
        Assert.True(handled);

        context.Response.Body.Position = 0;
        var body = await JsonDocument.ParseAsync(context.Response.Body);
        return (context.Response.StatusCode, body.RootElement.Clone());
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Maps_exceptions_to_problem_details(Exception exception, int expected)
    {
        var (status, body) = await Run(exception);

        Assert.Equal(expected, status);
        Assert.Equal(expected, body.GetProperty("status").GetInt32());
        Assert.Equal("/api/Tasks", body.GetProperty("instance").GetString());
        Assert.True(body.TryGetProperty("traceId", out _));
        Assert.False(string.IsNullOrEmpty(body.GetProperty("title").GetString()));
    }

    [Fact]
    public async Task Auth_failure_detail_is_always_generic()
    {
        var (_, body) = await Run(new AuthenticationFailedException("user does not exist"));
        Assert.Equal(AuthenticationFailedException.GenericMessage, body.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Unhandled_exception_does_not_leak_message()
    {
        var (_, body) = await Run(new InvalidOperationException("connection string xyz"));
        Assert.DoesNotContain("xyz", body.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Validation_errors_are_exposed_per_field()
    {
        var failures = new[] { new FluentValidation.Results.ValidationFailure("Email", "Email is required.") };
        var (_, body) = await Run(new ValidationException(failures));

        Assert.Equal("Email is required.", body.GetProperty("errors").GetProperty("Email")[0].GetString());
    }
}

public sealed class PublicIdContractTests
{
    private static readonly Assembly[] Assemblies =
    [
        typeof(EnterpriseTaskManagement.Application.DependencyInjection).Assembly,
        typeof(GlobalExceptionHandler).Assembly,
    ];

    [Fact]
    public void No_request_or_response_contract_exposes_an_internal_numeric_id()
    {
        var contracts = Assemblies
            .SelectMany(a => a.GetExportedTypes())
            .Where(t => t.Name.EndsWith("Request", StringComparison.Ordinal)
                     || t.Name.EndsWith("Response", StringComparison.Ordinal)
                     || t.Name.EndsWith("Summary", StringComparison.Ordinal)
                     || t.Name.EndsWith("Command", StringComparison.Ordinal)
                     || t.Name.EndsWith("Query", StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(contracts);

        var offenders = contracts
            .SelectMany(t => t.GetProperties().Select(p => (Type: t, Prop: p)))
            .Where(x => x.Prop.Name.EndsWith("Id", StringComparison.Ordinal)
                     && Nullable.GetUnderlyingType(x.Prop.PropertyType) is var u
                     && (u ?? x.Prop.PropertyType) is var pt
                     && (pt == typeof(long) || pt == typeof(int)))
            .Select(x => $"{x.Type.Name}.{x.Prop.Name}")
            .ToList();

        Assert.Empty(offenders);
    }
}

/// <summary>
/// Reads the EF Core model built from the real configurations with the SQL Server
/// provider. Building the model never opens a connection; no database is used.
/// </summary>
public sealed class PersistenceModelTests
{
    private static ApplicationDbContext Context() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=unused.invalid;Database=unused;Trusted_Connection=True;")
            .Options);

    [Theory]
    [InlineData(typeof(User))]
    [InlineData(typeof(Department))]
    [InlineData(typeof(Project))]
    [InlineData(typeof(TaskItem))]
    public void Aggregates_have_soft_delete_filter_and_rowversion(Type clr)
    {
        using var db = Context();
        var entity = db.Model.FindEntityType(clr)!;

        Assert.NotEmpty(entity.GetDeclaredQueryFilters());
        var version = entity.FindProperty("Version")!;
        Assert.True(version.IsConcurrencyToken);
        Assert.Equal(Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAddOrUpdate, version.ValueGenerated);
    }

    [Theory]
    [InlineData(typeof(User))]
    [InlineData(typeof(Department))]
    [InlineData(typeof(Project))]
    [InlineData(typeof(TaskItem))]
    public void PublicId_has_unique_index(Type clr)
    {
        using var db = Context();
        var entity = db.Model.FindEntityType(clr)!;

        Assert.Contains(entity.GetIndexes(), i => i.IsUnique && i.Properties.Any(p => p.Name == "PublicId"));
    }
}

public sealed class InfrastructureAuthTests
{
    [Fact]
    public void PasswordHasher_round_trips_and_salts()
    {
        var hasher = new PasswordHasher();
        var a = hasher.Hash("correct horse");
        var b = hasher.Hash("correct horse");

        Assert.NotEqual(a, b);
        Assert.True(hasher.Verify("correct horse", a));
        Assert.False(hasher.Verify("wrong", a));
    }

    [Fact]
    public void JwtTokenService_rejects_short_secret() =>
        Assert.Throws<InvalidOperationException>(() => new JwtTokenService(Options.Create(new JwtSettings
        {
            Issuer = "i", Audience = "a", SecretKey = "short",
        })));

    [Fact]
    public void JwtTokenService_emits_public_id_and_exactly_one_role()
    {
        var service = new JwtTokenService(Options.Create(new JwtSettings
        {
            Issuer = "tests", Audience = "tests", SecretKey = new string('k', 48), ExpirationMinutes = 5,
        }));
        var user = Build.User(UserRole.Admin);

        var token = new JsonWebToken(service.CreateAccessToken(user).Value);

        Assert.Equal(user.PublicId.ToString("N"), token.GetClaim(JwtTokenService.PublicIdClaimType).Value);
        var roles = token.Claims.Where(c => c.Type.EndsWith("role", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.Single(roles);
        Assert.Equal("Admin", roles[0].Value);
        Assert.DoesNotContain(token.Claims, c => c.Value == user.Id.ToString(System.Globalization.CultureInfo.InvariantCulture) && c.Type != "nbf" && c.Type != "exp" && c.Type != "iat");
    }
}
