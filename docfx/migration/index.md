# Migration Path

This section walks through each API tier, explaining **what changes**, **why**,
and **what trade-offs** are introduced. Read them in order to follow the
progressive evolution from anemic CRUD to DDD + CQRS.

## The Journey

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

## Tier Guides

1. [API 1 — Anemic CRUD](api1-anemic-crud.md)
2. [API 2 — Rich Domain Models](api2-rich-domain.md)
3. [API 3 — Aggregate Design](api3-aggregates.md)
4. [API 4 — Split Aggregates](api4-split-aggregates.md)
5. [API 5 — CQRS + MediatR](api5-cqrs-mediatr.md)

## Quick Comparison

| Metric | API 1 | API 2 | API 3 | API 4 | API 5 |
|--------|-------|-------|-------|-------|-------|
| Repository count | 7 | 7 | 3 | 4 | 4 |
| Service/Handler count | 7 | 7 | 3 | 4 | 13+ handlers |
| Business logic in | Services | Entities | Aggregate roots | Aggregate roots | Handlers + Domain |
| Concurrency safe? | ❌ | ❌ | ✅ | ✅ | ✅ |
| Reads optimized? | ❌ | ❌ | ❌ | ❌ | ✅ (CQRS) |
| Voting rules | Hardcoded | Hardcoded | Hardcoded | Hardcoded | ✅ Configurable (Strategy) |
| Configuration | Hardcoded | Hardcoded | Hardcoded | Hardcoded | ✅ Options pattern |
| Vote uniqueness | App check | App check | App check | DB unique index | Conditional (Options) |
| Unit tests? | ❌ None | ✅ ~29 | ✅ ~25 | ✅ ~23 | ✅ ~76 |
