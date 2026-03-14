using Api5.Application.Common.Interfaces;
using Api5.Application.DTOs.Responses;

namespace Api5.Application.Projects.Commands.CreateProject;

/// <summary>
/// Command to create a new project.
/// </summary>
/// <param name="Name">The name of the project.</param>
public record CreateProjectCommand(string Name) : ICommand<ProjectResponse>;
