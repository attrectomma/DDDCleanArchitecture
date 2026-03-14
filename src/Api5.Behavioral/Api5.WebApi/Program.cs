using Api5.Application.Common.Behaviors;
using Api5.Application.Common.Interfaces;
using Api5.Application.Common.Options;
using Api5.Application.Votes.Commands.CastVote;
using Api5.Domain.ProjectAggregate;
using Api5.Domain.RetroAggregate;
using Api5.Domain.UserAggregate;
using Api5.Domain.VoteAggregate;
using Api5.Infrastructure.Persistence;
using Api5.Infrastructure.Persistence.Interceptors;
using Api5.Infrastructure.Persistence.Repositories;
using Api5.WebApi.Middleware;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Options;

// ================================================================
// API 5 — Behavior-centric + CQRS + MediatR
// Program.cs: Configures DI, middleware, and the HTTP pipeline.
//
// DESIGN: Key differences from API 4:
//   - NO service registrations (no IUserService, IProjectService, etc.)
//   - MediatR discovers command/query handlers by convention
//   - Pipeline behaviors (Logging, Validation) run for every request
//   - FluentValidation validators are on Commands, not Request DTOs
//   - DomainEventInterceptor dispatches domain events after SaveChanges
//   - IReadOnlyDbContext registered for CQRS query handlers
// ================================================================

var builder = WebApplication.CreateBuilder(args);

// ── Voting Options (Options pattern + validation) ───────────
// DESIGN: The Options pattern binds the "Voting" section of appsettings.json
// to a strongly-typed VotingOptions class. Combined with IValidateOptions<T>
// and ValidateOnStart(), misconfiguration (e.g., invalid strategy name,
// non-positive budget) is caught at startup — not when a user casts a vote.
//
// Compare with API 1–4 where voting behaviour was entirely hardcoded.
// The Options pattern makes behaviour configurable without code changes:
//   appsettings.json:
//     "Voting": { "DefaultVotingStrategy": "Budget", "MaxVotesPerColumn": 5 }

builder.Services
    .AddOptions<VotingOptions>()
    .Bind(builder.Configuration.GetSection(VotingOptions.SectionName))
    .ValidateOnStart();

builder.Services.AddSingleton<IValidateOptions<VotingOptions>, VotingOptionsValidator>();

// ── EF Core + Interceptors ──────────────────────────────────────

builder.Services.AddSingleton<AuditInterceptor>();

// DESIGN: DomainEventInterceptor needs IPublisher (MediatR) which is
// scoped, so the interceptor itself must be scoped. It is resolved
// from the service provider each time the DbContext is created.
builder.Services.AddScoped<DomainEventInterceptor>();

builder.Services.AddDbContext<RetroBoardDbContext>((serviceProvider, options) =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("RetroBoard"));

    // DESIGN: Interceptor order matters:
    //   1. AuditInterceptor (SavingChangesAsync) — stamps timestamps, converts deletes
    //   2. DomainEventInterceptor (SavedChangesAsync) — dispatches events after save
    options.AddInterceptors(
        serviceProvider.GetRequiredService<AuditInterceptor>(),
        serviceProvider.GetRequiredService<DomainEventInterceptor>());

    // DESIGN: Custom model cache key factory ensures the EF Core model is
    // rebuilt when the VotingOptions configuration changes. This is needed
    // because the Vote entity's unique index depends on the configured
    // default voting strategy (see OnModelCreating in RetroBoardDbContext).
    options.ReplaceService<IModelCacheKeyFactory, VotingStrategyModelCacheKeyFactory>();
});

// ── CQRS Read Side ──────────────────────────────────────────────
// DESIGN (CQRS): Query handlers depend on IReadOnlyDbContext, not on
// repositories. The concrete RetroBoardDbContext implements this interface.

builder.Services.AddScoped<IReadOnlyDbContext>(sp =>
    sp.GetRequiredService<RetroBoardDbContext>());

// ── Repositories (Scoped) ───────────────────────────────────────
// DESIGN: 4 repositories — one per aggregate root.
// Used only by command handlers (write side).

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IRetroBoardRepository, RetroBoardRepository>();
builder.Services.AddScoped<IVoteRepository, VoteRepository>();

// ── Unit of Work (Scoped) ───────────────────────────────────────

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ── MediatR ─────────────────────────────────────────────────────
// DESIGN: MediatR discovers all IRequestHandler and INotificationHandler
// implementations in the Application assembly by convention. No need to
// register each handler individually.

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CastVoteCommand).Assembly);
});

// ── Pipeline Behaviors (order matters) ──────────────────────────
// DESIGN: Behaviors run in the order they are registered:
//   1. LoggingBehavior — logs request name before and after
//   2. ValidationBehavior — validates the command/query, throws if invalid
// This ensures requests are logged even if validation fails.

builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// ── FluentValidation ────────────────────────────────────────────
// DESIGN: Validators are on Commands (not Request DTOs). They are
// discovered by convention from the Application assembly and invoked
// by the ValidationBehavior pipeline, NOT by ASP.NET model binding.

builder.Services.AddValidatorsFromAssembly(typeof(CastVoteCommand).Assembly);

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
