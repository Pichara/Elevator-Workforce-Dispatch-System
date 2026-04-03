using ElevatorMaintenanceSystem.Data;
using ElevatorMaintenanceSystem.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;

namespace ElevatorMaintenanceSystem.Infrastructure;

/// <summary>
/// Hosted service to initialize database and create indexes on startup
/// </summary>
public class DatabaseInitializer : IHostedService
{
    private readonly IMongoDbContext _context;
    private readonly MongoDbSettings _settings;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(
        IMongoDbContext context,
        IOptions<MongoDbSettings> settings,
        ILogger<DatabaseInitializer> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _settings = settings.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Initializing MongoDB database: {DatabaseName}", _settings.DatabaseName);

            // Test connection
            var command = new JsonCommand<BsonDocument>("{ ping: 1 }");
            await _context.Database.RunCommandAsync(command);
            _logger.LogInformation("Successfully connected to MongoDB");

            // Create collections if they don't exist
            await CreateCollectionsAsync(cancellationToken);

            // Create indexes
            await CreateIndexesAsync(cancellationToken);

            _logger.LogInformation("Database initialization completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize MongoDB database");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task CreateCollectionsAsync(CancellationToken cancellationToken)
    {
        var cursor = await _context.Database.ListCollectionNamesAsync(cancellationToken: cancellationToken);
        var collectionNames = await cursor.ToListAsync(cancellationToken);

        // Define collection names - these will be created when documents are first inserted
        // We pre-create them here to set up indexes
        var requiredCollections = new[]
        {
            "elevators",
            "workers",
            "tickets"
        };

        foreach (var collectionName in requiredCollections)
        {
            if (!collectionNames.Contains(collectionName))
            {
                await _context.Database.CreateCollectionAsync(collectionName, cancellationToken: cancellationToken);
                _logger.LogInformation("Created collection: {CollectionName}", collectionName);
            }
        }
    }

    private async Task CreateIndexesAsync(CancellationToken cancellationToken)
    {
        // Create 2dsphere index for GPS coordinates on Elevators collection
        var elevatorsCollection = _context.Database.GetCollection<Elevator>("elevators");
        var elevatorsIndexModel = new CreateIndexModel<Elevator>(
            Builders<Elevator>.IndexKeys.Geo2DSphere(x => x.Location),
            new CreateIndexOptions { Name = "location_2dsphere" });

        await elevatorsCollection.Indexes.CreateOneAsync(elevatorsIndexModel);
        _logger.LogInformation("Created 2dsphere index on Elevators.Location");

        // Create 2dsphere index for GPS coordinates on Workers collection
        var workersCollection = _context.Database.GetCollection<Worker>("workers");
        var workersIndexModel = new CreateIndexModel<Worker>(
            Builders<Worker>.IndexKeys.Geo2DSphere(x => x.Location),
            new CreateIndexOptions { Name = "location_2dsphere" });

        await workersCollection.Indexes.CreateOneAsync(workersIndexModel);
        _logger.LogInformation("Created 2dsphere index on Workers.Location");

        // Create standard indexes for common queries
        await CreateStandardIndexesAsync(cancellationToken);
    }

    private async Task CreateStandardIndexesAsync(CancellationToken cancellationToken)
    {
        // Index for soft-delete queries
        var elevatorsCollection = _context.Database.GetCollection<Elevator>("elevators");
        var deletedAtIndex = Builders<Elevator>.IndexKeys.Ascending(x => x.DeletedAt);
        await elevatorsCollection.Indexes.CreateOneAsync(
            new CreateIndexModel<Elevator>(deletedAtIndex, new CreateIndexOptions { Name = "deleted_at_idx" }));

        var workersCollection = _context.Database.GetCollection<Worker>("workers");
        await workersCollection.Indexes.CreateOneAsync(
            new CreateIndexModel<Worker>(Builders<Worker>.IndexKeys.Ascending(x => x.DeletedAt),
                new CreateIndexOptions { Name = "deleted_at_idx" }));

        var ticketsCollection = _context.Database.GetCollection<Ticket>("tickets");
        await ticketsCollection.Indexes.CreateOneAsync(
            new CreateIndexModel<Ticket>(Builders<Ticket>.IndexKeys.Ascending(x => x.DeletedAt),
                new CreateIndexOptions { Name = "deleted_at_idx" }));

        _logger.LogInformation("Created standard indexes for queries");
    }
}

/// <summary>
/// Placeholder entities for index creation (will be replaced by actual models in Phase 2)
/// </summary>
public class Elevator : BaseDocument
{
    public GeoJsonPoint<GeoJson2DGeographicCoordinates> Location { get; set; } = null!;
}

public class Worker : BaseDocument
{
    public GeoJsonPoint<GeoJson2DGeographicCoordinates> Location { get; set; } = null!;
}

public class Ticket : BaseDocument
{
}
