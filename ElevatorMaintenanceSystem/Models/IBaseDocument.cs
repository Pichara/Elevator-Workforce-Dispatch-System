namespace ElevatorMaintenanceSystem.Models;

/// <summary>
/// Base interface for all MongoDB documents with sync metadata for future v2 clients
/// </summary>
public interface IBaseDocument
{
    /// <summary>
    /// Unique identifier (GUID for cross-platform compatibility)
    /// </summary>
    Guid Id { get; set; }

    /// <summary>
    /// Timestamp when the document was created
    /// </summary>
    DateTime CreatedAt { get; set; }

    /// <summary>
    /// Timestamp when the document was last updated
    /// </summary>
    DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Timestamp when the document was soft-deleted (null if active)
    /// </summary>
    DateTime? DeletedAt { get; set; }
}
