using ElevatorMaintenanceSystem.Api.Authentication;
using ElevatorMaintenanceSystem.Data;
using ElevatorMaintenanceSystem.Infrastructure;
using ElevatorMaintenanceSystem.Models;
using ElevatorMaintenanceSystem.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;

namespace ElevatorMaintenanceSystem.Tests.Api.Support;

public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    internal const string TestApiKey = "integration-test-api-key";

    private readonly InMemoryApiState _state = new();

    public Guid SeedElevatorId => _state.SeedElevatorId;
    public Guid SeedWorkerId => _state.SeedWorkerId;

    public HttpClient CreateApiClient(bool includeApiKey = true)
    {
        _state.Reset();

        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        if (includeApiKey)
        {
            client.DefaultRequestHeaders.Add(ApiKeyAuthenticationHandler.HeaderName, TestApiKey);
        }

        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiSettings:ApiKey"] = TestApiKey
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IMongoDbContext>();
            services.RemoveAll<MongoDbSettings>();
            services.RemoveAll<IUserContext>();
            services.RemoveAll<GpsCoordinateValidator>();
            services.RemoveAll<IElevatorRepository>();
            services.RemoveAll<IWorkerRepository>();
            services.RemoveAll<ITicketRepository>();
            services.RemoveAll<IElevatorService>();
            services.RemoveAll<IWorkerService>();
            services.RemoveAll<ITicketService>();

            services.AddSingleton(_state);
            services.AddSingleton<IUserContext, TestUserContext>();
            services.AddSingleton<GpsCoordinateValidator>();
            services.AddScoped<IElevatorRepository, InMemoryElevatorRepository>();
            services.AddScoped<IWorkerRepository, InMemoryWorkerRepository>();
            services.AddScoped<ITicketRepository, InMemoryTicketRepository>();
            services.AddScoped<IElevatorService, ElevatorService>();
            services.AddScoped<IWorkerService, WorkerService>();
            services.AddScoped<ITicketService, TicketService>();
        });
    }

    private sealed class TestUserContext : IUserContext
    {
        public string GetCurrentUser() => "api-test-user";
    }

    private sealed class InMemoryApiState
    {
        private readonly object _sync = new();

        public List<Elevator> Elevators { get; private set; } = [];
        public List<Worker> Workers { get; private set; } = [];
        public List<Ticket> Tickets { get; private set; } = [];
        public Guid SeedElevatorId { get; private set; }
        public Guid SeedWorkerId { get; private set; }

        public void Reset()
        {
            lock (_sync)
            {
                Elevators = [];
                Workers = [];
                Tickets = [];

                var elevator = new Elevator
                {
                    Id = Guid.NewGuid(),
                    Name = "Seed Elevator",
                    Address = "1 Seed Way",
                    BuildingName = "Seed Building",
                    FloorLabel = "G",
                    Manufacturer = "Otis",
                    InstallationDate = new DateTime(2020, 1, 1),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                    UpdatedAt = DateTime.UtcNow.AddDays(-5),
                    Location = CreatePoint(43.4516, -80.4925)
                };

                var worker = new Worker
                {
                    Id = Guid.NewGuid(),
                    FullName = "Seed Worker",
                    Email = "seed.worker@example.com",
                    PhoneNumber = "555-0101",
                    Skills = ["Repair"],
                    AvailabilityStatus = WorkerAvailabilityStatus.Available,
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                    UpdatedAt = DateTime.UtcNow.AddDays(-5),
                    Location = CreatePoint(43.4518, -80.4927)
                };

                Elevators.Add(elevator);
                Workers.Add(worker);
                SeedElevatorId = elevator.Id;
                SeedWorkerId = worker.Id;
            }
        }

        public T Execute<T>(Func<T> action)
        {
            lock (_sync)
            {
                return action();
            }
        }

        private static GeoJsonPoint<GeoJson2DGeographicCoordinates> CreatePoint(double latitude, double longitude)
        {
            return new GeoJsonPoint<GeoJson2DGeographicCoordinates>(new GeoJson2DGeographicCoordinates(longitude, latitude));
        }
    }

    private sealed class InMemoryElevatorRepository : IElevatorRepository
    {
        private readonly InMemoryApiState _state;

        public InMemoryElevatorRepository(InMemoryApiState state)
        {
            _state = state;
        }

        public Task<Elevator?> GetByIdAsync(Guid id)
        {
            return Task.FromResult(_state.Execute(() => _state.Elevators.FirstOrDefault(elevator => elevator.Id == id)));
        }

        public Task<IEnumerable<Elevator>> GetAllAsync()
        {
            return Task.FromResult<IEnumerable<Elevator>>(_state.Execute(() => _state.Elevators.ToList()));
        }

        public Task<IEnumerable<Elevator>> FindAsync(FilterDefinition<Elevator> filter)
        {
            throw new NotSupportedException();
        }

        public Task AddAsync(Elevator entity)
        {
            _state.Execute(() =>
            {
                _state.Elevators.Add(entity);
                return 0;
            });

            return Task.CompletedTask;
        }

        public Task UpdateAsync(Elevator entity)
        {
            _state.Execute(() =>
            {
                var index = _state.Elevators.FindIndex(elevator => elevator.Id == entity.Id);
                if (index >= 0)
                {
                    _state.Elevators[index] = entity;
                }

                return 0;
            });

            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id)
        {
            _state.Execute(() =>
            {
                _state.Elevators.RemoveAll(elevator => elevator.Id == id);
                return 0;
            });

            return Task.CompletedTask;
        }

        public Task<long> CountAsync(FilterDefinition<Elevator>? filter = null)
        {
            return Task.FromResult(_state.Execute(() => (long)_state.Elevators.Count));
        }

        public Task<IEnumerable<Elevator>> GetActiveAsync()
        {
            return Task.FromResult<IEnumerable<Elevator>>(
                _state.Execute(() => _state.Elevators.Where(elevator => elevator.IsActive && elevator.DeletedAt is null).ToList()));
        }
    }

    private sealed class InMemoryWorkerRepository : IWorkerRepository
    {
        private readonly InMemoryApiState _state;

        public InMemoryWorkerRepository(InMemoryApiState state)
        {
            _state = state;
        }

        public Task<Worker?> GetByIdAsync(Guid id)
        {
            return Task.FromResult(_state.Execute(() => _state.Workers.FirstOrDefault(worker => worker.Id == id)));
        }

        public Task<IEnumerable<Worker>> GetAllAsync()
        {
            return Task.FromResult<IEnumerable<Worker>>(_state.Execute(() => _state.Workers.ToList()));
        }

        public Task<IEnumerable<Worker>> FindAsync(FilterDefinition<Worker> filter)
        {
            throw new NotSupportedException();
        }

        public Task AddAsync(Worker entity)
        {
            _state.Execute(() =>
            {
                _state.Workers.Add(entity);
                return 0;
            });

            return Task.CompletedTask;
        }

        public Task UpdateAsync(Worker entity)
        {
            _state.Execute(() =>
            {
                var index = _state.Workers.FindIndex(worker => worker.Id == entity.Id);
                if (index >= 0)
                {
                    _state.Workers[index] = entity;
                }

                return 0;
            });

            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id)
        {
            _state.Execute(() =>
            {
                _state.Workers.RemoveAll(worker => worker.Id == id);
                return 0;
            });

            return Task.CompletedTask;
        }

        public Task<long> CountAsync(FilterDefinition<Worker>? filter = null)
        {
            return Task.FromResult(_state.Execute(() => (long)_state.Workers.Count));
        }

        public Task<IEnumerable<Worker>> GetActiveAsync()
        {
            return Task.FromResult<IEnumerable<Worker>>(
                _state.Execute(() => _state.Workers.Where(worker => worker.DeletedAt is null).ToList()));
        }
    }

    private sealed class InMemoryTicketRepository : ITicketRepository
    {
        private readonly InMemoryApiState _state;

        public InMemoryTicketRepository(InMemoryApiState state)
        {
            _state = state;
        }

        public Task<Ticket?> GetByIdAsync(Guid id)
        {
            return Task.FromResult(_state.Execute(() => _state.Tickets.FirstOrDefault(ticket => ticket.Id == id)));
        }

        public Task<IEnumerable<Ticket>> GetAllAsync()
        {
            return Task.FromResult<IEnumerable<Ticket>>(_state.Execute(() => _state.Tickets.ToList()));
        }

        public Task<IEnumerable<Ticket>> FindAsync(FilterDefinition<Ticket> filter)
        {
            throw new NotSupportedException();
        }

        public Task AddAsync(Ticket entity)
        {
            _state.Execute(() =>
            {
                _state.Tickets.Add(entity);
                return 0;
            });

            return Task.CompletedTask;
        }

        public Task UpdateAsync(Ticket entity)
        {
            _state.Execute(() =>
            {
                var index = _state.Tickets.FindIndex(ticket => ticket.Id == entity.Id);
                if (index >= 0)
                {
                    _state.Tickets[index] = entity;
                }

                return 0;
            });

            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id)
        {
            _state.Execute(() =>
            {
                _state.Tickets.RemoveAll(ticket => ticket.Id == id);
                return 0;
            });

            return Task.CompletedTask;
        }

        public Task<long> CountAsync(FilterDefinition<Ticket>? filter = null)
        {
            return Task.FromResult(_state.Execute(() => (long)_state.Tickets.Count));
        }

        public Task<IEnumerable<Ticket>> GetActiveAsync()
        {
            return Task.FromResult<IEnumerable<Ticket>>(
                _state.Execute(() => _state.Tickets.Where(ticket => ticket.DeletedAt is null).OrderByDescending(ticket => ticket.RequestedDate).ToList()));
        }

        public Task<Ticket?> UpdateDetailsAsync(Guid ticketId, string description, TicketIssueType issueType, TicketPriority priority, DateTime requestedDate, DateTime changedAtUtc, string changedBy)
        {
            return Task.FromResult(_state.Execute(() =>
            {
                var ticket = _state.Tickets.FirstOrDefault(item => item.Id == ticketId);
                if (ticket is null)
                {
                    return null;
                }

                ticket.Description = description;
                ticket.IssueType = issueType;
                ticket.Priority = priority;
                ticket.RequestedDate = requestedDate;
                ticket.UpdatedAt = changedAtUtc;
                ticket.History.Add(new TicketAuditEntry
                {
                    OccurredAtUtc = changedAtUtc,
                    ChangedBy = changedBy,
                    EntryType = TicketAuditEntryType.DetailsUpdated,
                    Message = "Updated ticket details."
                });

                return ticket;
            }));
        }

        public Task<Ticket?> AssignWorkerAsync(Guid ticketId, Guid workerId, DateTime changedAtUtc, string changedBy)
        {
            return Task.FromResult(_state.Execute(() =>
            {
                var ticket = _state.Tickets.FirstOrDefault(item => item.Id == ticketId);
                if (ticket is null)
                {
                    return null;
                }

                ticket.AssignedWorkerId = workerId;
                ticket.Status = TicketStatus.Assigned;
                ticket.UpdatedAt = changedAtUtc;
                ticket.History.Add(new TicketAuditEntry
                {
                    OccurredAtUtc = changedAtUtc,
                    ChangedBy = changedBy,
                    EntryType = TicketAuditEntryType.WorkerAssigned,
                    FromStatus = TicketStatus.Pending,
                    ToStatus = TicketStatus.Assigned,
                    ToWorkerId = workerId,
                    Message = "Assigned worker to ticket."
                });

                return ticket;
            }));
        }

        public Task<Ticket?> UnassignWorkerAsync(Guid ticketId, DateTime changedAtUtc, string changedBy)
        {
            return Task.FromResult(_state.Execute(() =>
            {
                var ticket = _state.Tickets.FirstOrDefault(item => item.Id == ticketId);
                if (ticket is null)
                {
                    return null;
                }

                var priorWorkerId = ticket.AssignedWorkerId;
                ticket.AssignedWorkerId = null;
                ticket.Status = TicketStatus.Pending;
                ticket.UpdatedAt = changedAtUtc;
                ticket.History.Add(new TicketAuditEntry
                {
                    OccurredAtUtc = changedAtUtc,
                    ChangedBy = changedBy,
                    EntryType = TicketAuditEntryType.WorkerUnassigned,
                    FromStatus = TicketStatus.Assigned,
                    ToStatus = TicketStatus.Pending,
                    FromWorkerId = priorWorkerId,
                    Message = "Removed worker assignment from ticket."
                });

                return ticket;
            }));
        }

        public Task<Ticket?> ChangeStatusAsync(Guid ticketId, TicketStatus fromStatus, TicketStatus toStatus, DateTime changedAtUtc, string changedBy)
        {
            return Task.FromResult(_state.Execute(() =>
            {
                var ticket = _state.Tickets.FirstOrDefault(item => item.Id == ticketId && item.Status == fromStatus);
                if (ticket is null)
                {
                    return null;
                }

                ticket.Status = toStatus;
                ticket.UpdatedAt = changedAtUtc;
                ticket.History.Add(new TicketAuditEntry
                {
                    OccurredAtUtc = changedAtUtc,
                    ChangedBy = changedBy,
                    EntryType = toStatus == TicketStatus.Canceled ? TicketAuditEntryType.Canceled : TicketAuditEntryType.StatusChanged,
                    FromStatus = fromStatus,
                    ToStatus = toStatus,
                    FromWorkerId = ticket.AssignedWorkerId,
                    ToWorkerId = ticket.AssignedWorkerId,
                    Message = $"Changed status from {fromStatus} to {toStatus}."
                });

                return ticket;
            }));
        }

        public Task<bool> DeleteCanceledAsync(Guid ticketId)
        {
            return Task.FromResult(_state.Execute(() => _state.Tickets.RemoveAll(ticket => ticket.Id == ticketId && ticket.Status == TicketStatus.Canceled) > 0));
        }

        public Task<IEnumerable<Ticket>> GetByStatusAsync(TicketStatus? status)
        {
            return Task.FromResult<IEnumerable<Ticket>>(_state.Execute(() => ApplyFilters(status, null, null, null, null).ToList()));
        }

        public Task<IEnumerable<Ticket>> GetByElevatorAsync(Guid elevatorId)
        {
            return Task.FromResult<IEnumerable<Ticket>>(_state.Execute(() => ApplyFilters(null, elevatorId, null, null, null).ToList()));
        }

        public Task<IEnumerable<Ticket>> GetByWorkerAsync(Guid workerId)
        {
            return Task.FromResult<IEnumerable<Ticket>>(_state.Execute(() => ApplyFilters(null, null, workerId, null, null).ToList()));
        }

        public Task<IEnumerable<Ticket>> GetByDateRangeAsync(DateTime? fromDate, DateTime? toDate)
        {
            return Task.FromResult<IEnumerable<Ticket>>(_state.Execute(() => ApplyFilters(null, null, null, fromDate, toDate).ToList()));
        }

        public Task<IEnumerable<Ticket>> GetFilteredAsync(TicketStatus? status, Guid? elevatorId, Guid? workerId, DateTime? fromDate, DateTime? toDate)
        {
            return Task.FromResult<IEnumerable<Ticket>>(_state.Execute(() => ApplyFilters(status, elevatorId, workerId, fromDate, toDate).ToList()));
        }

        private IEnumerable<Ticket> ApplyFilters(TicketStatus? status, Guid? elevatorId, Guid? workerId, DateTime? fromDate, DateTime? toDate)
        {
            return _state.Tickets
                .Where(ticket => ticket.DeletedAt is null)
                .Where(ticket => !status.HasValue || ticket.Status == status.Value)
                .Where(ticket => !elevatorId.HasValue || ticket.ElevatorId == elevatorId.Value)
                .Where(ticket => !workerId.HasValue || ticket.AssignedWorkerId == workerId.Value)
                .Where(ticket => !fromDate.HasValue || ticket.RequestedDate >= fromDate.Value)
                .Where(ticket => !toDate.HasValue || ticket.RequestedDate <= toDate.Value);
        }
    }
}
