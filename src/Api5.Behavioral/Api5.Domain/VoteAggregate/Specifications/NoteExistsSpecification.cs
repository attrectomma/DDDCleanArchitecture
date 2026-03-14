using Api5.Domain.Common.Specifications;
using Api5.Domain.VoteAggregate.Strategies;

namespace Api5.Domain.VoteAggregate.Specifications;

/// <summary>
/// Specification that checks whether the target note exists.
/// </summary>
/// <remarks>
/// DESIGN: This specification wraps a simple boolean check on the
/// <see cref="VoteEligibilityContext.NoteExists"/> property. The actual
/// database query happens in the command handler when building the context.
/// The specification evaluates the pre-fetched result — keeping the domain
/// layer free of infrastructure concerns.
///
/// Both <see cref="DefaultVotingStrategy"/> and <see cref="BudgetVotingStrategy"/>
/// include this specification in their composite rule. It serves as a defensive
/// check — the command handler typically throws <c>NotFoundException</c> before
/// reaching the strategy if the note does not exist.
/// </remarks>
public class NoteExistsSpecification : ISpecification<VoteEligibilityContext>
{
    /// <inheritdoc />
    public bool IsSatisfiedBy(VoteEligibilityContext candidate) =>
        candidate.NoteExists;
}
