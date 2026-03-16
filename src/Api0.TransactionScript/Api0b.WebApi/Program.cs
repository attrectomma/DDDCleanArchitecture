using Api0b.WebApi.Data;
using Api0b.WebApi.Data.Interceptors;
using Api0b.WebApi.Endpoints;
using Api0b.WebApi.Middleware;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;

// ================================================================
// API 0b — Transaction Script + Database Concurrency Safety
// Program.cs: Identical to Api0a's Program.cs. The concurrency safety
// changes are in entity configuration (xmin tokens) and middleware
// (catch blocks). The DI registrations and endpoint mappings are the same.
//
// DESIGN: Compare this with Api0a's Program.cs — they are identical.
// This demonstrates that the concurrency fix requires zero changes to
// the application's composition root. The safety is entirely in the
// entity model (xmin properties), the DbContext configuration (xmin
// mapping), and the middleware (catch blocks).
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
// DESIGN: Same endpoint mapping as Api0a. No changes needed.

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
