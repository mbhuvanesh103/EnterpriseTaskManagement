using System.Reflection;

using EnterpriseTaskManagement.API.Middleware;
using EnterpriseTaskManagement.Application;
using EnterpriseTaskManagement.Infrastructure;

using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Composition root. Each layer exposes exactly one registration entry point,
// so the API never touches EF Core, MediatR or FluentValidation directly.
// ---------------------------------------------------------------------------
builder.Services.AddControllers();

builder.Services.AddApplication();                       // MediatR + FluentValidation + validation pipeline
builder.Services.AddInfrastructure(builder.Configuration); // EF Core 10 + SQL Server + repositories

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

app.MapControllers();

app.Run();
