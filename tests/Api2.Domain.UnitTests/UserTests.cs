using Api2.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Api2.Domain.UnitTests;

/// <summary>
/// Unit tests for <see cref="User"/> entity constructor and guard clauses.
/// </summary>
/// <remarks>
/// DESIGN: User is a simple reference entity with minimal domain behaviour.
/// These tests verify the factory constructor rejects invalid input via
/// <see cref="Guard.AgainstNullOrWhiteSpace"/> — a key advantage of rich
/// entities over the anemic DTOs in API 1, where invalid state could be
/// created freely.
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
