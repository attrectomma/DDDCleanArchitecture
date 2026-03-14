# Domain Unit Tests — Implementation Plan

> **Theme:** Introduce unit test projects for domain entities starting from API 2.
> Rich domain models enforce invariants via pure in-memory functions — they
> are unit testable **without mocking or infrastructure dependencies**.
> API 1 is excluded because its anemic entities have no behavior to test.

---

## 1. Rationale

### Why Unit Tests?

The repository currently relies exclusively on integration tests that spin up
a full ASP.NET Core host with a PostgreSQL Testcontainer. These tests verify
end-to-end correctness but are **slow** (container startup, HTTP round-trips,
database I/O) and **coarse-grained** (a failing invariant test doesn't tell
you whether the problem is in the entity, the service, or the controller).

Domain unit tests complement integration tests by:

| Benefit | Explanation |
|---------|-------------|
| **Speed** | Milliseconds per test — no Docker, no database, no HTTP. |
| **Precision** | Tests exercise a single entity method and assert a single outcome. |
| **Documentation** | Each test reads as a specification of the domain rule it covers. |
| **Safety net for refactoring** | Rename a method, change a guard — the unit test catches the regression before the slower integration suite even runs. |
| **Educational value** | Demonstrates that rich domain entities are inherently testable — a key advantage over anemic models. |

### Why Start from API 2?

| Tier | Entity Behavior | Unit Testable? |
|------|----------------|----------------|
| API 1 | Property bags — public setters, no methods | ❌ Nothing to test |
| API 2 | Guard clauses, factory constructors, invariant methods | ✅ |
| API 3 | Aggregate root methods enforcing cross-entity invariants | ✅ |
| API 4 | Same as API 3, with Vote as its own aggregate | ✅ |
| API 5 | Same as API 4, plus domain event assertions | ✅ |

### No Mocking Required

Domain entities depend on **nothing** — no `DbContext`, no repositories, no
HTTP clients. Every method under test is a pure function that takes arguments,
mutates in-memory state, and either returns a result or throws an exception.
This is a direct consequence of Clean Architecture's dependency rule: the
Domain layer has zero outward dependencies.

---

## 2. Scope

### What Is Tested

- **Entity constructors** — valid arguments produce a correctly initialized
  entity; invalid arguments throw `ArgumentException`.
- **Guard clauses** — `Guard.AgainstNullOrWhiteSpace` rejects null/empty/whitespace.
- **Invariant-enforcing methods** — `AddNote`, `CastVote`, `AddMember`,
  `AddColumn`, etc. — happy path returns the created child entity; duplicate
  input throws `InvariantViolationException`.
- **Mutation methods** — `Rename`, `UpdateText`, `RemoveMember`, `RemoveVote`
  — happy path mutates state; invalid input throws the appropriate exception.
- **Domain events (API 5 only)** — after a domain operation, the aggregate's
  `DomainEvents` collection contains the expected event type with the correct
  payload.

### What Is NOT Tested

- Persistence (EF Core, migrations) — covered by integration tests.
- HTTP endpoints, controllers, middleware — covered by integration tests.
- Application services / MediatR handlers — orchestration logic that depends
  on repositories and UoW; covered by integration tests.
- Cross-aggregate invariants enforced via DB constraints — integration tests.

---

## 3. New Project Structure

```
tests/
├── RetroBoard.IntegrationTests.Shared/        (existing)
├── Api1.IntegrationTests/                     (existing — no unit tests for API 1)
├── Api2.IntegrationTests/                     (existing)
├── Api2.Domain.UnitTests/                     ← NEW
│   ├── UserTests.cs
│   ├── ProjectTests.cs
│   ├── RetroBoardTests.cs
│   ├── ColumnTests.cs
│   ├── NoteTests.cs
│   ├── VoteTests.cs
│   └── Api2.Domain.UnitTests.csproj
├── Api3.IntegrationTests/                     (existing)
├── Api3.Domain.UnitTests/                     ← NEW
│   ├── UserTests.cs
│   ├── ProjectTests.cs
│   ├── RetroBoardTests.cs
│   └── Api3.Domain.UnitTests.csproj
├── Api4.IntegrationTests/                     (existing)
├── Api4.Domain.UnitTests/                     ← NEW
│   ├── UserTests.cs
│   ├── ProjectTests.cs
│   ├── RetroBoardTests.cs
│   ├── VoteTests.cs
│   └── Api4.Domain.UnitTests.csproj
├── Api5.IntegrationTests/                     (existing)
└── Api5.Domain.UnitTests/                     ← NEW
    ├── UserTests.cs
    ├── ProjectTests.cs
    ├── RetroBoardTests.cs
    ├── VoteTests.cs
    └── Api5.Domain.UnitTests.csproj
```

### Why No Shared Unit Test Base?

Unlike integration tests (where all 5 APIs share the same HTTP contract and
thus the same test cases), unit tests exercise **API-specific domain APIs**
that differ significantly between tiers:

- API 2 entities: `Column.AddNote()`, `Note.CastVote()`
- API 3 aggregate root: `RetroBoard.AddNote(columnId, text)`, `RetroBoard.CastVote(columnId, noteId, userId)`
- API 4 aggregate root: no vote methods on RetroBoard (Vote is its own aggregate)
- API 5 aggregate root: same as API 4, plus domain event assertions

A shared base would create artificial coupling between tiers that are meant to
be compared, not unified.

---

## 4. Test Case Inventory

### 4.1 API 2 — Rich Entity Tests

| Class | Method Under Test | Test Name | Asserts |
|-------|------------------|-----------|---------|
| **UserTests** | `User(name, email)` | `Constructor_WithValidArgs_SetsProperties` | `Name`, `Email` set correctly |
| | `User(name, email)` | `Constructor_WithNullName_ThrowsArgumentException` | `ArgumentException` |
| | `User(name, email)` | `Constructor_WithEmptyEmail_ThrowsArgumentException` | `ArgumentException` |
| **ProjectTests** | `Project(name)` | `Constructor_WithValidName_SetsName` | `Name` set |
| | `Project(name)` | `Constructor_WithWhitespaceName_ThrowsArgumentException` | `ArgumentException` |
| | `AddMember(userId)` | `AddMember_WithNewUser_AddsMemberToCollection` | `Members.Count == 1`, correct `UserId` |
| | `AddMember(userId)` | `AddMember_WithDuplicateUser_ThrowsInvariantViolation` | `InvariantViolationException` |
| | `RemoveMember(userId)` | `RemoveMember_WithExistingMember_RemovesMember` | `Members.Count == 0` |
| | `RemoveMember(userId)` | `RemoveMember_WithNonExistingUser_ThrowsDomainException` | `DomainException` |
| **RetroBoardTests** | `RetroBoard(projectId, name)` | `Constructor_WithValidArgs_SetsProperties` | `ProjectId`, `Name` set |
| | `RetroBoard(projectId, name)` | `Constructor_WithEmptyName_ThrowsArgumentException` | `ArgumentException` |
| | `AddColumn(name)` | `AddColumn_WithUniqueName_AddsColumnToCollection` | `Columns.Count == 1`, correct `Name` |
| | `AddColumn(name)` | `AddColumn_WithDuplicateName_ThrowsInvariantViolation` | `InvariantViolationException` |
| | `AddColumn(name)` | `AddColumn_WithDuplicateNameDifferentCase_ThrowsInvariantViolation` | `InvariantViolationException` (case-insensitive) |
| **ColumnTests** | `Column(retroBoardId, name)` | `Constructor_WithValidArgs_SetsProperties` | `RetroBoardId`, `Name` set |
| | `Column(retroBoardId, name)` | `Constructor_WithNullName_ThrowsArgumentException` | `ArgumentException` |
| | `Rename(newName)` | `Rename_WithValidName_UpdatesName` | `Name` updated |
| | `Rename(newName)` | `Rename_WithWhitespaceName_ThrowsArgumentException` | `ArgumentException` |
| | `AddNote(text)` | `AddNote_WithUniqueText_AddsNoteToCollection` | `Notes.Count == 1`, correct `Text` |
| | `AddNote(text)` | `AddNote_WithDuplicateText_ThrowsInvariantViolation` | `InvariantViolationException` |
| | `AddNote(text)` | `AddNote_WithDuplicateTextDifferentCase_ThrowsInvariantViolation` | `InvariantViolationException` (case-insensitive) |
| **NoteTests** | `Note(columnId, text)` | `Constructor_WithValidArgs_SetsProperties` | `ColumnId`, `Text` set |
| | `Note(columnId, text)` | `Constructor_WithEmptyText_ThrowsArgumentException` | `ArgumentException` |
| | `CastVote(userId)` | `CastVote_WithNewUser_AddsVoteToCollection` | `Votes.Count == 1`, correct `UserId` |
| | `CastVote(userId)` | `CastVote_WithDuplicateUser_ThrowsInvariantViolation` | `InvariantViolationException` |
| | `RemoveVote(voteId)` | `RemoveVote_WithExistingVote_RemovesVote` | `Votes.Count == 0` |
| | `RemoveVote(voteId)` | `RemoveVote_WithNonExistingVote_ThrowsDomainException` | `DomainException` |
| | `UpdateText(newText)` | `UpdateText_WithValidText_UpdatesText` | `Text` updated |
| | `UpdateText(newText)` | `UpdateText_WithWhitespaceText_ThrowsArgumentException` | `ArgumentException` |
| **VoteTests** | `Vote(noteId, userId)` | `Constructor_WithValidArgs_SetsProperties` | `NoteId`, `UserId` set |

**Total: ~29 tests** (3 User + 5 Project + 5 RetroBoard + 7 Column + 8 Note + 1 Vote)

### 4.2 API 3 — Aggregate Root Tests

All column/note/vote operations go through the `RetroBoard` aggregate root.
Individual entity test classes are unnecessary — although child entity
constructors are technically `public`, the aggregate root is the designed
entry point for all mutations. Tests should exercise the aggregate root's
public methods (e.g., `RetroBoard.AddColumn()`), not construct child
entities directly.

| Class | Method Under Test | Test Name | Asserts |
|-------|------------------|-----------|---------|
| **UserTests** | `User(name, email)` | `Constructor_WithValidArgs_SetsProperties` | `Name`, `Email` set |
| | `User(name, email)` | `Constructor_WithNullName_ThrowsArgumentException` | `ArgumentException` |
| | `User(name, email)` | `Constructor_WithEmptyEmail_ThrowsArgumentException` | `ArgumentException` |
| **ProjectTests** | `Project(name)` | `Constructor_WithValidName_SetsName` | `Name` set |
| | `Project(name)` | `Constructor_WithWhitespaceName_ThrowsArgumentException` | `ArgumentException` |
| | `AddMember(userId)` | `AddMember_WithNewUser_AddsMemberToCollection` | `Members.Count == 1` |
| | `AddMember(userId)` | `AddMember_WithDuplicateUser_ThrowsInvariantViolation` | `InvariantViolationException` |
| | `RemoveMember(userId)` | `RemoveMember_WithExistingMember_RemovesMember` | `Members.Count == 0` |
| | `RemoveMember(userId)` | `RemoveMember_WithNonExistingUser_ThrowsDomainException` | `DomainException` |
| **RetroBoardTests** | `RetroBoard(projectId, name)` | `Constructor_WithValidArgs_SetsProperties` | `ProjectId`, `Name` set |
| | `RetroBoard(projectId, name)` | `Constructor_WithEmptyName_ThrowsArgumentException` | `ArgumentException` |
| | `AddColumn(name)` | `AddColumn_WithUniqueName_AddsColumn` | `Columns.Count == 1` |
| | `AddColumn(name)` | `AddColumn_WithDuplicateName_ThrowsInvariantViolation` | `InvariantViolationException` |
| | `RenameColumn(id, name)` | `RenameColumn_WithUniqueName_UpdatesName` | Column `Name` changed |
| | `RenameColumn(id, name)` | `RenameColumn_WithDuplicateName_ThrowsInvariantViolation` | `InvariantViolationException` |
| | `RenameColumn(id, name)` | `RenameColumn_WithNonExistingColumn_ThrowsDomainException` | `DomainException` |
| | `RemoveColumn(id)` | `RemoveColumn_WithExistingColumn_RemovesColumn` | `Columns.Count == 0` |
| | `RemoveColumn(id)` | `RemoveColumn_WithNonExistingColumn_ThrowsDomainException` | `DomainException` |
| | `AddNote(columnId, text)` | `AddNote_WithUniqueText_AddsNote` | Column's `Notes.Count == 1` |
| | `AddNote(columnId, text)` | `AddNote_WithDuplicateText_ThrowsInvariantViolation` | `InvariantViolationException` |
| | `AddNote(columnId, text)` | `AddNote_ToNonExistingColumn_ThrowsDomainException` | `DomainException` |
| | `UpdateNote(columnId, noteId, text)` | `UpdateNote_WithValidText_UpdatesText` | Note `Text` changed |
| | `RemoveNote(columnId, noteId)` | `RemoveNote_WithExistingNote_RemovesNote` | Column's `Notes.Count == 0` |
| | `CastVote(columnId, noteId, userId)` | `CastVote_WithNewUser_AddsVote` | Note's `Votes.Count == 1` |
| | `CastVote(columnId, noteId, userId)` | `CastVote_WithDuplicateUser_ThrowsInvariantViolation` | `InvariantViolationException` |
| | `RemoveVote(columnId, noteId, voteId)` | `RemoveVote_WithExistingVote_RemovesVote` | Note's `Votes.Count == 0` |

**Total: ~25 tests** (3 User + 5 Project + 17 RetroBoard)

### 4.3 API 4 — Split Aggregate Tests

Same as API 3, **minus** vote operations on `RetroBoard` (Vote is its own
aggregate). Vote construction is tested separately.

| Class | Method Under Test | Test Name | Asserts |
|-------|------------------|-----------|---------|
| **UserTests** | Same as API 3 | 3 tests | — |
| **ProjectTests** | Same as API 3 | 5 tests | — |
| **RetroBoardTests** | `RetroBoard(projectId, name)` | `Constructor_WithValidArgs_SetsProperties` | `ProjectId`, `Name` set |
| | `RetroBoard(projectId, name)` | `Constructor_WithEmptyName_ThrowsArgumentException` | `ArgumentException` |
| | `AddColumn(name)` | `AddColumn_WithUniqueName_AddsColumn` | `Columns.Count == 1` |
| | `AddColumn(name)` | `AddColumn_WithDuplicateName_ThrowsInvariantViolation` | `InvariantViolationException` |
| | `RenameColumn(id, name)` | `RenameColumn_WithUniqueName_UpdatesName` | Column `Name` changed |
| | `RenameColumn(id, name)` | `RenameColumn_WithDuplicateName_ThrowsInvariantViolation` | `InvariantViolationException` |
| | `RenameColumn(id, name)` | `RenameColumn_WithNonExistingColumn_ThrowsDomainException` | `DomainException` |
| | `RemoveColumn(id)` | `RemoveColumn_WithExistingColumn_RemovesColumn` | `Columns.Count == 0` |
| | `RemoveColumn(id)` | `RemoveColumn_WithNonExistingColumn_ThrowsDomainException` | `DomainException` |
| | `AddNote(columnId, text)` | `AddNote_WithUniqueText_AddsNote` | Column's `Notes.Count == 1` |
| | `AddNote(columnId, text)` | `AddNote_WithDuplicateText_ThrowsInvariantViolation` | `InvariantViolationException` |
| | `AddNote(columnId, text)` | `AddNote_ToNonExistingColumn_ThrowsDomainException` | `DomainException` |
| | `UpdateNote(columnId, noteId, text)` | `UpdateNote_WithValidText_UpdatesText` | Note `Text` changed |
| | `RemoveNote(columnId, noteId)` | `RemoveNote_WithExistingNote_RemovesNote` | Column's `Notes.Count == 0` |
| **VoteTests** | `Vote(noteId, userId)` | `Constructor_WithValidArgs_SetsProperties` | `NoteId`, `UserId` set |

**Total: ~23 tests** (3 User + 5 Project + 14 RetroBoard + 1 Vote)

### 4.4 API 5 — Behavioral + Domain Event Tests

Same aggregate behavior as API 4, **plus** assertions that domain events are
raised correctly. API 5 aggregates inherit from `AggregateRoot` which exposes
a `DomainEvents` collection.

| Class | Additional Event Tests | Asserts |
|-------|----------------------|---------|
| **RetroBoardTests** | `AddColumn` raises `ColumnAddedEvent` | `DomainEvents` contains event with correct `RetroBoardId`, `ColumnId`, `ColumnName` |
| | `AddNote` raises `NoteAddedEvent` | `DomainEvents` contains event with correct `RetroBoardId`, `ColumnId`, `NoteId` |
| | `RemoveNote` raises `NoteRemovedEvent` | `DomainEvents` contains event with correct `NoteId`, `ColumnId` |
| **ProjectTests** | `AddMember` raises `MemberAddedToProjectEvent` | `DomainEvents` contains event with correct `ProjectId`, `UserId`, `MembershipId` |
| | `RemoveMember` raises `MemberRemovedFromProjectEvent` | `DomainEvents` contains event with correct `ProjectId`, `UserId` |
| **VoteTests** | Constructor raises `VoteCastEvent` | `DomainEvents` contains event with correct `VoteId`, `NoteId`, `UserId` |

**Total: ~29 tests** (23 from API 4 behavior + 6 event-specific)

---

## 5. Implementation Phases

### Phase 1 — Project Scaffolding ✅ Done

Set up the four `.csproj` files and register them in the solution.

| # | Task | Status |
|---|------|--------|
| 1.1 | Create `tests/Api2.Domain.UnitTests/Api2.Domain.UnitTests.csproj` | ✅ Done |
| 1.2 | Create `tests/Api3.Domain.UnitTests/Api3.Domain.UnitTests.csproj` | ✅ Done |
| 1.3 | Create `tests/Api4.Domain.UnitTests/Api4.Domain.UnitTests.csproj` | ✅ Done |
| 1.4 | Create `tests/Api5.Domain.UnitTests/Api5.Domain.UnitTests.csproj` | ✅ Done |
| 1.5 | Add all four projects to `RetroBoard.slnx` under the `/tests/` folder | ✅ Done |
| 1.6 | Run `dotnet build` — verify all projects compile with zero errors | ✅ Done |

#### .csproj Template

All four projects follow this template (substitute `ApiN.Domain` accordingly).
Package versions are managed centrally in `Directory.Packages.props` — do NOT
add `Version` attributes. `TargetFramework`, `Nullable`, `ImplicitUsings` are
inherited from `Directory.Build.props`.

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Api2.RichDomain\Api2.Domain\Api2.Domain.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
    <PackageReference Include="FluentAssertions" />
  </ItemGroup>

</Project>
```

Project reference paths per test project:

| Test Project | ProjectReference Path |
|---|---|
| `Api2.Domain.UnitTests` | `..\..\src\Api2.RichDomain\Api2.Domain\Api2.Domain.csproj` |
| `Api3.Domain.UnitTests` | `..\..\src\Api3.Aggregates\Api3.Domain\Api3.Domain.csproj` |
| `Api4.Domain.UnitTests` | `..\..\src\Api4.SplitAggregates\Api4.Domain\Api4.Domain.csproj` |
| `Api5.Domain.UnitTests` | `..\..\src\Api5.Behavioral\Api5.Domain\Api5.Domain.csproj` |

#### Namespace Convention

Test class namespaces mirror the project name: `Api2.Domain.UnitTests`,
`Api3.Domain.UnitTests`, etc. No sub-namespaces — all test files are in
the project root folder.

#### RetroBoard.slnx Format

The solution uses the XML-based `.slnx` format. Add the four new projects
inside the existing `/tests/` folder node:

```xml
<Project Path="tests/Api2.Domain.UnitTests/Api2.Domain.UnitTests.csproj" />
<Project Path="tests/Api3.Domain.UnitTests/Api3.Domain.UnitTests.csproj" />
<Project Path="tests/Api4.Domain.UnitTests/Api4.Domain.UnitTests.csproj" />
<Project Path="tests/Api5.Domain.UnitTests/Api5.Domain.UnitTests.csproj" />
```

### Phase 2 — API 2 Unit Tests ✅ Done

Write tests for the rich entity behavior introduced in API 2.

| # | Task | Status |
|---|------|--------|
| 2.1 | Create `tests/Api2.Domain.UnitTests/UserTests.cs` — constructor guards (3 tests) | ✅ Done |
| 2.2 | Create `tests/Api2.Domain.UnitTests/ProjectTests.cs` — constructor + `AddMember` + `RemoveMember` (5 tests) | ✅ Done |
| 2.3 | Create `tests/Api2.Domain.UnitTests/RetroBoardTests.cs` — constructor + `AddColumn` (5 tests) | ✅ Done |
| 2.4 | Create `tests/Api2.Domain.UnitTests/ColumnTests.cs` — constructor + `Rename` + `AddNote` (7 tests) | ✅ Done |
| 2.5 | Create `tests/Api2.Domain.UnitTests/NoteTests.cs` — constructor + `CastVote` + `RemoveVote` + `UpdateText` (8 tests) | ✅ Done |
| 2.6 | Create `tests/Api2.Domain.UnitTests/VoteTests.cs` — constructor (1 test) | ✅ Done |
| 2.7 | Run Api2 unit tests — verify all ~29 tests pass | ✅ Done |

### Phase 3 — API 3 Unit Tests ✅ Done

Write tests for aggregate root behavior. All operations go through `RetroBoard` or `Project`.

| # | Task | Status |
|---|------|--------|
| 3.1 | Create `tests/Api3.Domain.UnitTests/UserTests.cs` — constructor guards (3 tests) | ✅ Done |
| 3.2 | Create `tests/Api3.Domain.UnitTests/ProjectTests.cs` — constructor + `AddMember` + `RemoveMember` (5 tests) | ✅ Done |
| 3.3 | Create `tests/Api3.Domain.UnitTests/RetroBoardTests.cs` — all column/note/vote aggregate operations (~17 tests) | ✅ Done |
| 3.4 | Run Api3 unit tests — verify all ~25 tests pass | ✅ Done |

### Phase 4 — API 4 Unit Tests ✅ Done

Same as API 3 minus vote operations on RetroBoard; Vote is its own aggregate.

| # | Task | Status |
|---|------|--------|
| 4.1 | Create `tests/Api4.Domain.UnitTests/UserTests.cs` — constructor guards (3 tests) | ✅ Done |
| 4.2 | Create `tests/Api4.Domain.UnitTests/ProjectTests.cs` — constructor + `AddMember` + `RemoveMember` (5 tests) | ✅ Done |
| 4.3 | Create `tests/Api4.Domain.UnitTests/RetroBoardTests.cs` — column + note operations only (~14 tests) | ✅ Done |
| 4.4 | Create `tests/Api4.Domain.UnitTests/VoteTests.cs` — constructor (1 test) | ✅ Done |
| 4.5 | Run Api4 unit tests — verify all ~23 tests pass | ✅ Done |

### Phase 5 — API 5 Unit Tests ✅ Done

Same behavior as API 4, plus domain event assertions.

| # | Task | Status |
|---|------|--------|
| 5.1 | Create `tests/Api5.Domain.UnitTests/UserTests.cs` — constructor guards (3 tests) | ✅ Done |
| 5.2 | Create `tests/Api5.Domain.UnitTests/ProjectTests.cs` — constructor + membership + event assertions (~7 tests) | ✅ Done |
| 5.3 | Create `tests/Api5.Domain.UnitTests/RetroBoardTests.cs` — column/note operations + event assertions (~20 tests) | ✅ Done |
| 5.4 | Create `tests/Api5.Domain.UnitTests/VoteTests.cs` — constructor + `VoteCastEvent` assertion (2 tests) | ✅ Done |
| 5.5 | Run Api5 unit tests — verify all ~29 tests pass | ✅ Done |

### Phase 6 — Documentation Updates ✅ Done

Update all documentation to reflect the new unit test projects and strategy.

| # | Task | File(s) | Status |
|---|------|---------|--------|
| 6.1 | Create new `docfx/testing/unit-tests.md` — full explanation of why unit tests exist, what they cover, why API 1 is excluded, comparison table across tiers | `docfx/testing/unit-tests.md` | ✅ Done |
| 6.2 | Create new `docfx/testing/why-no-mocking.md` — full analysis of why mocking is not used, comparison of handler vs. service code, when mocking IS justified, further reading | `docfx/testing/why-no-mocking.md` | ✅ Done |
| 6.3 | Update `docfx/testing/toc.yml` — add entries for `unit-tests.md` and `why-no-mocking.md` | `docfx/testing/toc.yml` | ✅ Done |
| 6.4 | Update `docfx/testing/index.md` — add bullets for both new guides | `docfx/testing/index.md` | ✅ Done |
| 6.5 | Update `docfx/testing/test-infrastructure.md` — add section clarifying both test types (unit vs. integration) and that unit tests have zero infrastructure dependencies | `docfx/testing/test-infrastructure.md` | ✅ Done |
| 6.6 | Update `docfx/migration/api2-rich-domain.md` — add "Testability" section highlighting that rich entities are unit testable | `docfx/migration/api2-rich-domain.md` | ✅ Done |
| 6.7 | Update `docfx/concepts/rich-domain-models.md` — add "Testability" section explaining pure invariant methods are unit testable without mocking | `docfx/concepts/rich-domain-models.md` | ✅ Done |
| 6.8 | Update `docfx/concepts/invariants.md` — add note/row about unit testing invariants directly | `docfx/concepts/invariants.md` | ✅ Done |
| 6.9 | Update `docfx/migration/index.md` — add "Unit tests?" row to the Quick Comparison table | `docfx/migration/index.md` | ✅ Done |
| 6.10 | Update `README.md` — update Testing Strategy section and Repository Structure to include unit test projects | `README.md` | ✅ Done |
| 6.11 | Update `.github/copilot-instructions.md` — update Testing Rules and Naming Conventions to cover unit tests | `.github/copilot-instructions.md` | ✅ Done |
| 6.12 | Update `docs/DesignDecisions.md` — add Section 12 documenting the two-layer testing strategy and the no-mocking decision | `docs/DesignDecisions.md` | ✅ Done |

### Phase 7 — Final Verification ✅ Done

| # | Task | Status |
|---|------|--------|
| 7.1 | Run `dotnet build` for entire solution — zero errors | ✅ Done |
| 7.2 | Run all unit tests across all four projects — all pass | ✅ Done |
| 7.3 | Verify integration tests still pass (no regressions) | ✅ Done |

---

## 6. Design Decisions

### No Shared Base Class for Unit Tests

Integration tests use shared base classes (`CrudTestsBase`, `InvariantTestsBase`)
because all five APIs expose the **same HTTP contract**. Unit tests exercise
**API-specific domain APIs** that differ between tiers (per-entity methods in
API 2 vs. aggregate root methods in API 3+ vs. domain events in API 5). A shared
base would force artificial generalization.

### No Mocking Framework — Anywhere in the Test Suite

This decision was explicitly evaluated and applies to the **entire** test
suite, not just domain unit tests. The repository does not use Moq,
NSubstitute, or any other mocking framework. The full rationale is documented
in `docs/DesignDecisions.md` (Section 12) and `docfx/testing/why-no-mocking.md`.

**Summary of the reasoning:**

1. **Domain unit tests don't need mocks** — entities have zero external
   dependencies. Every test is arrange → act → assert on plain C# objects.

2. **Mock-based handler/service tests were explicitly considered and rejected.**
   The application-layer handlers (API 5) and services (API 2–4) are thin
   orchestrators that follow a linear load → delegate → save pattern. A
   mock-based test for these would verify implementation details ("did you
   call `GetByIdAsync`?") rather than behavior — the classic brittle-mock
   anti-pattern.

3. **The argument that "only API 5 handlers would benefit" does not hold.**
   `Api5.CastVoteCommandHandler.Handle` is nearly line-for-line identical to
   `Api4.VoteService.CastVoteAsync`. MediatR dispatch does not make handler
   code inherently more mock-worthy than service code. If mocking were
   justified for API 5, it would be equally justified for API 2–4.

4. **Query handlers must not be mocked.** API 5's query handlers use
   `DbContext` directly with LINQ-to-SQL. Mocking `DbContext`/`DbSet` is a
   well-known anti-pattern — the EF Core team recommends testing queries
   against a real database.

5. **Integration tests already cover orchestration.** The full HTTP → DB
   round-trip tests catch wiring bugs (wrong DI registration, missing
   `Include`, broken query filter) that mock-based tests would miss.

6. **Educational clarity.** Introducing mocks risks teaching "mock everything"
   rather than the intended lesson: design your domain so interesting logic
   lives in pure functions, and verify wiring with end-to-end tests.

**When mocking IS justified** (outside this repo): significant branching logic
in handlers, external service dependencies (HTTP clients, queues), complex
multi-aggregate sagas, or prohibitively slow integration test suites.

### Test Project Dependencies

Each unit test project references **only** its corresponding `ApiN.Domain`
project. There are no references to Application, Infrastructure, or WebApi
layers. This reinforces the Clean Architecture dependency rule: the domain is
independently testable.

```
Api2.Domain.UnitTests  ──references──►  Api2.Domain
Api3.Domain.UnitTests  ──references──►  Api3.Domain
Api4.Domain.UnitTests  ──references──►  Api4.Domain
Api5.Domain.UnitTests  ──references──►  Api5.Domain
```

### API 1 Is Deliberately Excluded

API 1's entities are anemic property bags. A unit test for `user.Name = "Alice"`
would test C# auto-properties — not domain logic. Excluding API 1 reinforces
the educational message: **anemic models have nothing to unit test**. This
contrast makes the value of rich domain models tangible.

### Constructor Accessibility in API 3+

In API 3+, child entity constructors (Column, Note, Vote) are `public` — they
are not `internal`. However, the **designed entry point** for mutations is the
aggregate root. Unit tests should exercise child entities **through** the
aggregate root's public methods (e.g., `RetroBoard.AddColumn()`, not
`new Column(...)` directly). This tests the real usage pattern and validates
that the aggregate root correctly delegates to child entities.

---

## 7. Educational Value Summary

| Tier | What Unit Tests Demonstrate |
|------|-----------------------------|
| API 2 | Rich entities enforce their own invariants — no service needed to test them. But each entity is tested in isolation because there are no aggregates. |
| API 3 | Aggregate roots are the **only** entry point for mutations. Tests go through `RetroBoard.AddColumn()`, not `new Column()`. This proves the aggregate boundary works. |
| API 4 | Splitting Vote into its own aggregate means RetroBoard tests no longer cover vote logic — Vote has its own test class. This shows the trade-off of smaller aggregates. |
| API 5 | Domain events are first-class outputs of domain operations. Tests assert that the right events are raised — proving the domain is expressive and decoupled from side effects. |
