namespace ElevatorMaintenanceSystem.Infrastructure;

/// <summary>
/// Configuration settings for MongoDB connection
/// </summary>
public class MongoDbSettings
{
    public const string SectionName = "MongoDbSettings";
    public string ConnectionString { get; init; } = string.Empty;
    public string DatabaseName { get; init; } = string.Empty;
}
