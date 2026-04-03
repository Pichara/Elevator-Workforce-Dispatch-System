using ElevatorMaintenanceSystem.Infrastructure;
using MongoDB.Driver;

namespace ElevatorMaintenanceSystem.Data;

/// <summary>
/// MongoDB context providing access to database and collections
/// </summary>
public interface IMongoDbContext : IDisposable
{
    IMongoDatabase Database { get; }
}

/// <summary>
/// MongoDB context implementation
/// </summary>
public class MongoDbContext : IMongoDbContext
{
    private readonly MongoClient _client;
    private readonly MongoDbSettings _settings;

    public MongoDbContext(MongoDbSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _client = new MongoClient(_settings.ConnectionString);
    }

    public IMongoDatabase Database => _client.GetDatabase(_settings.DatabaseName);

    public void Dispose()
    {
        // MongoClient handles connection pooling automatically
        // No need to dispose in this implementation
    }
}
