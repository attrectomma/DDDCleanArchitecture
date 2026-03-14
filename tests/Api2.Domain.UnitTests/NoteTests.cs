using Api2.Domain.Entities;
using Api2.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Api2.Domain.UnitTests;

/// <summary>
/// Unit tests for <see cref="Note"/> entity — constructor, <see cref="Note.CastVote"/>,
/// <see cref="Note.RemoveVote"/>, and <see cref="Note.UpdateText"/>.
/// </summary>
/// <remarks>
/// DESIGN: In API 2, the Note entity owns vote management and enforces the
/// one-vote-per-user invariant via <see cref="Note.CastVote"/>. This is the
/// richest entity in API 2 — it has the most domain methods. In API 3+, vote
/// operations move to the RetroBoard aggregate root (API 3) or become a separate
/// Vote aggregate (API 4/5).
/// </remarks>
public class NoteTests
{
    /// <summary>
    /// Verifies that a Note created with valid arguments has its properties set correctly.
    /// </summary>
    [Fact]
    public void Constructor_WithValidArgs_SetsProperties()
    {
        // Arrange
        Guid columnId = Guid.NewGuid();
        string text = "Great teamwork";

        // Act
        Note note = new Note(columnId, text);

        // Assert
        note.ColumnId.Should().Be(columnId);
        note.Text.Should().Be(text);
    }

    /// <summary>
    /// Verifies that constructing a Note with empty text throws <see cref="ArgumentException"/>.
    /// </summary>
    [Fact]
    public void Constructor_WithEmptyText_ThrowsArgumentException()
    {
        // Arrange
        Guid columnId = Guid.NewGuid();
        string text = "";

        // Act
        Action act = () => new Note(columnId, text);

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("text");
    }

    /// <summary>
    /// Verifies that casting a vote for a new user adds a Vote to the collection.
    /// </summary>
    [Fact]
    public void CastVote_WithNewUser_AddsVoteToCollection()
    {
        // Arrange
        Note note = new Note(Guid.NewGuid(), "Great teamwork");
        Guid userId = Guid.NewGuid();

        // Act
        Vote vote = note.CastVote(userId);

        // Assert
        note.Votes.Should().HaveCount(1);
        vote.UserId.Should().Be(userId);
    }

    /// <summary>
    /// Verifies that casting a duplicate vote throws <see cref="InvariantViolationException"/>.
    /// </summary>
    [Fact]
    public void CastVote_WithDuplicateUser_ThrowsInvariantViolation()
    {
        // Arrange
        Note note = new Note(Guid.NewGuid(), "Great teamwork");
        Guid userId = Guid.NewGuid();
        note.CastVote(userId);

        // Act
        Action act = () => note.CastVote(userId);

        // Assert
        act.Should().Throw<InvariantViolationException>();
    }

    /// <summary>
    /// Verifies that removing an existing vote removes it from the collection.
    /// </summary>
    [Fact]
    public void RemoveVote_WithExistingVote_RemovesVote()
    {
        // Arrange
        Note note = new Note(Guid.NewGuid(), "Great teamwork");
        Guid userId = Guid.NewGuid();
        Vote vote = note.CastVote(userId);

        // Act
        note.RemoveVote(vote.Id);

        // Assert
        note.Votes.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that removing a non-existing vote throws <see cref="DomainException"/>.
    /// </summary>
    [Fact]
    public void RemoveVote_WithNonExistingVote_ThrowsDomainException()
    {
        // Arrange
        Note note = new Note(Guid.NewGuid(), "Great teamwork");
        Guid nonExistingVoteId = Guid.NewGuid();

        // Act
        Action act = () => note.RemoveVote(nonExistingVoteId);

        // Assert
        act.Should().Throw<DomainException>();
    }

    /// <summary>
    /// Verifies that updating text with a valid value changes the Text property.
    /// </summary>
    [Fact]
    public void UpdateText_WithValidText_UpdatesText()
    {
        // Arrange
        Note note = new Note(Guid.NewGuid(), "Original text");

        // Act
        note.UpdateText("Updated text");

        // Assert
        note.Text.Should().Be("Updated text");
    }

    /// <summary>
    /// Verifies that updating text with whitespace throws <see cref="ArgumentException"/>.
    /// </summary>
    [Fact]
    public void UpdateText_WithWhitespaceText_ThrowsArgumentException()
    {
        // Arrange
        Note note = new Note(Guid.NewGuid(), "Valid text");

        // Act
        Action act = () => note.UpdateText("   ");

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("newText");
    }
}
