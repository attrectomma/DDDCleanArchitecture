using Api5.Domain.UserAggregate;
using FluentAssertions;
using Xunit;

namespace Api5.Domain.UnitTests;

/// <summary>
/// Unit tests for <see cref="User"/> aggregate root — constructor guard clauses.
/// </summary>
/// <remarks>
/// DESIGN: In API 5, User inherits from <see cref="Api5.Domain.Common.AggregateRoot"/>
/// which provides domain event support. However, User currently raises no events —
/// it remains a simple reference entity. The constructor guard clauses are identical
/// to API 3/4. The key API 5 change for User is architectural: controllers interact
/// with User via MediatR commands/queries instead of services.
/// </remarks>
public class UserTests
{
    /// <summary>
    /// Verifies that a User created with valid arguments has its properties set correctly.
    /// </summary>
    [Fact]
    public void Constructor_WithValidArgs_SetsProperties()
    {
        // Arrange
        string name = "Alice";
        string email = "alice@example.com";

        // Act
        User user = new User(name, email);

        // Assert
        user.Name.Should().Be(name);
        user.Email.Should().Be(email);
    }

    /// <summary>
    /// Verifies that constructing a User with a null name throws <see cref="ArgumentException"/>.
    /// </summary>
    [Fact]
    public void Constructor_WithNullName_ThrowsArgumentException()
    {
        // Arrange
        string name = null!;
        string email = "alice@example.com";

        // Act
        Action act = () => new User(name, email);

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("name");
    }

    /// <summary>
    /// Verifies that constructing a User with an empty email throws <see cref="ArgumentException"/>.
    /// </summary>
    [Fact]
    public void Constructor_WithEmptyEmail_ThrowsArgumentException()
    {
        // Arrange
        string name = "Alice";
        string email = "";

        // Act
        Action act = () => new User(name, email);

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("email");
    }
}
