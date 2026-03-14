using Api5.Domain.Exceptions;
using Api5.Domain.VoteAggregate.Strategies;
using FluentAssertions;
using Xunit;

namespace Api5.Domain.UnitTests;

/// <summary>
/// Unit tests for <see cref="VotingStrategyFactory"/> — the factory that maps
/// <see cref="VotingStrategyType"/> enum values to concrete
/// <see cref="IVotingStrategy"/> implementations.
/// </summary>
public class VotingStrategyFactoryTests
{
    /// <summary>
    /// Verifies that the factory creates a <see cref="DefaultVotingStrategy"/>
    /// for <see cref="VotingStrategyType.Default"/>.
    /// </summary>
    [Fact]
    public void Create_DefaultType_ReturnsDefaultVotingStrategy()
    {
        // Act
        IVotingStrategy strategy = VotingStrategyFactory.Create(VotingStrategyType.Default);

        // Assert
        strategy.Should().BeOfType<DefaultVotingStrategy>();
    }

    /// <summary>
    /// Verifies that the factory creates a <see cref="BudgetVotingStrategy"/>
    /// for <see cref="VotingStrategyType.Budget"/>.
    /// </summary>
    [Fact]
    public void Create_BudgetType_ReturnsBudgetVotingStrategy()
    {
        // Act
        IVotingStrategy strategy = VotingStrategyFactory.Create(VotingStrategyType.Budget);

        // Assert
        strategy.Should().BeOfType<BudgetVotingStrategy>();
    }

    /// <summary>
    /// Verifies that the factory throws DomainException for an unknown strategy type.
    /// </summary>
    [Fact]
    public void Create_UnknownType_ThrowsDomainException()
    {
        // Arrange
        var unknownType = (VotingStrategyType)999;

        // Act
        Action act = () => VotingStrategyFactory.Create(unknownType);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Unknown voting strategy type*");
    }
}
