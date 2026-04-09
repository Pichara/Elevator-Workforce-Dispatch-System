using ElevatorMaintenanceSystem.Api.Dtos.Workers;
using ElevatorMaintenanceSystem.Api.Mapping;
using ElevatorMaintenanceSystem.Data;
using ElevatorMaintenanceSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElevatorMaintenanceSystem.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/workers")]
public sealed class WorkersController : ControllerBase
{
    private readonly IWorkerRepository _workerRepository;
    private readonly IWorkerService _workerService;

    public WorkersController(IWorkerRepository workerRepository, IWorkerService workerService)
    {
        _workerRepository = workerRepository;
        _workerService = workerService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<WorkerDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<WorkerDto>>> GetAll()
    {
        var workers = await _workerService.GetActiveAsync();
        return Ok(workers.Select(ManualMapper.ToDto));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WorkerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkerDto>> GetById(Guid id)
    {
        var worker = await _workerRepository.GetByIdAsync(id);
        if (worker is null || worker.DeletedAt.HasValue)
        {
            return NotFound();
        }

        return Ok(ManualMapper.ToDto(worker));
    }

    [HttpPost]
    [ProducesResponseType(typeof(WorkerDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<WorkerDto>> Create([FromBody] CreateWorkerDto dto)
    {
        var created = await _workerService.CreateAsync(
            ManualMapper.FromCreateDto(dto),
            dto.Latitude,
            dto.Longitude);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ManualMapper.ToDto(created));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWorkerDto dto)
    {
        var existing = await _workerRepository.GetByIdAsync(id);
        if (existing is null || existing.DeletedAt.HasValue)
        {
            return NotFound();
        }

        ManualMapper.ApplyUpdateDto(dto, existing);
        await _workerService.UpdateAsync(existing, dto.Latitude, dto.Longitude);
        return NoContent();
    }

    [HttpPatch("{id:guid}/location")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateLocation(Guid id, [FromBody] UpdateLocationDto dto)
    {
        await _workerService.UpdateLocationAsync(id, dto.Latitude, dto.Longitude);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var existing = await _workerRepository.GetByIdAsync(id);
        if (existing is null || existing.DeletedAt.HasValue)
        {
            return NotFound();
        }

        await _workerService.DeactivateAsync(id);
        return NoContent();
    }
}
