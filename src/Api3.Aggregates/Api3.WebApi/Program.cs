using Api3.Application.Interfaces;
using Api3.Application.Services;
using Api3.Domain.ProjectAggregate;
using Api3.Domain.RetroAggregate;
using Api3.Domain.UserAggregate;
using Api3.Infrastructure.Persistence;
using Api3.Infrastructure.Persistence.Interceptors;
using Api3.Infrastructure.Persistence.Repositories;
using Api3.WebApi.Middleware;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;

// ================================================================
// API 3 — Aggregate Design
// Program.cs: Configures DI, middleware, and the HTTP pipeline.
//
// DESIGN: Key differences from API 2:
//   - Only 3 repositories (User, Project, RetroBoard) instead of 6+
//   - Only 3 services (UserService, ProjectService, RetroBoardService)
//     instead of 7 — the RetroBoardService handles columns, notes, and votes
//   - Repository interfaces are in the Domain layer, not Application
//   - ConcurrencyConflictMiddleware is new — handles DbUpdateConcurrencyException
// ================================================================

var builder = WebApplication.CreateBuilder(args);

// ── EF Core + Interceptors ──────────────────────────────────────

builder.Services.AddSingleton<AuditInterceptor>();

builder.Services.AddDbContext<RetroBoardDbContext>((serviceProvider, options) =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("RetroBoard"));
    options.AddInterceptors(serviceProvider.GetRequiredService<AuditInterceptor>());
});

// ── Repositories (Scoped) ───────────────────────────────────────
// DESIGN: Only 3 repositories — one per aggregate root.
// No ColumnRepository, NoteRepository, or VoteRepository.

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IRetroBoardRepository, RetroBoardRepository>();

// ── Services (Scoped) ───────────────────────────────────────────
// DESIGN: Only 3 services — one per aggregate root.
// RetroBoardService replaces ColumnService, NoteService, and VoteService.

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IRetroBoardService, RetroBoardService>();

// ── Unit of Work (Scoped) ───────────────────────────────────────

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ── Validation ──────────────────────────────────────────────────

builder.Services.AddValidatorsFromAssemblyContaining<Api3.Application.Validators.CreateUserRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();

// ── Controllers + Swagger ───────────────────────────────────────

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ── Middleware pipeline ─────────────────────────────────────────

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// DESIGN: ConcurrencyConflictMiddleware must be registered BEFORE
// GlobalExceptionHandlerMiddleware so that DbUpdateConcurrencyException
// is caught by the concurrency middleware first, returning 409 Conflict.
app.UseMiddleware<ConcurrencyConflictMiddleware>();
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

app.MapControllers();

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
