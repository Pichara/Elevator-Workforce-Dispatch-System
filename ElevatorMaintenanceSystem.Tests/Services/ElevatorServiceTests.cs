using ElevatorMaintenanceSystem.Data;
using ElevatorMaintenanceSystem.Infrastructure;
using ElevatorMaintenanceSystem.Models;
using ElevatorMaintenanceSystem.Services;
using MongoDB.Driver;
using Xunit;

namespace ElevatorMaintenanceSystem.Tests.Services;

public class ElevatorServiceTests
{
    [Fact]
    public async Task CreateAsync_AssignsCoordinatesAndTimestamps()
    {
        var repository = new InMemoryElevatorRepository();
        var service = new ElevatorService(repository, new GpsCoordinateValidator());
        var elevator = new Elevator
        {
            Name = "Tower A",
            Address = "100 Main St",
            BuildingName = "Main Tower",
            FloorLabel = "L1",
            Manufacturer = "Otis",
            InstallationDate = new DateTime(2020, 1, 1),
            IsActive = true
        };

        var created = await service.CreateAsync(elevator, 40.7128, -74.0060);

        Assert.Equal(new DateTime(2020, 1, 1), created.InstallationDate);
        Assert.Equal(40.7128, created.Location.Coordinates.Latitude, 4);
        Assert.Equal(-74.0060, created.Location.Coordinates.Longitude, 4);
        Assert.Equal(created.CreatedAt, created.UpdatedAt);
        Assert.Single(repository.Items);
    }

    [Fact]
    public async Task DeleteInactiveAsync_Throws_WhenElevatorIsActive()
    {
        var repository = new InMemoryElevatorRepository();
        var elevator = repository.Seed(new Elevator
        {
            Name = "Tower A",
            Address = "100 Main St",
            BuildingName = "Main Tower",
            FloorLabel = "L1",
            Manufacturer = "Otis",
            InstallationDate = new DateTime(2020, 1, 1),
            IsActive = true,
            Location = new GpsCoordinateValidator().CreatePoint(40.7128, -74.0060)
        });

        var service = new ElevatorService(repository, new GpsCoordinateValidator());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteInactiveAsync(elevator.Id));
        Assert.Single(repository.Items);
    }

    [Fact]
    public async Task DeleteInactiveAsync_RemovesInactiveElevator()
    {
        var repository = new InMemoryElevatorRepository();
        var elevator = repository.Seed(new Elevator
        {
            Name = "Tower B",
            Address = "200 Main St",
            BuildingName = "Annex",
            FloorLabel = "L2",
            Manufacturer = "Kone",
            InstallationDate = new DateTime(2021, 1, 1),
            IsActive = false,
            Location = new GpsCoordinateValidator().CreatePoint(41.8781, -87.6298)
        });

        var service = new ElevatorService(repository, new GpsCoordinateValidator());

        await service.DeleteInactiveAsync(elevator.Id);

        Assert.Empty(repository.Items);
    }

    private sealed class InMemoryElevatorRepository : IElevatorRepository
    {
        public List<Elevator> Items { get; } = new();

        public Elevator Seed(Elevator elevator)
        {
            Items.Add(elevator);
            return elevator;
        }

        public Task<Elevator?> GetByIdAsync(Guid id)
        {
            return Task.FromResult(Items.FirstOrDefault(elevator => elevator.Id == id));
        }

        public Task<IEnumerable<Elevator>> GetAllAsync()
        {
            return Task.FromResult<IEnumerable<Elevator>>(Items.ToList());
        }

        public Task<IEnumerable<Elevator>> FindAsync(FilterDefinition<Elevator> filter)
        {
            throw new NotSupportedException();
        }

        public Task AddAsync(Elevator entity)
        {
            Items.Add(entity);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Elevator entity)
        {
            var index = Items.FindIndex(elevator => elevator.Id == entity.Id);
            if (index >= 0)
            {
                Items[index] = entity;
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id)
        {
            Items.RemoveAll(elevator => elevator.Id == id);
            return Task.CompletedTask;
        }

        public Task<long> CountAsync(FilterDefinition<Elevator>? filter = null)
        {
            return Task.FromResult((long)Items.Count);
        }

        public Task<IEnumerable<Elevator>> GetActiveAsync()
        {
            return Task.FromResult<IEnumerable<Elevator>>(Items.Where(elevator => elevator.IsActive && elevator.DeletedAt == null).ToList());
        }
    }
}
