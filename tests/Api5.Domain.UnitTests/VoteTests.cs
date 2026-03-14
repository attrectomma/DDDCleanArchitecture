using Api5.Domain.Common;
using Api5.Domain.VoteAggregate;
using Api5.Domain.VoteAggregate.Events;
using FluentAssertions;
using Xunit;

namespace Api5.Domain.UnitTests;

/// <summary>
/// Unit tests for <see cref="Vote"/> aggregate root — constructor and
/// <see cref="VoteCastEvent"/> domain event assertion.
/// </summary>
/// <remarks>
/// DESIGN: In API 5, Vote inherits from <see cref="AggregateRoot"/> and raises
/// a <see cref="VoteCastEvent"/> upon creation. This is the only difference from
/// API 4's Vote — the aggregate boundaries and the "one vote per user per note"
/// invariant enforcement (DB constraint + application check) are unchanged.
///
/// The <see cref="VoteCastEvent"/> enables decoupled side effects — handlers
/// can update real-time vote counts, send notifications, or maintain analytics
/// without the Vote aggregate knowing about those concerns.
/// </remarks>
public class VoteTests
{
    /// <summary>
    /// Verifies that a Vote created with valid arguments has its properties set correctly.
    /// </summary>
    [Fact]
    public void Constructor_WithValidArgs_SetsProperties()
    {
        // Arrange
        Guid noteId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();

        // Act
        Vote vote = new Vote(noteId, userId);

        // Assert
        vote.NoteId.Should().Be(noteId);
        vote.UserId.Should().Be(userId);
    }

    /// <summary>
    /// Verifies that constructing a Vote raises a <see cref="VoteCastEvent"/>
    /// with the correct payload.
    /// </summary>
    [Fact]
    public void Constructor_WithValidArgs_RaisesVoteCastEvent()
    {
        // Arrange
        Guid noteId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();

        // Act
        Vote vote = new Vote(noteId, userId);

        // Assert
        IDomainEvent domainEvent = vote.DomainEvents.Should().ContainSingle().Subject;
        VoteCastEvent castEvent = domainEvent.Should().BeOfType<VoteCastEvent>().Subject;
        castEvent.VoteId.Should().Be(vote.Id);
        castEvent.NoteId.Should().Be(noteId);
        castEvent.UserId.Should().Be(userId);
    }
}
