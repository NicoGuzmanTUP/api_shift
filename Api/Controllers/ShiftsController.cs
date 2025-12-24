using Api.Application.DTOs.Shifts;
using Api.Application.Interfaces;
using Api.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ShiftsController : ControllerBase
{
    private readonly IShiftService _shiftService;
    private readonly ILogger<ShiftsController> _logger;

    public ShiftsController(IShiftService shiftService, ILogger<ShiftsController> logger)
    {
        _shiftService = shiftService;
        _logger = logger;
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var id)
            ? throw new InvalidOperationException("User ID not found in claims")
            : id;
    }

    private string GetUserRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value ?? "EMPLOYEE";
    }

    [HttpGet("my")]
    public async Task<ActionResult<List<ShiftDto>>> GetMyShifts()
    {
        var userId = GetUserId();
        var shifts = await _shiftService.GetMyShiftsAsync(userId);
        return Ok(shifts);
    }

    [HttpGet("team")]
    public async Task<ActionResult<List<ShiftDto>>> GetTeamShifts()
    {
        var shifts = await _shiftService.GetTeamShiftsAsync();
        return Ok(shifts);
    }

    [HttpPost]
    public async Task<ActionResult<ShiftDto>> CreateShift(CreateShiftRequest request)
    {
        try
        {
            var userId = GetUserId();
            var shift = await _shiftService.CreateShiftAsync(userId, request);
            return CreatedAtAction(nameof(GetMyShifts), shift);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Validación fallida al crear turno: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "HR")]
    public async Task<ActionResult<ShiftDto>> UpdateShift(int id, UpdateShiftRequest request)
    {
        try
        {
            var shift = await _shiftService.UpdateShiftAsync(id, request);
            return Ok(shift);
        }
        catch (EntityNotFoundException ex)
        {
            _logger.LogWarning("Turno no encontrado: {ShiftId}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Validación fallida al actualizar turno: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "HR")]
    public async Task<IActionResult> DeleteShift(int id)
    {
        try
        {
            await _shiftService.DeleteShiftAsync(id);
            return NoContent();
        }
        catch (EntityNotFoundException ex)
        {
            _logger.LogWarning("Turno no encontrado: {ShiftId}", id);
            return NotFound(new { message = ex.Message });
        }
    }
}
