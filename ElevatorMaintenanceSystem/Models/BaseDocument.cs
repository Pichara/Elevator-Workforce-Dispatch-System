using MongoDB.Bson.Serialization.Attributes;

namespace ElevatorMaintenanceSystem.Models;

/// <summary>
/// Base class for all MongoDB documents with sync metadata
/// </summary>
public abstract class BaseDocument : IBaseDocument
{
    [BsonId]
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Update the UpdatedAt timestamp
    /// </summary>
    protected void UpdateTimestamps()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}
