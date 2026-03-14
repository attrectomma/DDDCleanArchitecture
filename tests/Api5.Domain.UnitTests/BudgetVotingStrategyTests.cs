using Api5.Domain.Exceptions;
using Api5.Domain.VoteAggregate.Strategies;
using FluentAssertions;
using Xunit;

namespace Api5.Domain.UnitTests;

/// <summary>
/// Unit tests for <see cref="BudgetVotingStrategy"/> — the dot-voting strategy
/// that allows multiple votes per note with a per-column budget.
/// </summary>
/// <remarks>
/// DESIGN: These tests highlight the key difference from
/// <see cref="DefaultVotingStrategy"/>: the Budget strategy does NOT enforce
/// uniqueness per note. A user can vote multiple times on the same note
/// as long as their column budget is not exceeded. This difference is
/// achieved by composing different specifications — the same Specification
/// pattern, different composition in each Strategy.
/// </remarks>
public class BudgetVotingStrategyTests
{
    private readonly BudgetVotingStrategy _strategy = new();

    /// <summary>
    /// Verifies that Validate does not throw when all conditions are met
    /// and the user has budget remaining.
    /// </summary>
    [Fact]
    public void Validate_AllConditionsMetAndUnderBudget_DoesNotThrow()
    {
        // Arrange
        VoteEligibilityContext context = CreateContext(
            noteExists: true,
            userIsProjectMember: true,
            userVoteCountInColumn: 2);

        // Act
        Action act = () => _strategy.Validate(context);

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that Validate does NOT throw when the user already voted on the
    /// same note, as long as the budget is not exceeded. This is the key
    /// behavioural difference from DefaultVotingStrategy.
    /// </summary>
    [Fact]
    public void Validate_AlreadyVotedOnNotButUnderBudget_DoesNotThrow()
    {
        // Arrange
        VoteEligibilityContext context = CreateContext(
            noteExists: true,
            userIsProjectMember: true,
            userAlreadyVotedOnNote: true,
            userVoteCountInColumn: 1);

        // Act
        Action act = () => _strategy.Validate(context);

        // Assert — Budget strategy allows duplicate votes on the same note
        act.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that Validate throws InvariantViolationException when the
    /// per-column vote budget is exceeded.
    /// </summary>
    [Fact]
    public void Validate_BudgetExceeded_ThrowsInvariantViolation()
    {
        // Arrange
        VoteEligibilityContext context = CreateContext(
            userVoteCountInColumn: BudgetVotingStrategy.DefaultMaxVotesPerColumn);

        // Act
        Action act = () => _strategy.Validate(context);

        // Assert
        act.Should().Throw<InvariantViolationException>()
            .WithMessage($"*{context.UserId}*")
            .WithMessage($"*{BudgetVotingStrategy.DefaultMaxVotesPerColumn}*");
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
    /// Verifies that the Rules composite returns true when all conditions are met.
    /// </summary>
    [Fact]
    public void Rules_AllConditionsMetAndUnderBudget_ReturnsTrue()
    {
        // Arrange
        VoteEligibilityContext context = CreateContext(
            noteExists: true,
            userIsProjectMember: true,
            userVoteCountInColumn: 0);

        // Act & Assert
        _strategy.Rules.IsSatisfiedBy(context).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that the Rules composite returns false when the budget is exceeded.
    /// </summary>
    [Fact]
    public void Rules_BudgetExceeded_ReturnsFalse()
    {
        // Arrange
        VoteEligibilityContext context = CreateContext(
            userVoteCountInColumn: BudgetVotingStrategy.DefaultMaxVotesPerColumn);

        // Act & Assert
        _strategy.Rules.IsSatisfiedBy(context).Should().BeFalse();
    }

    /// <summary>
    /// Verifies that a custom budget limit is respected.
    /// </summary>
    [Fact]
    public void Validate_CustomBudget_RespectsLimit()
    {
        // Arrange
        var customStrategy = new BudgetVotingStrategy(maxVotesPerColumn: 5);
        VoteEligibilityContext context = CreateContext(userVoteCountInColumn: 4);

        // Act
        Action act = () => customStrategy.Validate(context);

        // Assert — 4 < 5, should pass
        act.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that a custom budget limit throws when exceeded.
    /// </summary>
    [Fact]
    public void Validate_CustomBudgetExceeded_ThrowsInvariantViolation()
    {
        // Arrange
        var customStrategy = new BudgetVotingStrategy(maxVotesPerColumn: 5);
        VoteEligibilityContext context = CreateContext(userVoteCountInColumn: 5);

        // Act
        Action act = () => customStrategy.Validate(context);

        // Assert
        act.Should().Throw<InvariantViolationException>()
            .WithMessage("*5*");
    }

    // ── Helper ──────────────────────────────────────────────────

    private static VoteEligibilityContext CreateContext(
        bool noteExists = true,
        bool userIsProjectMember = true,
        bool userAlreadyVotedOnNote = false,
        int userVoteCountInColumn = 0) =>
        new(
            NoteId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            ColumnId: Guid.NewGuid(),
            RetroBoardId: Guid.NewGuid(),
            ProjectId: Guid.NewGuid(),
            NoteExists: noteExists,
            UserIsProjectMember: userIsProjectMember,
            UserAlreadyVotedOnNote: userAlreadyVotedOnNote,
            UserVoteCountInColumn: userVoteCountInColumn);
}
