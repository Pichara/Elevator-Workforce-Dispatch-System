using CommunityToolkit.Mvvm.ComponentModel;
using ElevatorMaintenanceSystem.Data;
using ElevatorMaintenanceSystem.Infrastructure;
using ElevatorMaintenanceSystem.ViewModels;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ElevatorMaintenanceSystem.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IMongoDbContext _context;
    private readonly MongoDbSettings _settings;
    private readonly ILogger<MainViewModel> _logger;

    [ObservableProperty]
    private string _connectionStatus = "Connecting...";

    public MainViewModel(
        IMongoDbContext context,
        MongoDbSettings settings,
        ILogger<MainViewModel> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Test connection on initialization
        _ = TestConnectionAsync();
    }

    private async Task TestConnectionAsync()
    {
        try
        {
            _logger.LogInformation("Testing MongoDB connection...");

            // Run ping command to verify connection
            var command = new JsonCommand<BsonDocument>("{ ping: 1 }");
            var result = await _context.Database.RunCommandAsync(command);

            if (result != null)
            {
                ConnectionStatus = $"Connected to MongoDB: {_settings.DatabaseName}";
                _logger.LogInformation("Successfully connected to MongoDB database: {DatabaseName}", _settings.DatabaseName);
            }
            else
            {
                ConnectionStatus = "Failed to connect to MongoDB";
                _logger.LogWarning("MongoDB ping returned null result");
            }
        }
        catch (Exception ex)
        {
            ConnectionStatus = $"Connection Error: {ex.Message}";
            _logger.LogError(ex, "Failed to connect to MongoDB");
        }
    }
}
