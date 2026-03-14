using Api5.Application.Common.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Api5.Infrastructure.Persistence;

/// <summary>
/// Custom model cache key factory that includes the
/// <see cref="VotingOptions.DefaultVotingStrategy"/> in the cache key.
/// </summary>
/// <remarks>
/// DESIGN: EF Core caches the compiled model per <see cref="DbContext"/> type
/// by default. When the same <see cref="RetroBoardDbContext"/> is used with
/// different <see cref="VotingOptions"/> configurations (e.g., Default strategy
/// in one test fixture and Budget strategy in another), each configuration
/// needs its own model because the Vote entity's unique index depends on the
/// configured strategy.
///
/// This factory ensures that different <see cref="VotingOptions.DefaultVotingStrategy"/>
/// values produce different cache keys, causing EF Core to build separate
/// models. Without this, the first model built would be reused for all
/// configurations — breaking the conditional unique index logic in
/// <see cref="RetroBoardDbContext.OnModelCreating"/>.
///
/// In production (single configuration), this has no performance impact —
/// only one model is ever built and cached.
/// </remarks>
public class VotingStrategyModelCacheKeyFactory : IModelCacheKeyFactory
{
    /// <summary>
    /// Creates a cache key that includes the voting strategy configuration.
    /// </summary>
    /// <param name="context">The DbContext instance.</param>
    /// <returns>A cache key incorporating the context type and voting strategy.</returns>
    public object Create(DbContext context)
        => Create(context, false);

    /// <summary>
    /// Creates a cache key that includes the voting strategy configuration
    /// and whether this is a design-time invocation.
    /// </summary>
    /// <param name="context">The DbContext instance.</param>
    /// <param name="designTime">Whether this is a design-time invocation.</param>
    /// <returns>A cache key incorporating the context type, voting strategy, and design-time flag.</returns>
    public object Create(DbContext context, bool designTime)
    {
        if (context is RetroBoardDbContext retroContext)
        {
            return (context.GetType(), retroContext.VotingOptions.DefaultVotingStrategy, designTime);
        }

        return (context.GetType(), designTime);
    }
}
