using ElevatorMaintenanceSystem.Api.Dtos.Tickets;
using ElevatorMaintenanceSystem.Api.Mapping;
using ElevatorMaintenanceSystem.Data;
using ElevatorMaintenanceSystem.Models;
using ElevatorMaintenanceSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElevatorMaintenanceSystem.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tickets")]
public sealed class TicketsController : ControllerBase
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ITicketService _ticketService;

    public TicketsController(ITicketRepository ticketRepository, ITicketService ticketService)
    {
        _ticketRepository = ticketRepository;
        _ticketService = ticketService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TicketDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TicketDto>>> GetAll(
        [FromQuery] TicketStatus? status,
        [FromQuery] Guid? elevatorId,
        [FromQuery] Guid? workerId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        var hasFilters = status.HasValue || elevatorId.HasValue || workerId.HasValue || fromDate.HasValue || toDate.HasValue;

        var tickets = hasFilters
            ? await _ticketService.GetFilteredAsync(status, elevatorId, workerId, fromDate, toDate)
            : await _ticketService.GetActiveAsync();

        return Ok(tickets.Select(ManualMapper.ToDto));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TicketDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketDto>> GetById(Guid id)
    {
        var ticket = await _ticketRepository.GetByIdAsync(id);
        if (ticket is null || ticket.DeletedAt.HasValue)
        {
            return NotFound();
        }

        return Ok(ManualMapper.ToDto(ticket));
    }

    [HttpPost]
    [ProducesResponseType(typeof(TicketDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TicketDto>> Create([FromBody] CreateTicketDto dto)
    {
        if (dto.ElevatorId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(dto.ElevatorId), "ElevatorId is required.");
            return ValidationProblem(ModelState);
        }

        var created = await _ticketService.CreateAsync(
            dto.ElevatorId,
            dto.Description,
            dto.IssueType,
            dto.Priority,
            dto.RequestedDate);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ManualMapper.ToDto(created));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TicketDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TicketDto>> Update(Guid id, [FromBody] UpdateTicketDto dto)
    {
        var updated = await _ticketService.UpdateDetailsAsync(
            id,
            dto.Description,
            dto.IssueType,
            dto.Priority,
            dto.RequestedDate);

        return Ok(ManualMapper.ToDto(updated));
    }

    [HttpPost("{id:guid}/assign")]
    [ProducesResponseType(typeof(TicketDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TicketDto>> Assign(Guid id, [FromBody] AssignWorkerDto dto)
    {
        if (dto.WorkerId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(dto.WorkerId), "WorkerId is required.");
            return ValidationProblem(ModelState);
        }

        var updated = await _ticketService.AssignWorkerAsync(id, dto.WorkerId);
        return Ok(ManualMapper.ToDto(updated));
    }

    [HttpPost("{id:guid}/unassign")]
    [ProducesResponseType(typeof(TicketDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TicketDto>> Unassign(Guid id)
    {
        var updated = await _ticketService.UnassignWorkerAsync(id);
        return Ok(ManualMapper.ToDto(updated));
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(TicketDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TicketDto>> ChangeStatus(Guid id, [FromBody] ChangeStatusDto dto)
    {
        var updated = await _ticketService.ChangeStatusAsync(id, dto.Status);
        return Ok(ManualMapper.ToDto(updated));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _ticketService.DeleteCanceledAsync(id);
        return NoContent();
    }
}
