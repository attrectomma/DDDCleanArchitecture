using Api2.Domain.Entities;
using Api2.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Api2.Domain.UnitTests;

/// <summary>
/// Unit tests for <see cref="RetroBoard"/> entity — constructor and
/// <see cref="RetroBoard.AddColumn"/>.
/// </summary>
/// <remarks>
/// DESIGN: In API 2, RetroBoard has an <see cref="RetroBoard.AddColumn"/> method
/// that enforces column name uniqueness (case-insensitive). However, in practice
/// the ColumnService still uses a repository query for this check because loading
/// all columns just to add one is wasteful without aggregate boundaries. API 3
/// makes RetroBoard a true aggregate root where AddColumn is the primary path.
/// These tests prove the domain method works correctly in isolation.
/// </remarks>
public class RetroBoardTests
{
    /// <summary>
    /// Verifies that a RetroBoard created with valid arguments has its properties set correctly.
    /// </summary>
    [Fact]
    public void Constructor_WithValidArgs_SetsProperties()
    {
        // Arrange
        Guid projectId = Guid.NewGuid();
        string name = "Sprint 42 Retro";

        // Act
        RetroBoard board = new RetroBoard(projectId, name);

        // Assert
        board.ProjectId.Should().Be(projectId);
        board.Name.Should().Be(name);
    }

    /// <summary>
    /// Verifies that constructing a RetroBoard with an empty name throws <see cref="ArgumentException"/>.
    /// </summary>
    [Fact]
    public void Constructor_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        Guid projectId = Guid.NewGuid();
        string name = "";

        // Act
        Action act = () => new RetroBoard(projectId, name);

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("name");
    }

    /// <summary>
    /// Verifies that adding a column with a unique name adds it to the collection.
    /// </summary>
    [Fact]
    public void AddColumn_WithUniqueName_AddsColumnToCollection()
    {
        // Arrange
        RetroBoard board = new RetroBoard(Guid.NewGuid(), "Sprint 42 Retro");

        // Act
        Column column = board.AddColumn("What went well");

        // Assert
        board.Columns.Should().HaveCount(1);
        column.Name.Should().Be("What went well");
    }

    /// <summary>
    /// Verifies that adding a column with a duplicate name throws <see cref="InvariantViolationException"/>.
    /// </summary>
    [Fact]
    public void AddColumn_WithDuplicateName_ThrowsInvariantViolation()
    {
        // Arrange
        RetroBoard board = new RetroBoard(Guid.NewGuid(), "Sprint 42 Retro");
        board.AddColumn("What went well");

        // Act
        Action act = () => board.AddColumn("What went well");

        // Assert
        act.Should().Throw<InvariantViolationException>();
    }

    /// <summary>
    /// Verifies that column name uniqueness is case-insensitive.
    /// </summary>
    [Fact]
    public void AddColumn_WithDuplicateNameDifferentCase_ThrowsInvariantViolation()
    {
        // Arrange
        RetroBoard board = new RetroBoard(Guid.NewGuid(), "Sprint 42 Retro");
        board.AddColumn("What went well");

        // Act
        Action act = () => board.AddColumn("WHAT WENT WELL");

        // Assert
        act.Should().Throw<InvariantViolationException>();
    }
}
