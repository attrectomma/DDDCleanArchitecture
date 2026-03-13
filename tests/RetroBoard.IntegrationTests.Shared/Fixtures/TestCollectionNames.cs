namespace RetroBoard.IntegrationTests.Shared.Fixtures;

/// <summary>
/// Central place for xUnit collection name constants.
/// Avoids magic strings scattered across test classes.
/// </summary>
public static class TestCollectionNames
{
    /// <summary>
    /// Collection name for all integration tests that need a Postgres database.
    /// </summary>
    public const string IntegrationTests = "Integration Tests";
}
