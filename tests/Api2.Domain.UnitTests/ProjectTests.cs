using Api2.Domain.Entities;
using Api2.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Api2.Domain.UnitTests;

/// <summary>
/// Unit tests for <see cref="Project"/> entity — constructor, <see cref="Project.AddMember"/>,
/// and <see cref="Project.RemoveMember"/>.
/// </summary>
/// <remarks>
/// DESIGN: In API 2, the Project entity owns its membership logic. These tests
/// verify that the in-memory invariant checks work correctly without a database.
/// This is a direct benefit of moving business logic from services (API 1) into
/// entities — domain rules become unit testable.
/// </remarks>
public class ProjectTests
{
    /// <summary>
    /// Verifies that a Project created with a valid name has its Name set correctly.
    /// </summary>
    [Fact]
    public void Constructor_WithValidName_SetsName()
    {
        // Arrange
        string name = "Sprint Retro";

        // Act
        Project project = new Project(name);

        // Assert
        project.Name.Should().Be(name);
    }

    /// <summary>
    /// Verifies that constructing a Project with a whitespace-only name throws <see cref="ArgumentException"/>.
    /// </summary>
    [Fact]
    public void Constructor_WithWhitespaceName_ThrowsArgumentException()
    {
        // Arrange
        string name = "   ";

        // Act
        Action act = () => new Project(name);

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("name");
    }

    /// <summary>
    /// Verifies that adding a new user creates a <see cref="ProjectMember"/> in the collection.
    /// </summary>
    [Fact]
    public void AddMember_WithNewUser_AddsMemberToCollection()
    {
        // Arrange
        Project project = new Project("Sprint Retro");
        Guid userId = Guid.NewGuid();

        // Act
        ProjectMember member = project.AddMember(userId);

        // Assert
        project.Members.Should().HaveCount(1);
        member.UserId.Should().Be(userId);
    }

    /// <summary>
    /// Verifies that adding a duplicate user throws <see cref="InvariantViolationException"/>.
    /// </summary>
    [Fact]
    public void AddMember_WithDuplicateUser_ThrowsInvariantViolation()
    {
        // Arrange
        Project project = new Project("Sprint Retro");
        Guid userId = Guid.NewGuid();
        project.AddMember(userId);

        // Act
        Action act = () => project.AddMember(userId);

        // Assert
        act.Should().Throw<InvariantViolationException>();
    }

    /// <summary>
    /// Verifies that removing an existing member removes it from the collection.
    /// </summary>
    [Fact]
    public void RemoveMember_WithExistingMember_RemovesMember()
    {
        // Arrange
        Project project = new Project("Sprint Retro");
        Guid userId = Guid.NewGuid();
        project.AddMember(userId);

        // Act
        project.RemoveMember(userId);

        // Assert
        project.Members.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that removing a non-existing user throws <see cref="DomainException"/>.
    /// </summary>
    [Fact]
    public void RemoveMember_WithNonExistingUser_ThrowsDomainException()
    {
        // Arrange
        Project project = new Project("Sprint Retro");
        Guid userId = Guid.NewGuid();

        // Act
        Action act = () => project.RemoveMember(userId);

        // Assert
        act.Should().Throw<DomainException>();
    }
}
