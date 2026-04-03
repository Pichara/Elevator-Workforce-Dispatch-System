using ElevatorMaintenanceSystem.Models;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace ElevatorMaintenanceSystem.Data;

/// <summary>
/// Generic repository implementation for MongoDB operations
/// </summary>
/// <typeparam name="T">Document type implementing IBaseDocument</typeparam>
public class MongoRepository<T> : IMongoRepository<T> where T : IBaseDocument
{
    private readonly IMongoCollection<T> _collection;
    private readonly FilterDefinitionBuilder<T> _filterBuilder = Builders<T>.Filter;

    public MongoRepository(IMongoDbContext context, string collectionName)
    {
        _collection = context.Database.GetCollection<T>(collectionName);
    }

    public async Task<T?> GetByIdAsync(Guid id)
    {
        return await _collection.Find(_filterBuilder.Eq(x => x.Id, id)).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _collection.Find(_filterBuilder.Empty).ToListAsync();
    }

    public async Task<IEnumerable<T>> FindAsync(FilterDefinition<T> filter)
    {
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task AddAsync(T entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        await _collection.InsertOneAsync(entity);
    }

    public async Task UpdateAsync(T entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        await _collection.ReplaceOneAsync(_filterBuilder.Eq(x => x.Id, entity.Id), entity);
    }

    public async Task DeleteAsync(Guid id)
    {
        await _collection.DeleteOneAsync(_filterBuilder.Eq(x => x.Id, id));
    }

    public async Task<long> CountAsync(FilterDefinition<T>? filter = null)
    {
        return filter == null
            ? await _collection.CountDocumentsAsync(_filterBuilder.Empty)
            : await _collection.CountDocumentsAsync(filter);
    }
}
