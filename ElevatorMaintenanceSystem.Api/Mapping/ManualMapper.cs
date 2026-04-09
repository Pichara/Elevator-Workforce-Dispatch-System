using ElevatorMaintenanceSystem.Api.Dtos.Elevators;
using ElevatorMaintenanceSystem.Api.Dtos.Tickets;
using ElevatorMaintenanceSystem.Api.Dtos.Workers;
using ElevatorMaintenanceSystem.Models;

namespace ElevatorMaintenanceSystem.Api.Mapping;

public static class ManualMapper
{
    public static ElevatorDto ToDto(Elevator elevator)
    {
        ArgumentNullException.ThrowIfNull(elevator);

        return new ElevatorDto
        {
            Id = elevator.Id,
            Name = elevator.Name,
            Address = elevator.Address,
            BuildingName = elevator.BuildingName,
            FloorLabel = elevator.FloorLabel,
            Manufacturer = elevator.Manufacturer,
            InstallationDate = elevator.InstallationDate,
            IsActive = elevator.IsActive,
            Latitude = elevator.Location?.Coordinates.Latitude ?? 0d,
            Longitude = elevator.Location?.Coordinates.Longitude ?? 0d,
            CreatedAt = elevator.CreatedAt,
            UpdatedAt = elevator.UpdatedAt
        };
    }

    public static Elevator FromCreateDto(CreateElevatorDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new Elevator
        {
            Id = Guid.Empty,
            Name = dto.Name.Trim(),
            Address = dto.Address.Trim(),
            BuildingName = dto.BuildingName.Trim(),
            FloorLabel = dto.FloorLabel.Trim(),
            Manufacturer = dto.Manufacturer.Trim(),
            InstallationDate = dto.InstallationDate,
            IsActive = true
        };
    }

    public static void ApplyUpdateDto(UpdateElevatorDto dto, Elevator elevator)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentNullException.ThrowIfNull(elevator);

        elevator.Name = dto.Name.Trim();
        elevator.Address = dto.Address.Trim();
        elevator.BuildingName = dto.BuildingName.Trim();
        elevator.FloorLabel = dto.FloorLabel.Trim();
        elevator.Manufacturer = dto.Manufacturer.Trim();
        elevator.InstallationDate = dto.InstallationDate;
        elevator.IsActive = dto.IsActive;
    }

    public static WorkerDto ToDto(Worker worker)
    {
        ArgumentNullException.ThrowIfNull(worker);

        return new WorkerDto
        {
            Id = worker.Id,
            Name = worker.FullName,
            Email = worker.Email,
            Phone = worker.PhoneNumber,
            Skills = worker.Skills.ToList(),
            AvailabilityStatus = worker.AvailabilityStatus,
            Latitude = worker.Location?.Coordinates.Latitude ?? 0d,
            Longitude = worker.Location?.Coordinates.Longitude ?? 0d,
            IsActive = worker.DeletedAt is null,
            CreatedAt = worker.CreatedAt,
            UpdatedAt = worker.UpdatedAt
        };
    }

    public static Worker FromCreateDto(CreateWorkerDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new Worker
        {
            Id = Guid.Empty,
            FullName = dto.Name.Trim(),
            Email = dto.Email.Trim(),
            PhoneNumber = dto.Phone.Trim(),
            Skills = dto.Skills
                .Where(skill => !string.IsNullOrWhiteSpace(skill))
                .Select(skill => skill.Trim())
                .ToList(),
            AvailabilityStatus = dto.AvailabilityStatus
        };
    }

    public static void ApplyUpdateDto(UpdateWorkerDto dto, Worker worker)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentNullException.ThrowIfNull(worker);

        worker.FullName = dto.Name.Trim();
        worker.Email = dto.Email.Trim();
        worker.PhoneNumber = dto.Phone.Trim();
        worker.Skills = dto.Skills
            .Where(skill => !string.IsNullOrWhiteSpace(skill))
            .Select(skill => skill.Trim())
            .ToList();
        worker.AvailabilityStatus = dto.AvailabilityStatus;
    }

    public static TicketDto ToDto(Ticket ticket)
    {
        ArgumentNullException.ThrowIfNull(ticket);

        return new TicketDto
        {
            Id = ticket.Id,
            ElevatorId = ticket.ElevatorId,
            AssignedWorkerId = ticket.AssignedWorkerId,
            Description = ticket.Description,
            IssueType = ticket.IssueType,
            Priority = ticket.Priority,
            RequestedDate = ticket.RequestedDate,
            Status = ticket.Status,
            History = ticket.History.Select(ToAuditEntryDto).ToList(),
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt
        };
    }

    public static TicketAuditEntryDto ToAuditEntryDto(TicketAuditEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return new TicketAuditEntryDto
        {
            OccurredAtUtc = entry.OccurredAtUtc,
            ChangedBy = entry.ChangedBy,
            EntryType = entry.EntryType,
            FromStatus = entry.FromStatus,
            ToStatus = entry.ToStatus,
            FromWorkerId = entry.FromWorkerId,
            ToWorkerId = entry.ToWorkerId,
            Message = entry.Message
        };
    }
}
