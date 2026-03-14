using Api5.Domain.Common.Specifications;
using Api5.Domain.VoteAggregate.Specifications;
using Api5.Domain.VoteAggregate.Strategies;
using FluentAssertions;
using Xunit;

namespace Api5.Domain.UnitTests;

/// <summary>
/// Unit tests for the four concrete vote eligibility specifications:
/// <see cref="NoteExistsSpecification"/>,
/// <see cref="UserIsProjectMemberSpecification"/>,
/// <see cref="UniqueVotePerNoteSpecification"/>, and
/// <see cref="VoteBudgetNotExceededSpecification"/>.
/// </summary>
/// <remarks>
/// DESIGN: Each specification is tested in isolation with a crafted
/// <see cref="VoteEligibilityContext"/>. The contexts are built using
/// the <see cref="CreateContext"/> helper which provides sensible defaults
/// — tests override only the property relevant to the specification under test.
///
/// These tests demonstrate that specifications are pure, synchronous, and
/// require no infrastructure. Compare with API 4 where the equivalent
/// checks were buried inside <c>VoteService.CastVoteAsync</c> and required
/// mocking repositories to test.
/// </remarks>
public class VoteEligibilitySpecificationTests
{
    // ── NoteExistsSpecification ─────────────────────────────────

    /// <summary>
    /// Verifies that NoteExistsSpecification is satisfied when the note exists.
    /// </summary>
    [Fact]
    public void NoteExists_WhenNoteExists_ReturnsTrue()
    {
        // Arrange
        var spec = new NoteExistsSpecification();
        VoteEligibilityContext context = CreateContext(noteExists: true);

        // Act & Assert
        spec.IsSatisfiedBy(context).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that NoteExistsSpecification is not satisfied when the note does not exist.
    /// </summary>
    [Fact]
    public void NoteExists_WhenNoteDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var spec = new NoteExistsSpecification();
        VoteEligibilityContext context = CreateContext(noteExists: false);

        // Act & Assert
        spec.IsSatisfiedBy(context).Should().BeFalse();
    }

    // ── UserIsProjectMemberSpecification ────────────────────────

    /// <summary>
    /// Verifies that UserIsProjectMemberSpecification is satisfied when the user is a member.
    /// </summary>
    [Fact]
    public void UserIsProjectMember_WhenMember_ReturnsTrue()
    {
        // Arrange
        var spec = new UserIsProjectMemberSpecification();
        VoteEligibilityContext context = CreateContext(userIsProjectMember: true);

        // Act & Assert
        spec.IsSatisfiedBy(context).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that UserIsProjectMemberSpecification is not satisfied when the user is not a member.
    /// </summary>
    [Fact]
    public void UserIsProjectMember_WhenNotMember_ReturnsFalse()
    {
        // Arrange
        var spec = new UserIsProjectMemberSpecification();
        VoteEligibilityContext context = CreateContext(userIsProjectMember: false);

        // Act & Assert
        spec.IsSatisfiedBy(context).Should().BeFalse();
    }

    // ── UniqueVotePerNoteSpecification ──────────────────────────

    /// <summary>
    /// Verifies that UniqueVotePerNoteSpecification is satisfied when the user has not voted.
    /// </summary>
    [Fact]
    public void UniqueVotePerNote_WhenNotAlreadyVoted_ReturnsTrue()
    {
        // Arrange
        var spec = new UniqueVotePerNoteSpecification();
        VoteEligibilityContext context = CreateContext(userAlreadyVotedOnNote: false);

        // Act & Assert
        spec.IsSatisfiedBy(context).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that UniqueVotePerNoteSpecification is not satisfied when the user already voted.
    /// </summary>
    [Fact]
    public void UniqueVotePerNote_WhenAlreadyVoted_ReturnsFalse()
    {
        // Arrange
        var spec = new UniqueVotePerNoteSpecification();
        VoteEligibilityContext context = CreateContext(userAlreadyVotedOnNote: true);

        // Act & Assert
        spec.IsSatisfiedBy(context).Should().BeFalse();
    }

    // ── VoteBudgetNotExceededSpecification ──────────────────────

    /// <summary>
    /// Verifies that VoteBudgetNotExceeded is satisfied when the user has remaining votes.
    /// </summary>
    [Fact]
    public void VoteBudgetNotExceeded_WhenUnderBudget_ReturnsTrue()
    {
        // Arrange
        var spec = new VoteBudgetNotExceededSpecification(maxVotesPerColumn: 3);
        VoteEligibilityContext context = CreateContext(userVoteCountInColumn: 2);

        // Act & Assert
        spec.IsSatisfiedBy(context).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that VoteBudgetNotExceeded is not satisfied when the user is at the limit.
    /// </summary>
    [Fact]
    public void VoteBudgetNotExceeded_WhenAtBudget_ReturnsFalse()
    {
        // Arrange
        var spec = new VoteBudgetNotExceededSpecification(maxVotesPerColumn: 3);
        VoteEligibilityContext context = CreateContext(userVoteCountInColumn: 3);

        // Act & Assert
        spec.IsSatisfiedBy(context).Should().BeFalse();
    }

    /// <summary>
    /// Verifies that VoteBudgetNotExceeded is not satisfied when the user exceeds the limit.
    /// </summary>
    [Fact]
    public void VoteBudgetNotExceeded_WhenOverBudget_ReturnsFalse()
    {
        // Arrange
        var spec = new VoteBudgetNotExceededSpecification(maxVotesPerColumn: 3);
        VoteEligibilityContext context = CreateContext(userVoteCountInColumn: 5);

        // Act & Assert
        spec.IsSatisfiedBy(context).Should().BeFalse();
    }

    /// <summary>
    /// Verifies that VoteBudgetNotExceeded is satisfied when the user has no votes yet.
    /// </summary>
    [Fact]
    public void VoteBudgetNotExceeded_WhenZeroVotes_ReturnsTrue()
    {
        // Arrange
        var spec = new VoteBudgetNotExceededSpecification(maxVotesPerColumn: 3);
        VoteEligibilityContext context = CreateContext(userVoteCountInColumn: 0);

        // Act & Assert
        spec.IsSatisfiedBy(context).Should().BeTrue();
    }

    // ── Composite: Default strategy rule ────────────────────────

    /// <summary>
    /// Verifies that the Default strategy's composite rule (NoteExists AND
    /// UserIsProjectMember AND UniqueVotePerNote) passes when all conditions are met.
    /// </summary>
    [Fact]
    public void DefaultComposite_AllSatisfied_ReturnsTrue()
    {
        // Arrange
        ISpecification<VoteEligibilityContext> composite =
            new NoteExistsSpecification()
                .And(new UserIsProjectMemberSpecification())
                .And(new UniqueVotePerNoteSpecification());

        VoteEligibilityContext context = CreateContext(
            noteExists: true,
            userIsProjectMember: true,
            userAlreadyVotedOnNote: false);

        // Act & Assert
        composite.IsSatisfiedBy(context).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that the Default strategy's composite rule fails when the user
    /// already voted (even if note exists and user is a member).
    /// </summary>
    [Fact]
    public void DefaultComposite_AlreadyVoted_ReturnsFalse()
    {
        // Arrange
        ISpecification<VoteEligibilityContext> composite =
            new NoteExistsSpecification()
                .And(new UserIsProjectMemberSpecification())
                .And(new UniqueVotePerNoteSpecification());

        VoteEligibilityContext context = CreateContext(
            noteExists: true,
            userIsProjectMember: true,
            userAlreadyVotedOnNote: true);

        // Act & Assert
        composite.IsSatisfiedBy(context).Should().BeFalse();
    }

    // ── Composite: Budget strategy rule ─────────────────────────

    /// <summary>
    /// Verifies that the Budget strategy's composite rule passes even when the
    /// user already voted on the note, as long as the budget is not exceeded.
    /// This is the key difference from the Default strategy.
    /// </summary>
    [Fact]
    public void BudgetComposite_AlreadyVotedButUnderBudget_ReturnsTrue()
    {
        // Arrange
        ISpecification<VoteEligibilityContext> composite =
            new NoteExistsSpecification()
                .And(new UserIsProjectMemberSpecification())
                .And(new VoteBudgetNotExceededSpecification(3));

        VoteEligibilityContext context = CreateContext(
            noteExists: true,
            userIsProjectMember: true,
            userAlreadyVotedOnNote: true,
            userVoteCountInColumn: 1);

        // Act & Assert — the Budget composite does NOT check uniqueness
        composite.IsSatisfiedBy(context).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that the Budget strategy's composite rule fails when
    /// the vote budget is exceeded.
    /// </summary>
    [Fact]
    public void BudgetComposite_BudgetExceeded_ReturnsFalse()
    {
        // Arrange
        ISpecification<VoteEligibilityContext> composite =
            new NoteExistsSpecification()
                .And(new UserIsProjectMemberSpecification())
                .And(new VoteBudgetNotExceededSpecification(3));

        VoteEligibilityContext context = CreateContext(
            noteExists: true,
            userIsProjectMember: true,
            userAlreadyVotedOnNote: true,
            userVoteCountInColumn: 3);

        // Act & Assert
        composite.IsSatisfiedBy(context).Should().BeFalse();
    }

    // ── Helper ──────────────────────────────────────────────────

    /// <summary>
    /// Creates a <see cref="VoteEligibilityContext"/> with sensible defaults.
    /// Override individual parameters to test specific scenarios.
    /// </summary>
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
