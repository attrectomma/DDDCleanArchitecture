using Api5.Application.DTOs.Responses;
using MediatR;

namespace Api5.Application.Projects.Queries.GetProject;

/// <summary>
/// Query to retrieve a project by its ID.
/// </summary>
/// <remarks>
/// DESIGN (CQRS): This is a READ operation. The handler projects directly
/// from the database — no aggregate loading, no change tracking.
/// </remarks>
/// <param name="ProjectId">The unique identifier of the project to retrieve.</param>
public record GetProjectQuery(Guid ProjectId) : IRequest<ProjectResponse>;
