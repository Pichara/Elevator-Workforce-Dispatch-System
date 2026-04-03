using ElevatorMaintenanceSystem.Data;
using ElevatorMaintenanceSystem.Infrastructure;
using ElevatorMaintenanceSystem.Models;
using ElevatorMaintenanceSystem.Services;
using MongoDB.Driver;
using Xunit;

namespace ElevatorMaintenanceSystem.Tests.Services;

public class WorkerServiceTests
{
    [Fact]
    public async Task DeactivateAsync_SetsUnavailableAndDeletedAt()
    {
        var repository = new InMemoryWorkerRepository();
        var worker = repository.Seed(new Worker
        {
            FullName = "Ava Stone",
            Email = "ava@example.com",
            PhoneNumber = "555-0001",
            Skills = ["Repair", "Inspection"],
            AvailabilityStatus = WorkerAvailabilityStatus.Available,
            Location = new GpsCoordinateValidator().CreatePoint(34.0522, -118.2437)
        });

        var service = new WorkerService(repository, new GpsCoordinateValidator());

        var deactivated = await service.DeactivateAsync(worker.Id);

        Assert.Equal(WorkerAvailabilityStatus.Unavailable, deactivated.AvailabilityStatus);
        Assert.True(deactivated.DeletedAt.HasValue);
        Assert.Equal(deactivated.DeletedAt, deactivated.UpdatedAt);
    }

    [Fact]
    public async Task UpdateLocationAsync_UpdatesCoordinatesAndTimestamp()
    {
        var repository = new InMemoryWorkerRepository();
        var worker = repository.Seed(new Worker
        {
            FullName = "Ben Lee",
            Email = "ben@example.com",
            PhoneNumber = "555-0002",
            Skills = ["Install"],
            AvailabilityStatus = WorkerAvailabilityStatus.Assigned,
            UpdatedAt = DateTime.UtcNow.AddMinutes(-10),
            Location = new GpsCoordinateValidator().CreatePoint(29.7604, -95.3698)
        });
        var originalUpdatedAt = worker.UpdatedAt;

        var service = new WorkerService(repository, new GpsCoordinateValidator());

        var updated = await service.UpdateLocationAsync(worker.Id, 47.6062, -122.3321);

        Assert.Equal(47.6062, updated.Location.Coordinates.Latitude, 4);
        Assert.Equal(-122.3321, updated.Location.Coordinates.Longitude, 4);
        Assert.True(updated.UpdatedAt > originalUpdatedAt);
    }

    [Fact]
    public async Task UpdateLocationAsync_Throws_WhenCoordinatesAreInvalid()
    {
        var repository = new InMemoryWorkerRepository();
        var worker = repository.Seed(new Worker
        {
            FullName = "Cara Jones",
            Email = "cara@example.com",
            PhoneNumber = "555-0003",
            Skills = ["Repair"],
            AvailabilityStatus = WorkerAvailabilityStatus.Available,
            Location = new GpsCoordinateValidator().CreatePoint(37.7749, -122.4194)
        });

        var service = new WorkerService(repository, new GpsCoordinateValidator());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.UpdateLocationAsync(worker.Id, 200, -122.4194));
    }

    private sealed class InMemoryWorkerRepository : IWorkerRepository
    {
        public List<Worker> Items { get; } = new();

        public Worker Seed(Worker worker)
        {
            Items.Add(worker);
            return worker;
        }

        public Task<Worker?> GetByIdAsync(Guid id)
        {
            return Task.FromResult(Items.FirstOrDefault(worker => worker.Id == id));
        }

        public Task<IEnumerable<Worker>> GetAllAsync()
        {
            return Task.FromResult<IEnumerable<Worker>>(Items.ToList());
        }

        public Task<IEnumerable<Worker>> FindAsync(FilterDefinition<Worker> filter)
        {
            throw new NotSupportedException();
        }

        public Task AddAsync(Worker entity)
        {
            Items.Add(entity);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Worker entity)
        {
            var index = Items.FindIndex(worker => worker.Id == entity.Id);
            if (index >= 0)
            {
                Items[index] = entity;
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id)
        {
            Items.RemoveAll(worker => worker.Id == id);
            return Task.CompletedTask;
        }

        public Task<long> CountAsync(FilterDefinition<Worker>? filter = null)
        {
            return Task.FromResult((long)Items.Count);
        }

        public Task<IEnumerable<Worker>> GetActiveAsync()
        {
            return Task.FromResult<IEnumerable<Worker>>(Items.Where(worker => worker.DeletedAt == null).ToList());
        }
    }
}
