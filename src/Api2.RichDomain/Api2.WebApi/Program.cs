using Api2.Application.Interfaces;
using Api2.Application.Services;
using Api2.Infrastructure.Persistence;
using Api2.Infrastructure.Persistence.Interceptors;
using Api2.Infrastructure.Persistence.Repositories;
using Api2.WebApi.Middleware;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;

// ================================================================
// API 2 — Rich Domain
// Program.cs: Configures DI, middleware, and the HTTP pipeline.
// DESIGN: Compare with API 1 — the key difference is that
// IProjectMemberRepository is no longer registered because membership
// is now managed through the Project entity's domain methods.
// VoteService now depends on INoteRepository instead of IVoteRepository
// for its primary operations.
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
// DESIGN: No IProjectMemberRepository — membership flows through Project entity.
// IVoteRepository is kept but most vote operations go through Note entity.

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IRetroBoardRepository, RetroBoardRepository>();
builder.Services.AddScoped<IColumnRepository, ColumnRepository>();
builder.Services.AddScoped<INoteRepository, NoteRepository>();
builder.Services.AddScoped<IVoteRepository, VoteRepository>();

// ── Services (Scoped) ───────────────────────────────────────────

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IProjectMemberService, ProjectMemberService>();
builder.Services.AddScoped<IRetroBoardService, RetroBoardService>();
builder.Services.AddScoped<IColumnService, ColumnService>();
builder.Services.AddScoped<INoteService, NoteService>();
builder.Services.AddScoped<IVoteService, VoteService>();

// ── Unit of Work (Scoped) ───────────────────────────────────────

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ── Validation ──────────────────────────────────────────────────

builder.Services.AddValidatorsFromAssemblyContaining<Api2.Application.Validators.CreateUserRequestValidator>();
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
