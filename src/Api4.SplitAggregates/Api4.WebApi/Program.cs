using Api4.Application.Interfaces;
using Api4.Application.Services;
using Api4.Domain.ProjectAggregate;
using Api4.Domain.RetroAggregate;
using Api4.Domain.UserAggregate;
using Api4.Domain.VoteAggregate;
using Api4.Infrastructure.Persistence;
using Api4.Infrastructure.Persistence.Interceptors;
using Api4.Infrastructure.Persistence.Repositories;
using Api4.WebApi.Middleware;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;

// ================================================================
// API 4 — Split Aggregates
// Program.cs: Configures DI, middleware, and the HTTP pipeline.
//
// DESIGN: Key differences from API 3:
//   - 4 repositories (User, Project, RetroBoard, Vote) instead of 3
//   - 4 services (UserService, ProjectService, RetroBoardService, VoteService)
//   - VoteService is NEW — handles cross-aggregate vote operations
//   - RetroBoardService no longer handles votes
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
// DESIGN: 4 repositories — one per aggregate root.
// The new VoteRepository is for the standalone Vote aggregate.

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IRetroBoardRepository, RetroBoardRepository>();
builder.Services.AddScoped<IVoteRepository, VoteRepository>();

// ── Services (Scoped) ───────────────────────────────────────────
// DESIGN: 4 services — one per aggregate root.
// VoteService is NEW and handles cross-aggregate vote coordination.

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IRetroBoardService, RetroBoardService>();
builder.Services.AddScoped<IVoteService, VoteService>();

// ── Unit of Work (Scoped) ───────────────────────────────────────

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ── Validation ──────────────────────────────────────────────────

builder.Services.AddValidatorsFromAssemblyContaining<Api4.Application.Validators.CreateUserRequestValidator>();
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
