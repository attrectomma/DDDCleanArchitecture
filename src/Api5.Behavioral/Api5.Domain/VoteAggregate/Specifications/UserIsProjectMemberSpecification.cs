using Api5.Domain.Common.Specifications;
using Api5.Domain.VoteAggregate.Strategies;

namespace Api5.Domain.VoteAggregate.Specifications;

/// <summary>
/// Specification that checks whether the user is a member of the project
/// associated with the retro board.
/// </summary>
/// <remarks>
/// DESIGN: Project membership is a cross-aggregate concern — the user
/// belongs to the Project aggregate, not the Vote or RetroBoard aggregate.
/// In API 4, this check was inline in <c>VoteService.CastVoteAsync</c>.
/// In API 5, it is encapsulated as a reusable specification that both
/// <see cref="DefaultVotingStrategy"/> and <see cref="BudgetVotingStrategy"/>
/// compose into their rule sets.
///
/// The <see cref="VoteEligibilityContext.UserIsProjectMember"/> property is
/// <c>true</c> when the project has no members (open access) or when the
/// user is explicitly a project member.
/// </remarks>
public class UserIsProjectMemberSpecification : ISpecification<VoteEligibilityContext>
{
    /// <inheritdoc />
    public bool IsSatisfiedBy(VoteEligibilityContext candidate) =>
        candidate.UserIsProjectMember;
}
