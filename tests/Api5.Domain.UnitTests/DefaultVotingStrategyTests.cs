using Api5.Domain.Exceptions;
using Api5.Domain.VoteAggregate.Strategies;
using FluentAssertions;
using Xunit;

namespace Api5.Domain.UnitTests;

/// <summary>
/// Unit tests for <see cref="DefaultVotingStrategy"/> — the one-vote-per-user-per-note
/// strategy that replicates API 1–4 voting behaviour.
/// </summary>
/// <remarks>
/// DESIGN: These tests verify the strategy's <see cref="IVotingStrategy.Validate"/>
/// method, which composes <c>NoteExistsSpecification</c>,
/// <c>UserIsProjectMemberSpecification</c>, and <c>UniqueVotePerNoteSpecification</c>.
/// Each test targets a specific failure mode and verifies the thrown exception type
/// and message.
/// </remarks>
public class DefaultVotingStrategyTests
{
    private readonly DefaultVotingStrategy _strategy = new();

    /// <summary>
    /// Verifies that Validate does not throw when all conditions are met.
    /// </summary>
    [Fact]
    public void Validate_AllConditionsMet_DoesNotThrow()
    {
        // Arrange
        VoteEligibilityContext context = CreateContext(
            noteExists: true,
            userIsProjectMember: true,
            userAlreadyVotedOnNote: false);

        // Act
        Action act = () => _strategy.Validate(context);

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that Validate throws DomainException when the note does not exist.
    /// </summary>
    [Fact]
    public void Validate_NoteDoesNotExist_ThrowsDomainException()
    {
        // Arrange
        VoteEligibilityContext context = CreateContext(noteExists: false);

        // Act
        Action act = () => _strategy.Validate(context);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage($"*{context.NoteId}*");
    }

    /// <summary>
    /// Verifies that Validate throws InvariantViolationException when the user is not a member.
    /// </summary>
    [Fact]
    public void Validate_UserNotProjectMember_ThrowsInvariantViolation()
    {
        // Arrange
        VoteEligibilityContext context = CreateContext(userIsProjectMember: false);

        // Act
        Action act = () => _strategy.Validate(context);

        // Assert
        act.Should().Throw<InvariantViolationException>()
            .WithMessage($"*{context.UserId}*")
            .WithMessage($"*{context.ProjectId}*");
    }

    /// <summary>
    /// Verifies that Validate throws InvariantViolationException when the user already voted.
    /// </summary>
    [Fact]
    public void Validate_AlreadyVoted_ThrowsInvariantViolation()
    {
        // Arrange
        VoteEligibilityContext context = CreateContext(userAlreadyVotedOnNote: true);

        // Act
        Action act = () => _strategy.Validate(context);

        // Assert
        act.Should().Throw<InvariantViolationException>()
            .WithMessage($"*{context.UserId}*")
            .WithMessage($"*{context.NoteId}*");
    }

    /// <summary>
    /// Verifies that the Rules composite returns true when all conditions are met.
    /// </summary>
    [Fact]
    public void Rules_AllConditionsMet_ReturnsTrue()
    {
        // Arrange
        VoteEligibilityContext context = CreateContext(
            noteExists: true,
            userIsProjectMember: true,
            userAlreadyVotedOnNote: false);

        // Act & Assert
        _strategy.Rules.IsSatisfiedBy(context).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that the Rules composite returns false when the user already voted.
    /// </summary>
    [Fact]
    public void Rules_AlreadyVoted_ReturnsFalse()
    {
        // Arrange
        VoteEligibilityContext context = CreateContext(userAlreadyVotedOnNote: true);

        // Act & Assert
        _strategy.Rules.IsSatisfiedBy(context).Should().BeFalse();
    }

    // ── Helper ──────────────────────────────────────────────────

    private static VoteEligibilityContext CreateContext(
        bool noteExists = true,
        bool userIsProjectMember = true,
        bool userAlreadyVotedOnNote = false) =>
        new(
            NoteId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            ColumnId: Guid.NewGuid(),
            RetroBoardId: Guid.NewGuid(),
            ProjectId: Guid.NewGuid(),
            NoteExists: noteExists,
            UserIsProjectMember: userIsProjectMember,
            UserAlreadyVotedOnNote: userAlreadyVotedOnNote,
            UserVoteCountInColumn: 0);
}
