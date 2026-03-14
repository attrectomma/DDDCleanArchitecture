using Api4.Domain.UserAggregate;
using FluentAssertions;
using Xunit;

namespace Api4.Domain.UnitTests;

/// <summary>
/// Unit tests for <see cref="User"/> aggregate root — constructor guard clauses.
/// </summary>
/// <remarks>
/// DESIGN: Identical to API 3. User is unchanged between API 3 and API 4 because
/// the vote extraction does not affect user management. User remains a simple
/// aggregate root with its own repository and no child entities.
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
