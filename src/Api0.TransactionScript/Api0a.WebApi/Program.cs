using Api0a.WebApi.Data;
using Api0a.WebApi.Data.Interceptors;
using Api0a.WebApi.Endpoints;
using Api0a.WebApi.Middleware;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;

// ================================================================
// API 0a — Transaction Script (No Concurrency Safety)
// Program.cs: A single file configures everything — DI, middleware,
// and endpoint mapping. No layers, no services, no repositories.
//
// DESIGN: Compare this with API 1's Program.cs which registers
// 7 repository interfaces, 7 repository implementations, 7 service
// interfaces, 7 service implementations, and a UnitOfWork. Here we
// register only the DbContext, an interceptor, and validators.
// ================================================================

var builder = WebApplication.CreateBuilder(args);

// ── EF Core + Interceptors ──────────────────────────────────────

builder.Services.AddSingleton<AuditInterceptor>();

builder.Services.AddDbContext<RetroBoardDbContext>((serviceProvider, options) =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("RetroBoard"));
    options.AddInterceptors(serviceProvider.GetRequiredService<AuditInterceptor>());
});

// ── Validation ──────────────────────────────────────────────────

builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddFluentValidationAutoValidation();

// ── Swagger ─────────────────────────────────────────────────────

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ── Middleware pipeline ─────────────────────────────────────────

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

// ── Minimal API Endpoints ───────────────────────────────────────
// DESIGN: Each call maps a group of endpoints. No controllers, no
// routing attributes — just extension methods on IEndpointRouteBuilder.

app.MapUserEndpoints();
app.MapProjectEndpoints();
app.MapRetroBoardEndpoints();
app.MapColumnEndpoints();
app.MapNoteEndpoints();
app.MapVoteEndpoints();

app.Run();

// ================================================================
// Make the implicit Program class public so the test project can
// reference it via WebApplicationFactory<Program>.
// ================================================================
/// <summary>
/// Partial Program class to enable integration test access via
/// WebApplicationFactory.
/// </summary>
public partial class Program { }
