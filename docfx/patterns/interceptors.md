# Interceptor Pattern (EF Core)

## Intent

Intercept EF Core's `SaveChanges` pipeline to apply cross-cutting behavior
**automatically** — without modifying entity code or service code.

## Interceptors in RetroBoard

### AuditInterceptor

Automatically stamps `CreatedAt`, `LastUpdatedAt`, and handles soft delete:

```csharp
public class AuditInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(...)
    {
        foreach (var entry in context.ChangeTracker.Entries<AuditableEntityBase>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.LastUpdatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.LastUpdatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Deleted:
                    entry.State = EntityState.Modified;        // Convert to update
                    entry.Entity.DeletedAt = DateTime.UtcNow;  // Set soft delete flag
                    break;
            }
        }
        return base.SavingChangesAsync(...);
    }
}
```

### DomainEventInterceptor (API 5 only)

Dispatches domain events after save. See [Domain Events](domain-events.md).

## Registration

Interceptors are registered as **singletons** and added to the DbContext:

```csharp
builder.Services.AddSingleton<AuditInterceptor>();

builder.Services.AddDbContext<RetroBoardDbContext>((sp, options) =>
    options
        .UseNpgsql(connectionString)
        .AddInterceptors(sp.GetRequiredService<AuditInterceptor>()));
```

## Why Interceptors Instead of Overriding SaveChanges?

Both work. Interceptors are preferred because:
- They're composable (add/remove without modifying DbContext).
- They're registered via DI (can have dependencies).
- They separate concerns (auditing, soft delete, events — each in its own class).
