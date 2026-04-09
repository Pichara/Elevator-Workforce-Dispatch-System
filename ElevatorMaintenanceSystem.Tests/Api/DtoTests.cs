using ElevatorMaintenanceSystem.Api.Dtos.Elevators;
using ElevatorMaintenanceSystem.Api.Dtos.Tickets;
using ElevatorMaintenanceSystem.Api.Dtos.Workers;
using ElevatorMaintenanceSystem.Api.Mapping;
using ElevatorMaintenanceSystem.Models;
using MongoDB.Driver.GeoJsonObjectModel;

namespace ElevatorMaintenanceSystem.Tests.Api;

public class DtoTests
{
    [Fact]
    public void ElevatorDto_FlattensGeoJsonPoint_ToLatitudeLongitude()
    {
        var elevator = new Elevator
        {
            Id = Guid.NewGuid(),
            Name = "Tower A",
            Address = "100 Main St",
            BuildingName = "Main Tower",
            FloorLabel = "L1",
            Manufacturer = "Otis",
            InstallationDate = new DateTime(2020, 1, 1),
            Location = CreatePoint(43.4516, -80.4925)
        };

        var dto = ManualMapper.ToDto(elevator);

        Assert.Equal(43.4516, dto.Latitude, 4);
        Assert.Equal(-80.4925, dto.Longitude, 4);
    }

    [Fact]
    public void WorkerDto_FlattensGeoJsonPoint_ToLatitudeLongitude()
    {
        var worker = new Worker
        {
            Id = Guid.NewGuid(),
            FullName = "Ava Stone",
            Email = "ava@example.com",
            PhoneNumber = "555-1000",
            Skills = ["Repair"],
            AvailabilityStatus = WorkerAvailabilityStatus.Assigned,
            Location = CreatePoint(43.452, -80.493)
        };

        var dto = ManualMapper.ToDto(worker);

        Assert.Equal("Ava Stone", dto.Name);
        Assert.Equal(43.452, dto.Latitude, 3);
        Assert.Equal(-80.493, dto.Longitude, 3);
    }

    [Fact]
    public void TicketDto_MapsAuditHistory_WithoutMongoSpecificTypes()
    {
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ElevatorId = Guid.NewGuid(),
            Description = "Door stuck",
            IssueType = TicketIssueType.Mechanical,
            Priority = TicketPriority.High,
            RequestedDate = new DateTime(2026, 4, 9),
            Status = TicketStatus.Pending,
            History =
            [
                new TicketAuditEntry
                {
                    OccurredAtUtc = new DateTime(2026, 4, 9, 12, 0, 0, DateTimeKind.Utc),
                    ChangedBy = "api-test-user",
                    EntryType = TicketAuditEntryType.Created,
                    ToStatus = TicketStatus.Pending,
                    Message = "Created ticket."
                }
            ]
        };

        var dto = ManualMapper.ToDto(ticket);

        Assert.Single(dto.History);
        Assert.Equal(TicketAuditEntryType.Created, dto.History[0].EntryType);
        Assert.DoesNotContain(typeof(TicketDto).GetProperties(), property => property.PropertyType.FullName?.Contains("MongoDB", StringComparison.Ordinal) == true);
    }

    [Fact]
    public void CreateDtos_HoldSimpleSerializableShapes()
    {
        Assert.Equal(typeof(Guid), typeof(CreateTicketDto).GetProperty(nameof(CreateTicketDto.ElevatorId))!.PropertyType);
        Assert.Equal(typeof(double), typeof(CreateElevatorDto).GetProperty(nameof(CreateElevatorDto.Latitude))!.PropertyType);
        Assert.Equal(typeof(List<string>), typeof(CreateWorkerDto).GetProperty(nameof(CreateWorkerDto.Skills))!.PropertyType);
    }

    private static GeoJsonPoint<GeoJson2DGeographicCoordinates> CreatePoint(double latitude, double longitude)
    {
        return new GeoJsonPoint<GeoJson2DGeographicCoordinates>(new GeoJson2DGeographicCoordinates(longitude, latitude));
    }
}
