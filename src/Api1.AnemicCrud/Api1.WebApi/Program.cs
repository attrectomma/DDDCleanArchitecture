using Api1.Application.Interfaces;
using Api1.Application.Services;
using Api1.Infrastructure.Persistence;
using Api1.Infrastructure.Persistence.Interceptors;
using Api1.Infrastructure.Persistence.Repositories;
using Api1.WebApi.Middleware;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;

// ================================================================
// API 1 — Anemic CRUD
// Program.cs: Configures DI, middleware, and the HTTP pipeline.
// DESIGN: This is the simplest possible setup — one registration per
// repository, one per service, no aggregate grouping, no mediator.
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

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IProjectMemberRepository, ProjectMemberRepository>();
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

builder.Services.AddValidatorsFromAssemblyContaining<Api1.Application.Validators.CreateUserRequestValidator>();
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
