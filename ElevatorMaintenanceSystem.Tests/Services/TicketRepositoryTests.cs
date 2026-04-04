using ElevatorMaintenanceSystem.Data;
using ElevatorMaintenanceSystem.Infrastructure;
using ElevatorMaintenanceSystem.Models;
using MongoDB.Driver;
using Xunit;

namespace ElevatorMaintenanceSystem.Tests.Services;

public class TicketRepositoryTests
{
    [Fact]
    public async Task GetByStatusAsync_FiltersByStatusAndExcludesSoftDeletedTickets()
    {
        using var harness = await CreateHarnessAsync();
        var pending = CreateTicket(harness.ElevatorA.Id, null, TicketStatus.Pending, new DateTime(2026, 4, 2));
        var closed = CreateTicket(harness.ElevatorA.Id, harness.WorkerA.Id, TicketStatus.Closed, new DateTime(2026, 4, 3));
        var deletedPending = CreateTicket(harness.ElevatorB.Id, null, TicketStatus.Pending, new DateTime(2026, 4, 4), deletedAt: DateTime.UtcNow);

        await harness.SeedTicketsAsync(pending, closed, deletedPending);

        var pendingResults = (await harness.Repository.GetByStatusAsync(TicketStatus.Pending)).ToList();
        var allResults = (await harness.Repository.GetByStatusAsync(null)).ToList();

        Assert.Collection(pendingResults, ticket => Assert.Equal(pending.Id, ticket.Id));
        Assert.Equal(
            new HashSet<Guid>([pending.Id, closed.Id]),
            allResults.Select(ticket => ticket.Id).ToHashSet());
    }

    [Fact]
    public async Task GetByElevatorAsync_ReturnsOnlyTicketsForRequestedElevator()
    {
        using var harness = await CreateHarnessAsync();
        var elevatorATicket = CreateTicket(harness.ElevatorA.Id, null, TicketStatus.Pending, new DateTime(2026, 4, 2));
        var elevatorBTicket = CreateTicket(harness.ElevatorB.Id, harness.WorkerA.Id, TicketStatus.Assigned, new DateTime(2026, 4, 3));

        await harness.SeedTicketsAsync(elevatorATicket, elevatorBTicket);

        var results = (await harness.Repository.GetByElevatorAsync(harness.ElevatorA.Id)).ToList();

        Assert.Collection(results, ticket => Assert.Equal(elevatorATicket.Id, ticket.Id));
    }

    [Fact]
    public async Task GetByWorkerAsync_ReturnsOnlyTicketsAssignedToRequestedWorker()
    {
        using var harness = await CreateHarnessAsync();
        var assignedToA = CreateTicket(harness.ElevatorA.Id, harness.WorkerA.Id, TicketStatus.Assigned, new DateTime(2026, 4, 2));
        var assignedToB = CreateTicket(harness.ElevatorB.Id, harness.WorkerB.Id, TicketStatus.InProgress, new DateTime(2026, 4, 3));
        var unassigned = CreateTicket(harness.ElevatorA.Id, null, TicketStatus.Pending, new DateTime(2026, 4, 4));

        await harness.SeedTicketsAsync(assignedToA, assignedToB, unassigned);

        var results = (await harness.Repository.GetByWorkerAsync(harness.WorkerA.Id)).ToList();

        Assert.Collection(results, ticket => Assert.Equal(assignedToA.Id, ticket.Id));
    }

    [Fact]
    public async Task GetByDateRangeAsync_SupportsInclusiveAndOpenEndedRanges()
    {
        using var harness = await CreateHarnessAsync();
        var earlier = CreateTicket(harness.ElevatorA.Id, null, TicketStatus.Pending, new DateTime(2026, 4, 1));
        var inside = CreateTicket(harness.ElevatorA.Id, harness.WorkerA.Id, TicketStatus.Assigned, new DateTime(2026, 4, 3));
        var later = CreateTicket(harness.ElevatorB.Id, harness.WorkerB.Id, TicketStatus.Closed, new DateTime(2026, 4, 5));

        await harness.SeedTicketsAsync(earlier, inside, later);

        var bounded = (await harness.Repository.GetByDateRangeAsync(new DateTime(2026, 4, 2), new DateTime(2026, 4, 4))).ToList();
        var openEnded = (await harness.Repository.GetByDateRangeAsync(null, new DateTime(2026, 4, 3))).ToList();

        Assert.Collection(bounded, ticket => Assert.Equal(inside.Id, ticket.Id));
        Assert.Equal(
            new HashSet<Guid>([earlier.Id, inside.Id]),
            openEnded.Select(ticket => ticket.Id).ToHashSet());
    }

    [Fact]
    public async Task GetFilteredAsync_CombinesAllProvidedFilters()
    {
        using var harness = await CreateHarnessAsync();
        var expected = CreateTicket(harness.ElevatorA.Id, harness.WorkerA.Id, TicketStatus.Assigned, new DateTime(2026, 4, 3));
        var wrongStatus = CreateTicket(harness.ElevatorA.Id, harness.WorkerA.Id, TicketStatus.Pending, new DateTime(2026, 4, 3));
        var wrongElevator = CreateTicket(harness.ElevatorB.Id, harness.WorkerA.Id, TicketStatus.Assigned, new DateTime(2026, 4, 3));
        var wrongWorker = CreateTicket(harness.ElevatorA.Id, harness.WorkerB.Id, TicketStatus.Assigned, new DateTime(2026, 4, 3));
        var wrongDate = CreateTicket(harness.ElevatorA.Id, harness.WorkerA.Id, TicketStatus.Assigned, new DateTime(2026, 4, 10));

        await harness.SeedTicketsAsync(expected, wrongStatus, wrongElevator, wrongWorker, wrongDate);

        var results = (await harness.Repository.GetFilteredAsync(
            TicketStatus.Assigned,
            harness.ElevatorA.Id,
            harness.WorkerA.Id,
            new DateTime(2026, 4, 2),
            new DateTime(2026, 4, 4))).ToList();

        Assert.Collection(results, ticket => Assert.Equal(expected.Id, ticket.Id));
    }

    private static async Task<TicketRepositoryHarness> CreateHarnessAsync()
    {
        var databaseName = $"ElevatorMaintenanceTests_{Guid.NewGuid():N}";
        var settings = new MongoDbSettings
        {
            ConnectionString = "mongodb://localhost:27017/",
            DatabaseName = databaseName
        };

        var context = new MongoDbContext(settings);
        var repository = new TicketRepository(context);
        var database = new MongoClient(settings.ConnectionString).GetDatabase(databaseName);
        var elevators = database.GetCollection<Elevator>("elevators");
        var workers = database.GetCollection<Worker>("workers");

        var elevatorA = new Elevator
        {
            Id = Guid.NewGuid(),
            Name = "Tower A",
            Address = "100 Main St",
            BuildingName = "North Tower",
            FloorLabel = "L1",
            Manufacturer = "Otis",
            InstallationDate = new DateTime(2020, 1, 1),
            IsActive = true
        };

        var elevatorB = new Elevator
        {
            Id = Guid.NewGuid(),
            Name = "Tower B",
            Address = "200 Main St",
            BuildingName = "South Tower",
            FloorLabel = "L2",
            Manufacturer = "Kone",
            InstallationDate = new DateTime(2021, 1, 1),
            IsActive = true
        };

        var workerA = new Worker
        {
            Id = Guid.NewGuid(),
            FullName = "Ava Stone",
            Email = "ava@example.com",
            PhoneNumber = "555-0100",
            Skills = ["Repair"],
            AvailabilityStatus = WorkerAvailabilityStatus.Available
        };

        var workerB = new Worker
        {
            Id = Guid.NewGuid(),
            FullName = "Noah Reed",
            Email = "noah@example.com",
            PhoneNumber = "555-0101",
            Skills = ["Inspection"],
            AvailabilityStatus = WorkerAvailabilityStatus.Available
        };

        await elevators.InsertManyAsync([elevatorA, elevatorB]);
        await workers.InsertManyAsync([workerA, workerB]);

        return new TicketRepositoryHarness(settings, context, repository, elevatorA, elevatorB, workerA, workerB);
    }

    private static Ticket CreateTicket(
        Guid elevatorId,
        Guid? workerId,
        TicketStatus status,
        DateTime requestedDate,
        DateTime? deletedAt = null)
    {
        return new Ticket
        {
            Id = Guid.NewGuid(),
            ElevatorId = elevatorId,
            AssignedWorkerId = workerId,
            Description = $"{status} ticket",
            IssueType = TicketIssueType.Mechanical,
            Priority = TicketPriority.Medium,
            RequestedDate = requestedDate,
            Status = status,
            CreatedAt = requestedDate.AddHours(-2),
            UpdatedAt = requestedDate.AddHours(-1),
            DeletedAt = deletedAt,
            History =
            [
                new TicketAuditEntry
                {
                    OccurredAtUtc = requestedDate.AddHours(-2),
                    ChangedBy = "seed",
                    EntryType = TicketAuditEntryType.Created,
                    ToStatus = status,
                    ToWorkerId = workerId,
                    Message = "Seeded ticket."
                }
            ]
        };
    }

    private sealed class TicketRepositoryHarness : IDisposable
    {
        private readonly MongoClient _client;
        private readonly IMongoCollection<Ticket> _ticketCollection;

        public TicketRepositoryHarness(
            MongoDbSettings settings,
            IMongoDbContext context,
            TicketRepository repository,
            Elevator elevatorA,
            Elevator elevatorB,
            Worker workerA,
            Worker workerB)
        {
            Settings = settings;
            Context = context;
            Repository = repository;
            ElevatorA = elevatorA;
            ElevatorB = elevatorB;
            WorkerA = workerA;
            WorkerB = workerB;
            _client = new MongoClient(settings.ConnectionString);
            _ticketCollection = _client.GetDatabase(settings.DatabaseName).GetCollection<Ticket>("tickets");
        }

        public MongoDbSettings Settings { get; }
        public IMongoDbContext Context { get; }
        public TicketRepository Repository { get; }
        public Elevator ElevatorA { get; }
        public Elevator ElevatorB { get; }
        public Worker WorkerA { get; }
        public Worker WorkerB { get; }

        public Task SeedTicketsAsync(params Ticket[] tickets)
        {
            return _ticketCollection.InsertManyAsync(tickets);
        }

        public void Dispose()
        {
            try
            {
                _client.DropDatabase(Settings.DatabaseName);
            }
            finally
            {
                Context.Dispose();
            }
        }
    }
}
