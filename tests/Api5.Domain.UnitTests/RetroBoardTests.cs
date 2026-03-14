using Api5.Domain.Common;
using Api5.Domain.Exceptions;
using Api5.Domain.RetroAggregate;
using Api5.Domain.RetroAggregate.Events;
using Api5.Domain.VoteAggregate.Strategies;
using FluentAssertions;
using Xunit;

namespace Api5.Domain.UnitTests;

/// <summary>
/// Unit tests for <see cref="RetroBoard"/> aggregate root — constructor, all
/// column and note operations, and domain event assertions.
/// </summary>
/// <remarks>
/// DESIGN: In API 5, RetroBoard inherits from <see cref="AggregateRoot"/> and
/// raises domain events when structural changes occur:
///   - <see cref="ColumnAddedEvent"/> when a column is added.
///   - <see cref="NoteAddedEvent"/> when a note is added.
///   - <see cref="NoteRemovedEvent"/> when a note is removed.
///
/// These tests verify both the behavioral invariants (identical to API 4) and
/// the event payloads. The <see cref="NoteRemovedEvent"/> is particularly
/// important — it triggers orphaned vote cleanup in the Application layer's
/// <c>NoteRemovedEventHandler</c>, decoupling the RetroBoard aggregate from
/// the Vote aggregate entirely.
///
/// Vote operations remain absent (same as API 4) — Vote is its own aggregate.
/// </remarks>
public class RetroBoardTests
{
    // ── Constructor ─────────────────────────────────────────────

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
        board.VotingStrategyType.Should().Be(VotingStrategyType.Default);
    }

    /// <summary>
    /// Verifies that a RetroBoard can be created with a specific voting strategy.
    /// </summary>
    [Fact]
    public void Constructor_WithBudgetStrategy_SetsVotingStrategyType()
    {
        // Arrange
        Guid projectId = Guid.NewGuid();
        string name = "Budget Retro";

        // Act
        RetroBoard board = new RetroBoard(projectId, name, VotingStrategyType.Budget);

        // Assert
        board.VotingStrategyType.Should().Be(VotingStrategyType.Budget);
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

    // ── AddColumn ───────────────────────────────────────────────

    /// <summary>
    /// Verifies that adding a column with a unique name adds it to the collection.
    /// </summary>
    [Fact]
    public void AddColumn_WithUniqueName_AddsColumn()
    {
        // Arrange
        RetroBoard board = CreateBoard();

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
        RetroBoard board = CreateBoard();
        board.AddColumn("What went well");

        // Act
        Action act = () => board.AddColumn("What went well");

        // Assert
        act.Should().Throw<InvariantViolationException>();
    }

    /// <summary>
    /// Verifies that adding a column raises a <see cref="ColumnAddedEvent"/>
    /// with the correct payload.
    /// </summary>
    [Fact]
    public void AddColumn_WithUniqueName_RaisesColumnAddedEvent()
    {
        // Arrange
        RetroBoard board = CreateBoard();

        // Act
        Column column = board.AddColumn("What went well");

        // Assert
        IDomainEvent domainEvent = board.DomainEvents.Should().ContainSingle().Subject;
        ColumnAddedEvent addedEvent = domainEvent.Should().BeOfType<ColumnAddedEvent>().Subject;
        addedEvent.RetroBoardId.Should().Be(board.Id);
        addedEvent.ColumnId.Should().Be(column.Id);
        addedEvent.ColumnName.Should().Be("What went well");
    }

    // ── RenameColumn ────────────────────────────────────────────

    /// <summary>
    /// Verifies that renaming a column with a unique name updates the column's Name property.
    /// </summary>
    [Fact]
    public void RenameColumn_WithUniqueName_UpdatesName()
    {
        // Arrange
        RetroBoard board = CreateBoard();
        Column column = board.AddColumn("Old Name");

        // Act
        board.RenameColumn(column.Id, "New Name");

        // Assert
        column.Name.Should().Be("New Name");
    }

    /// <summary>
    /// Verifies that renaming a column to a name that already exists throws <see cref="InvariantViolationException"/>.
    /// </summary>
    /// <remarks>
    /// Columns need distinct Ids because <see cref="RetroBoard.RenameColumn"/> uses
    /// <c>c.Id != columnId</c> to exclude the column being renamed. Without EF Core,
    /// Id defaults to <see cref="Guid.Empty"/>, so we assign unique Ids explicitly.
    /// </remarks>
    [Fact]
    public void RenameColumn_WithDuplicateName_ThrowsInvariantViolation()
    {
        // Arrange
        RetroBoard board = CreateBoard();
        Column first = board.AddColumn("Keep");
        first.Id = Guid.NewGuid();
        Column second = board.AddColumn("Drop");
        second.Id = Guid.NewGuid();

        // Act
        Action act = () => board.RenameColumn(first.Id, "Drop");

        // Assert
        act.Should().Throw<InvariantViolationException>();
    }

    /// <summary>
    /// Verifies that renaming a non-existing column throws <see cref="DomainException"/>.
    /// </summary>
    [Fact]
    public void RenameColumn_WithNonExistingColumn_ThrowsDomainException()
    {
        // Arrange
        RetroBoard board = CreateBoard();

        // Act
        Action act = () => board.RenameColumn(Guid.NewGuid(), "Any Name");

        // Assert
        act.Should().Throw<DomainException>();
    }

    // ── RemoveColumn ────────────────────────────────────────────

    /// <summary>
    /// Verifies that removing an existing column removes it from the collection.
    /// </summary>
    [Fact]
    public void RemoveColumn_WithExistingColumn_RemovesColumn()
    {
        // Arrange
        RetroBoard board = CreateBoard();
        Column column = board.AddColumn("What went well");

        // Act
        board.RemoveColumn(column.Id);

        // Assert
        board.Columns.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that removing a non-existing column throws <see cref="DomainException"/>.
    /// </summary>
    [Fact]
    public void RemoveColumn_WithNonExistingColumn_ThrowsDomainException()
    {
        // Arrange
        RetroBoard board = CreateBoard();

        // Act
        Action act = () => board.RemoveColumn(Guid.NewGuid());

        // Assert
        act.Should().Throw<DomainException>();
    }

    // ── AddNote ─────────────────────────────────────────────────

    /// <summary>
    /// Verifies that adding a note with unique text adds it to the column's Notes collection.
    /// </summary>
    [Fact]
    public void AddNote_WithUniqueText_AddsNote()
    {
        // Arrange
        RetroBoard board = CreateBoard();
        Column column = board.AddColumn("What went well");

        // Act
        Note note = board.AddNote(column.Id, "Great teamwork");

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
        RetroBoard board = CreateBoard();
        Column column = board.AddColumn("What went well");
        board.AddNote(column.Id, "Great teamwork");

        // Act
        Action act = () => board.AddNote(column.Id, "Great teamwork");

        // Assert
        act.Should().Throw<InvariantViolationException>();
    }

    /// <summary>
    /// Verifies that adding a note to a non-existing column throws <see cref="DomainException"/>.
    /// </summary>
    [Fact]
    public void AddNote_ToNonExistingColumn_ThrowsDomainException()
    {
        // Arrange
        RetroBoard board = CreateBoard();

        // Act
        Action act = () => board.AddNote(Guid.NewGuid(), "Some text");

        // Assert
        act.Should().Throw<DomainException>();
    }

    /// <summary>
    /// Verifies that adding a note raises a <see cref="NoteAddedEvent"/>
    /// with the correct payload.
    /// </summary>
    [Fact]
    public void AddNote_WithUniqueText_RaisesNoteAddedEvent()
    {
        // Arrange
        RetroBoard board = CreateBoard();
        Column column = board.AddColumn("What went well");
        board.ClearDomainEvents(); // Clear the ColumnAddedEvent

        // Act
        Note note = board.AddNote(column.Id, "Great teamwork");

        // Assert
        IDomainEvent domainEvent = board.DomainEvents.Should().ContainSingle().Subject;
        NoteAddedEvent addedEvent = domainEvent.Should().BeOfType<NoteAddedEvent>().Subject;
        addedEvent.RetroBoardId.Should().Be(board.Id);
        addedEvent.ColumnId.Should().Be(column.Id);
        addedEvent.NoteId.Should().Be(note.Id);
    }

    // ── UpdateNote ──────────────────────────────────────────────

    /// <summary>
    /// Verifies that updating a note's text changes the Note's Text property.
    /// </summary>
    [Fact]
    public void UpdateNote_WithValidText_UpdatesText()
    {
        // Arrange
        RetroBoard board = CreateBoard();
        Column column = board.AddColumn("What went well");
        Note note = board.AddNote(column.Id, "Original text");

        // Act
        board.UpdateNote(column.Id, note.Id, "Updated text");

        // Assert
        note.Text.Should().Be("Updated text");
    }

    // ── RemoveNote ──────────────────────────────────────────────

    /// <summary>
    /// Verifies that removing an existing note removes it from the column's Notes collection.
    /// </summary>
    [Fact]
    public void RemoveNote_WithExistingNote_RemovesNote()
    {
        // Arrange
        RetroBoard board = CreateBoard();
        Column column = board.AddColumn("What went well");
        Note note = board.AddNote(column.Id, "Great teamwork");

        // Act
        board.RemoveNote(column.Id, note.Id);

        // Assert
        column.Notes.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that removing a note raises a <see cref="NoteRemovedEvent"/>
    /// with the correct payload.
    /// </summary>
    /// <remarks>
    /// DESIGN: This is the most important domain event test. The
    /// <see cref="NoteRemovedEvent"/> triggers orphaned vote cleanup in the
    /// Application layer's <c>NoteRemovedEventHandler</c>, decoupling the
    /// RetroBoard aggregate from the Vote aggregate entirely.
    /// </remarks>
    [Fact]
    public void RemoveNote_WithExistingNote_RaisesNoteRemovedEvent()
    {
        // Arrange
        RetroBoard board = CreateBoard();
        Column column = board.AddColumn("What went well");
        Note note = board.AddNote(column.Id, "Great teamwork");
        board.ClearDomainEvents(); // Clear ColumnAddedEvent and NoteAddedEvent

        // Act
        board.RemoveNote(column.Id, note.Id);

        // Assert
        IDomainEvent domainEvent = board.DomainEvents.Should().ContainSingle().Subject;
        NoteRemovedEvent removedEvent = domainEvent.Should().BeOfType<NoteRemovedEvent>().Subject;
        removedEvent.NoteId.Should().Be(note.Id);
        removedEvent.ColumnId.Should().Be(column.Id);
    }

    // ── SetVotingStrategy ───────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="RetroBoard.SetVotingStrategy"/> updates
    /// the <see cref="RetroBoard.VotingStrategyType"/> property.
    /// </summary>
    [Fact]
    public void SetVotingStrategy_ToBudget_UpdatesVotingStrategyType()
    {
        // Arrange
        RetroBoard board = CreateBoard();
        board.VotingStrategyType.Should().Be(VotingStrategyType.Default);

        // Act
        board.SetVotingStrategy(VotingStrategyType.Budget);

        // Assert
        board.VotingStrategyType.Should().Be(VotingStrategyType.Budget);
    }

    /// <summary>
    /// Verifies that <see cref="RetroBoard.SetVotingStrategy"/> can switch
    /// back from Budget to Default.
    /// </summary>
    [Fact]
    public void SetVotingStrategy_BackToDefault_UpdatesVotingStrategyType()
    {
        // Arrange
        RetroBoard board = new RetroBoard(Guid.NewGuid(), "Board", VotingStrategyType.Budget);

        // Act
        board.SetVotingStrategy(VotingStrategyType.Default);

        // Assert
        board.VotingStrategyType.Should().Be(VotingStrategyType.Default);
    }

    // ── Helper ──────────────────────────────────────────────────

    /// <summary>
    /// Creates a default <see cref="RetroBoard"/> for test setup.
    /// </summary>
    private static RetroBoard CreateBoard() => new RetroBoard(Guid.NewGuid(), "Sprint 42 Retro");
}
