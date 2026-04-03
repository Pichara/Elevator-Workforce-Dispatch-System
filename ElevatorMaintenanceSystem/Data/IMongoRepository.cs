using ElevatorMaintenanceSystem.Models;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace ElevatorMaintenanceSystem.Data;

/// <summary>
/// Generic repository interface for MongoDB operations
/// </summary>
/// <typeparam name="T">Document type implementing IBaseDocument</typeparam>
public interface IMongoRepository<T> where T : IBaseDocument
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(FilterDefinition<T> filter);
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(Guid id);
    Task<long> CountAsync(FilterDefinition<T>? filter = null);
}
