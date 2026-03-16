# Migration Path

This section walks through each API tier, explaining **what changes**, **why**,
and **what trade-offs** are introduced. Read them in order to follow the
progressive evolution from anemic CRUD to DDD + CQRS.

## The Journey

### Main Path (Progressive DDD Evolution)

```
API 1: Anemic CRUD         "This is what juniors write"
  │
  ▼  Move logic into entities
API 2: Rich Domain          "Better, but no consistency boundary"
  │
  ▼  Introduce aggregates + optimistic concurrency
API 3: Aggregates           "Consistency solved, but aggregate is too big"
  │
  ▼  Extract Vote as its own aggregate
API 4: Split Aggregates     "Right-sized aggregates, but cross-agg complexity"
  │
  ▼  Separate reads from writes, add MediatR
API 5: CQRS + MediatR       "Behavior-centric, scalable, but more indirection"
```

### Parallel Track (Transaction Script)

```
Api0a: Transaction Script             "Simple. Same concurrency failures as API 1/2."
  │
  ▼  Add xmin tokens + catch DB exceptions (~35 lines of diff)
Api0b: Transaction Script + DB         "Same simplicity. Concurrency fixed."
       Concurrency Safety
```

> API 0 is **not** part of the linear path. It answers the question:
> *"What if I just kept it simple?"* See [API 0 — Transaction Script](api0-transaction-script.md).

## Tier Guides

- [API 0 — Transaction Script](api0-transaction-script.md) *(parallel track)*
1. [API 1 — Anemic CRUD](api1-anemic-crud.md)
2. [API 2 — Rich Domain Models](api2-rich-domain.md)
3. [API 3 — Aggregate Design](api3-aggregates.md)
4. [API 4 — Split Aggregates](api4-split-aggregates.md)
5. [API 5 — CQRS + MediatR](api5-cqrs-mediatr.md)

## Quick Comparison

| Metric | Api0a | Api0b | API 1 | API 2 | API 3 | API 4 | API 5 |
|--------|-------|-------|-------|-------|-------|-------|-------|
| Projects | 1 | 1 | 4 | 4 | 4 | 4 | 4 |
| API style | Minimal APIs | Minimal APIs | Controllers | Controllers | Controllers | Controllers | Controllers |
| Repository count | 0 | 0 | 7 | 7 | 3 | 4 | 4 |
| Service/Handler count | 0 | 0 | 7 | 7 | 3 | 4 | 13+ handlers |
| Business logic in | Endpoint handlers | Endpoint handlers | Services | Entities | Aggregate roots | Aggregate roots | Handlers + Domain |
| Concurrency safe? | ❌ | ✅ (DB) | ❌ | ❌ | ✅ | ✅ | ✅ |
| Consistency boundary | ❌ None | ❌ None | ❌ None | ❌ None | ✅ Aggregate | ✅ Aggregate | ✅ Aggregate |
| Reads optimized? | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ (CQRS) |
| Voting rules | Hardcoded | Hardcoded | Hardcoded | Hardcoded | Hardcoded | Hardcoded | ✅ Configurable (Strategy) |
| Configuration | Hardcoded | Hardcoded | Hardcoded | Hardcoded | Hardcoded | Hardcoded | ✅ Options pattern |
| Vote uniqueness | App check | DB constraint | App check | App check | App check | DB unique index | Conditional (Options) |
| Transaction scope | Implicit | Implicit | Implicit | Implicit | Implicit | Implicit | Explicit (`TransactionBehavior`) |
| Unit tests? | ❌ None | ❌ None | ❌ None | ✅ ~29 | ✅ ~25 | ✅ ~23 | ✅ ~76 |
