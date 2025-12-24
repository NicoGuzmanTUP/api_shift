using Api.Application.DTOs.ShiftSwaps;
using Api.Application.Interfaces;
using Api.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ShiftSwapsController : ControllerBase
{
    private readonly IShiftSwapService _shiftSwapService;

    public ShiftSwapsController(IShiftSwapService shiftSwapService)
    {
        _shiftSwapService = shiftSwapService;
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var id)
            ? throw new InvalidOperationException("User ID not found in claims")
            : id;
    }

    [HttpPost]
    public async Task<ActionResult<ShiftSwapRequestDto>> CreateSwapRequest(CreateShiftSwapRequest request)
    {
        try
        {
            var userId = GetUserId();
            var swap = await _shiftSwapService.CreateSwapRequestAsync(userId, request);
            return CreatedAtAction(nameof(GetSwapRequest), new { id = swap.Id }, swap);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/accept")]
    public async Task<ActionResult<ShiftSwapRequestDto>> AcceptSwap(int id)
    {
        try
        {
            var userId = GetUserId();
            var swap = await _shiftSwapService.AcceptSwapAsync(id, userId);
            return Ok(swap);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/reject")]
    public async Task<ActionResult<ShiftSwapRequestDto>> RejectSwap(int id)
    {
        try
        {
            var userId = GetUserId();
            var swap = await _shiftSwapService.RejectSwapAsync(id, userId);
            return Ok(swap);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/cancel")]
    public async Task<ActionResult<ShiftSwapRequestDto>> CancelSwap(int id)
    {
        try
        {
            var userId = GetUserId();
            var swap = await _shiftSwapService.CancelSwapAsync(id, userId);
            return Ok(swap);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "HR")]
    [HttpPost("{id}/approve")]
    public async Task<ActionResult<ShiftSwapRequestDto>> ApproveSwap(int id)
    {
        try
        {
            var swap = await _shiftSwapService.ApproveSwapAsync(id);
            return Ok(swap);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ShiftSwapRequestDto>> GetSwapRequest(int id)
    {
        var swap = await _shiftSwapService.GetSwapByIdAsync(id);
        return swap is null
            ? NotFound(new { message = "Solicitud no encontrada" })
            : Ok(swap);
    }

    [Authorize(Roles = "HR")]
    [HttpGet("pending")]
    public async Task<ActionResult<List<ShiftSwapRequestDto>>> GetPendingSwaps()
    {
        var swaps = await _shiftSwapService.GetPendingSwapsAsync();
        return Ok(swaps);
    }
}
