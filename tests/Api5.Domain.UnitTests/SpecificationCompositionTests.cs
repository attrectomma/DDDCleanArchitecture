using Api5.Domain.Common.Specifications;
using FluentAssertions;
using Xunit;

namespace Api5.Domain.UnitTests;

/// <summary>
/// Unit tests for the Specification pattern infrastructure:
/// <see cref="AndSpecification{T}"/>, <see cref="OrSpecification{T}"/>,
/// <see cref="NotSpecification{T}"/>, and <see cref="SpecificationExtensions"/>.
/// </summary>
/// <remarks>
/// DESIGN: These tests verify the boolean algebra of specification composition.
/// They use a simple integer-based specification to focus on the composition
/// logic rather than domain-specific rules. Domain-specific specification
/// tests are in <see cref="VoteEligibilitySpecificationTests"/>.
/// </remarks>
public class SpecificationCompositionTests
{
    // ── AND ─────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that AND returns true when both specifications are satisfied.
    /// </summary>
    [Fact]
    public void And_BothSatisfied_ReturnsTrue()
    {
        // Arrange
        ISpecification<int> isPositive = new InlineSpecification<int>(x => x > 0);
        ISpecification<int> isEven = new InlineSpecification<int>(x => x % 2 == 0);
        ISpecification<int> combined = isPositive.And(isEven);

        // Act & Assert
        combined.IsSatisfiedBy(4).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that AND returns false when only the left specification is satisfied.
    /// </summary>
    [Fact]
    public void And_OnlyLeftSatisfied_ReturnsFalse()
    {
        // Arrange
        ISpecification<int> isPositive = new InlineSpecification<int>(x => x > 0);
        ISpecification<int> isEven = new InlineSpecification<int>(x => x % 2 == 0);
        ISpecification<int> combined = isPositive.And(isEven);

        // Act & Assert
        combined.IsSatisfiedBy(3).Should().BeFalse();
    }

    /// <summary>
    /// Verifies that AND returns false when only the right specification is satisfied.
    /// </summary>
    [Fact]
    public void And_OnlyRightSatisfied_ReturnsFalse()
    {
        // Arrange
        ISpecification<int> isPositive = new InlineSpecification<int>(x => x > 0);
        ISpecification<int> isEven = new InlineSpecification<int>(x => x % 2 == 0);
        ISpecification<int> combined = isPositive.And(isEven);

        // Act & Assert
        combined.IsSatisfiedBy(-2).Should().BeFalse();
    }

    /// <summary>
    /// Verifies that AND returns false when neither specification is satisfied.
    /// </summary>
    [Fact]
    public void And_NeitherSatisfied_ReturnsFalse()
    {
        // Arrange
        ISpecification<int> isPositive = new InlineSpecification<int>(x => x > 0);
        ISpecification<int> isEven = new InlineSpecification<int>(x => x % 2 == 0);
        ISpecification<int> combined = isPositive.And(isEven);

        // Act & Assert
        combined.IsSatisfiedBy(-3).Should().BeFalse();
    }

    // ── OR ──────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that OR returns true when both specifications are satisfied.
    /// </summary>
    [Fact]
    public void Or_BothSatisfied_ReturnsTrue()
    {
        // Arrange
        ISpecification<int> isPositive = new InlineSpecification<int>(x => x > 0);
        ISpecification<int> isEven = new InlineSpecification<int>(x => x % 2 == 0);
        ISpecification<int> combined = isPositive.Or(isEven);

        // Act & Assert
        combined.IsSatisfiedBy(4).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that OR returns true when only the left specification is satisfied.
    /// </summary>
    [Fact]
    public void Or_OnlyLeftSatisfied_ReturnsTrue()
    {
        // Arrange
        ISpecification<int> isPositive = new InlineSpecification<int>(x => x > 0);
        ISpecification<int> isEven = new InlineSpecification<int>(x => x % 2 == 0);
        ISpecification<int> combined = isPositive.Or(isEven);

        // Act & Assert
        combined.IsSatisfiedBy(3).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that OR returns false when neither specification is satisfied.
    /// </summary>
    [Fact]
    public void Or_NeitherSatisfied_ReturnsFalse()
    {
        // Arrange
        ISpecification<int> isPositive = new InlineSpecification<int>(x => x > 0);
        ISpecification<int> isEven = new InlineSpecification<int>(x => x % 2 == 0);
        ISpecification<int> combined = isPositive.Or(isEven);

        // Act & Assert
        combined.IsSatisfiedBy(-3).Should().BeFalse();
    }

    // ── NOT ─────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that NOT returns false when the inner specification is satisfied.
    /// </summary>
    [Fact]
    public void Not_InnerSatisfied_ReturnsFalse()
    {
        // Arrange
        ISpecification<int> isPositive = new InlineSpecification<int>(x => x > 0);
        ISpecification<int> notPositive = isPositive.Not();

        // Act & Assert
        notPositive.IsSatisfiedBy(5).Should().BeFalse();
    }

    /// <summary>
    /// Verifies that NOT returns true when the inner specification is not satisfied.
    /// </summary>
    [Fact]
    public void Not_InnerNotSatisfied_ReturnsTrue()
    {
        // Arrange
        ISpecification<int> isPositive = new InlineSpecification<int>(x => x > 0);
        ISpecification<int> notPositive = isPositive.Not();

        // Act & Assert
        notPositive.IsSatisfiedBy(-1).Should().BeTrue();
    }

    // ── Chaining ────────────────────────────────────────────────

    /// <summary>
    /// Verifies that multiple specifications can be chained: A AND B AND C.
    /// </summary>
    [Fact]
    public void And_ChainedThreeSpecs_AllMustBeSatisfied()
    {
        // Arrange
        ISpecification<int> isPositive = new InlineSpecification<int>(x => x > 0);
        ISpecification<int> isEven = new InlineSpecification<int>(x => x % 2 == 0);
        ISpecification<int> isLessThan100 = new InlineSpecification<int>(x => x < 100);
        ISpecification<int> combined = isPositive.And(isEven).And(isLessThan100);

        // Act & Assert
        combined.IsSatisfiedBy(42).Should().BeTrue();
        combined.IsSatisfiedBy(101).Should().BeFalse();
        combined.IsSatisfiedBy(-4).Should().BeFalse();
    }

    /// <summary>
    /// Verifies that AND and NOT can be combined: A AND NOT(B).
    /// </summary>
    [Fact]
    public void And_WithNot_ComposesCorrectly()
    {
        // Arrange
        ISpecification<int> isPositive = new InlineSpecification<int>(x => x > 0);
        ISpecification<int> isEven = new InlineSpecification<int>(x => x % 2 == 0);
        ISpecification<int> positiveAndOdd = isPositive.And(isEven.Not());

        // Act & Assert
        positiveAndOdd.IsSatisfiedBy(3).Should().BeTrue();
        positiveAndOdd.IsSatisfiedBy(4).Should().BeFalse();
        positiveAndOdd.IsSatisfiedBy(-3).Should().BeFalse();
    }

    // ── Test helper ─────────────────────────────────────────────

    /// <summary>
    /// Inline specification for testing composition without domain-specific types.
    /// </summary>
    private class InlineSpecification<T> : ISpecification<T>
    {
        private readonly Func<T, bool> _predicate;

        public InlineSpecification(Func<T, bool> predicate) => _predicate = predicate;

        public bool IsSatisfiedBy(T candidate) => _predicate(candidate);
    }
}
