using Api2.Domain.Entities;
using Api2.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Api2.Domain.UnitTests;

/// <summary>
/// Unit tests for <see cref="Column"/> entity — constructor, <see cref="Column.Rename"/>,
/// and <see cref="Column.AddNote"/>.
/// </summary>
/// <remarks>
/// DESIGN: In API 2, Column owns its note collection and enforces text uniqueness
/// via <see cref="Column.AddNote"/>. Column also supports renaming via
/// <see cref="Column.Rename"/> with guard-clause validation. Cross-sibling
/// uniqueness (column names within a retro board) cannot be checked here —
/// the service layer still handles that. API 3 resolves this by making the
/// RetroBoard aggregate root the entry point for all column/note mutations.
/// </remarks>
public class ColumnTests
{
    /// <summary>
    /// Verifies that a Column created with valid arguments has its properties set correctly.
    /// </summary>
    [Fact]
    public void Constructor_WithValidArgs_SetsProperties()
    {
        // Arrange
        Guid retroBoardId = Guid.NewGuid();
        string name = "What went well";

        // Act
        Column column = new Column(retroBoardId, name);

        // Assert
        column.RetroBoardId.Should().Be(retroBoardId);
        column.Name.Should().Be(name);
    }

    /// <summary>
    /// Verifies that constructing a Column with a null name throws <see cref="ArgumentException"/>.
    /// </summary>
    [Fact]
    public void Constructor_WithNullName_ThrowsArgumentException()
    {
        // Arrange
        Guid retroBoardId = Guid.NewGuid();
        string name = null!;

        // Act
        Action act = () => new Column(retroBoardId, name);

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("name");
    }

    /// <summary>
    /// Verifies that renaming a Column with a valid name updates the Name property.
    /// </summary>
    [Fact]
    public void Rename_WithValidName_UpdatesName()
    {
        // Arrange
        Column column = new Column(Guid.NewGuid(), "Old Name");

        // Act
        column.Rename("New Name");

        // Assert
        column.Name.Should().Be("New Name");
    }

    /// <summary>
    /// Verifies that renaming a Column with a whitespace-only name throws <see cref="ArgumentException"/>.
    /// </summary>
    [Fact]
    public void Rename_WithWhitespaceName_ThrowsArgumentException()
    {
        // Arrange
        Column column = new Column(Guid.NewGuid(), "Valid Name");

        // Act
        Action act = () => column.Rename("   ");

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("newName");
    }

    /// <summary>
    /// Verifies that adding a note with unique text adds it to the collection.
    /// </summary>
    [Fact]
    public void AddNote_WithUniqueText_AddsNoteToCollection()
    {
        // Arrange
        Column column = new Column(Guid.NewGuid(), "What went well");

        // Act
        Note note = column.AddNote("Great teamwork");

        // Assert
        column.Notes.Should().HaveCount(1);
        note.Text.Should().Be("Great teamwork");
    }

    /// <summary>
    /// Verifies that adding a note with duplicate text throws <see cref="InvariantViolationException"/>.
    /// </summary>
    [Fact]
    public void AddNote_WithDuplicateText_ThrowsInvariantViolation()
    {
        // Arrange
        Column column = new Column(Guid.NewGuid(), "What went well");
        column.AddNote("Great teamwork");

        // Act
        Action act = () => column.AddNote("Great teamwork");

        // Assert
        act.Should().Throw<InvariantViolationException>();
    }

    /// <summary>
    /// Verifies that note text uniqueness is case-insensitive.
    /// </summary>
    [Fact]
    public void AddNote_WithDuplicateTextDifferentCase_ThrowsInvariantViolation()
    {
        // Arrange
        Column column = new Column(Guid.NewGuid(), "What went well");
        column.AddNote("Great teamwork");

        // Act
        Action act = () => column.AddNote("GREAT TEAMWORK");

        // Assert
        act.Should().Throw<InvariantViolationException>();
    }
}
