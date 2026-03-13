# Phase 1 — Concurrency and Consistency Boundary Tests Summary

## Overview

This document summarizes the **Concurrency** and **Consistency Boundary** tests for all APIs. These tests are designed to be reusable across all five API tiers via shared abstract base classes, with API-specific test classes providing only fixture wiring.

## Shared Test Base Classes

### `ConcurrencyTestsBase.cs`
Tests that fire **parallel requests** to expose race conditions in the check-then-act pattern.

| Test | What it proves | API 1/2 | API 3+ |
|------|---------------|---------|--------|
| `AddColumn_ConcurrentDuplicateNames_OnlyOneSucceeds` | Concurrent duplicate column creation | ❌ Unhandled `DbUpdateException` | ✅ 409 Conflict |
| `CastVote_ConcurrentDuplicateVotes_OnlyOneSucceeds` | Concurrent duplicate vote creation | ❌ Unhandled `DbUpdateException` | ✅ 409 Conflict |

### `ConsistencyBoundaryTestsBase.cs`
Tests that validate the **scope of transactional consistency** — whether the API understands which entities form a unit.

| Test | Category | What it proves | API 1/2 | API 3+ |
|------|----------|---------------|---------|--------|
| `AddNoteToDeletedColumn_IsRejected` | Baseline | Soft delete hides column from child creation | ✅ Pass | ✅ Pass |
| `UpdateColumn_ToMatchExistingName_IsRejected` | Baseline | Uniqueness enforced during updates | ✅ Pass | ✅ Pass |
| `CastVote_ByNonProjectMember_IsRejected` | Cross-entity | Domain invariant #4: only members may participate | ❌ **201 Created** (no membership check) | ✅ Rejected |
| `CastVote_OnNoteInDeletedColumn_IsRejected` | Cross-entity | Parent lifecycle leak: note accessible after column deleted | ❌ **201 Created** (note not deleted) | ✅ Rejected |

## API 1 Test Results — 22 tests, 18 passing, 4 failing

| Test Suite | Tests | Status | Why |
|------------|-------|--------|-----|
| **CRUD** | 9 | ✅ All pass | Basic create/read/delete works |
| **Invariant** | 4 | ✅ All pass | Service-layer checks work sequentially |
| **Soft Delete** | 3 | ✅ All pass | EF Core global query filter works |
| **Consistency Boundary** | 4 | ⚠️ 2 pass, 2 fail | Baseline tests pass; cross-entity tests fail |
| **Concurrency** | 2 | ❌ Both fail | Race conditions bypass service-layer checks |

## What the Failing Tests Demonstrate

### Cross-Entity Boundary Failures

**`CastVote_ByNonProjectMember_IsRejected`** — The outsider's vote succeeds because `VoteService` only checks: (1) note exists, (2) user exists, (3) no duplicate vote. It never walks the chain User → ProjectMember → Project → RetroBoard → Column → Note. Each service validates its own narrow slice — there is no "owner" of the full boundary.

**`CastVote_OnNoteInDeletedColumn_IsRejected`** — The vote succeeds because `VoteService` queries the note directly via `_noteRepository.GetByIdAsync(noteId)`. The note's own `DeletedAt` is null (only the column was deleted), so the note is found. The service has zero awareness that the note's parent is gone. Each entity is an island.

### Concurrency Failures

**Both concurrency tests** — Parallel requests both pass the "check" step, both proceed to "act", and one hits a DB unique constraint violation. The `DbUpdateException` is unhandled by the middleware, resulting in a 500 instead of a 409.

## Architectural Insight

```
API 1/2 (no aggregate boundary):

  VoteService checks:
    ✓ Note exists?         → yes (Note.DeletedAt is null)
    ✓ User exists?         → yes
    ✓ Already voted?       → no
    → CREATES VOTE ← (no membership check, no parent lifecycle check)

API 3+ (aggregate boundary):

  Command handler:
    ✓ Load RetroBoard aggregate (root + columns + notes + votes)
      └─ Deleted column excluded by query filter → note unreachable
    ✓ Check project membership → outsider rejected
    ✓ Aggregate enforces invariants transactionally
    → Only valid operations succeed
```

## Files Modified

| File | Change |
|------|--------|
| `tests/RetroBoard.IntegrationTests.Shared/Tests/ConsistencyBoundaryTestsBase.cs` | Replaced duplicate invariant tests with genuine cross-entity boundary tests |
| `tests/Api1.IntegrationTests/Tests/Api1ConsistencyBoundaryTests.cs` | Updated remarks to match actual test behaviour |

## Design Decision: Why Not Fix API 1?

These failures are **intentional**. The point is to demonstrate that:
- Service-layer checks only validate the immediate entity
- Cross-entity rules (membership, parent lifecycle) require a coordinating component
- That component is the **aggregate root** (API 3+)
- Adding ad-hoc checks to `VoteService` would be a band-aid, not an architectural solution
