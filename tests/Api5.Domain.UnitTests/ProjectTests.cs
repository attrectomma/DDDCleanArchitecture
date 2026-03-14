using Api5.Domain.Common;
using Api5.Domain.Exceptions;
using Api5.Domain.ProjectAggregate;
using Api5.Domain.ProjectAggregate.Events;
using FluentAssertions;
using Xunit;

namespace Api5.Domain.UnitTests;

/// <summary>
/// Unit tests for <see cref="Project"/> aggregate root — constructor,
/// <see cref="Project.AddMember"/>, <see cref="Project.RemoveMember"/>,
/// and domain event assertions.
/// </summary>
/// <remarks>
/// DESIGN: In API 5, the Project aggregate raises domain events when members
/// are added or removed. These tests verify both the behavioral invariants
/// (identical to API 3/4) and the event payloads. Domain events are a key
/// API 5 addition — they enable decoupled side effects via
/// <c>INotificationHandler</c> implementations without modifying the aggregate.
/// </remarks>
public class ProjectTests
{
    // ── Constructor ─────────────────────────────────────────────

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

    // ── AddMember ───────────────────────────────────────────────

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
    /// Verifies that adding a member raises a <see cref="MemberAddedToProjectEvent"/>
    /// with the correct payload.
    /// </summary>
    [Fact]
    public void AddMember_WithNewUser_RaisesMemberAddedToProjectEvent()
    {
        // Arrange
        Project project = new Project("Sprint Retro");
        Guid userId = Guid.NewGuid();

        // Act
        ProjectMember member = project.AddMember(userId);

        // Assert
        IDomainEvent domainEvent = project.DomainEvents.Should().ContainSingle().Subject;
        MemberAddedToProjectEvent addedEvent = domainEvent.Should().BeOfType<MemberAddedToProjectEvent>().Subject;
        addedEvent.ProjectId.Should().Be(project.Id);
        addedEvent.UserId.Should().Be(userId);
        addedEvent.MembershipId.Should().Be(member.Id);
    }

    // ── RemoveMember ────────────────────────────────────────────

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

    /// <summary>
    /// Verifies that removing a member raises a <see cref="MemberRemovedFromProjectEvent"/>
    /// with the correct payload.
    /// </summary>
    [Fact]
    public void RemoveMember_WithExistingMember_RaisesMemberRemovedFromProjectEvent()
    {
        // Arrange
        Project project = new Project("Sprint Retro");
        Guid userId = Guid.NewGuid();
        project.AddMember(userId);
        project.ClearDomainEvents(); // Clear the AddMember event

        // Act
        project.RemoveMember(userId);

        // Assert
        IDomainEvent domainEvent = project.DomainEvents.Should().ContainSingle().Subject;
        MemberRemovedFromProjectEvent removedEvent = domainEvent.Should().BeOfType<MemberRemovedFromProjectEvent>().Subject;
        removedEvent.ProjectId.Should().Be(project.Id);
        removedEvent.UserId.Should().Be(userId);
    }
}
