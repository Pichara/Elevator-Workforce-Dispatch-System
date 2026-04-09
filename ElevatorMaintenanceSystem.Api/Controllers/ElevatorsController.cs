using ElevatorMaintenanceSystem.Api.Dtos.Elevators;
using ElevatorMaintenanceSystem.Api.Mapping;
using ElevatorMaintenanceSystem.Data;
using ElevatorMaintenanceSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElevatorMaintenanceSystem.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/elevators")]
public sealed class ElevatorsController : ControllerBase
{
    private readonly IElevatorRepository _elevatorRepository;
    private readonly IElevatorService _elevatorService;

    public ElevatorsController(IElevatorRepository elevatorRepository, IElevatorService elevatorService)
    {
        _elevatorRepository = elevatorRepository;
        _elevatorService = elevatorService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ElevatorDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ElevatorDto>>> GetAll()
    {
        var elevators = await _elevatorService.GetActiveAsync();
        return Ok(elevators.Select(ManualMapper.ToDto));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ElevatorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ElevatorDto>> GetById(Guid id)
    {
        var elevator = await _elevatorRepository.GetByIdAsync(id);
        if (elevator is null || elevator.DeletedAt.HasValue)
        {
            return NotFound();
        }

        return Ok(ManualMapper.ToDto(elevator));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ElevatorDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ElevatorDto>> Create([FromBody] CreateElevatorDto dto)
    {
        var created = await _elevatorService.CreateAsync(
            ManualMapper.FromCreateDto(dto),
            dto.Latitude,
            dto.Longitude);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ManualMapper.ToDto(created));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateElevatorDto dto)
    {
        var existing = await _elevatorRepository.GetByIdAsync(id);
        if (existing is null || existing.DeletedAt.HasValue)
        {
            return NotFound();
        }

        ManualMapper.ApplyUpdateDto(dto, existing);
        await _elevatorService.UpdateAsync(existing, dto.Latitude, dto.Longitude);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var existing = await _elevatorRepository.GetByIdAsync(id);
        if (existing is null || existing.DeletedAt.HasValue)
        {
            return NotFound();
        }

        await _elevatorService.DeleteInactiveAsync(id);
        return NoContent();
    }
}
