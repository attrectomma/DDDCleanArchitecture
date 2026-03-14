using Api2.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Api2.Domain.UnitTests;

/// <summary>
/// Unit tests for <see cref="Vote"/> entity constructor.
/// </summary>
/// <remarks>
/// DESIGN: In API 2, Vote is a simple value-like entity with no domain methods
/// of its own — it is created by <see cref="Note.CastVote"/> and removed by
/// <see cref="Note.RemoveVote"/>. The constructor only records the note and user
/// IDs; there are no guard clauses because GUIDs cannot be null.
///
/// In API 4, Vote becomes its own aggregate root with independent lifecycle,
/// and in API 5 the constructor also raises a <c>VoteCastEvent</c>.
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
}
