using Api.Application.DTOs.ShiftSwaps;
using Api.Application.Interfaces;
using Api.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace Api.Controllers;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class RequireN8nTokenAttribute : ActionFilterAttribute
{
    private const string HeaderName = "X-N8N-TOKEN";

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var config = context.HttpContext.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration;
        var expected = config?["N8n:ApiKey"];

        if (string.IsNullOrEmpty(expected))
        {
            context.Result = new StatusCodeResult(500);
            return;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var provided) || provided != expected)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        await next();
    }
}

//[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
//public class RequireApiKeyAttribute : ActionFilterAttribute
//{
//    private const string HeaderName = "ApiKey";

//    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
//    {
//        var config = context.HttpContext.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration;
//        var expected = config?["N8n:ApiKey"]; // Api key stored under N8n:ApiKey

//        if (string.IsNullOrEmpty(expected))
//        {
//            context.Result = new StatusCodeResult(500);
//            return;
//        }

//        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var provided) || provided != expected)
//        {
//            context.Result = new UnauthorizedResult();
//            return;
//        }

//        await next();
//    }
//}

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
    //[RequireApiKey]
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
    public async Task<ActionResult<ShiftSwapRequestDto>> RejectSwap(int id, [FromBody] RejectShiftSwapRequest request)
    {
        try
        {
            var userId = GetUserId();
            var swap = await _shiftSwapService.RejectSwapAsync(id, userId, request?.Reason);
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

    // Keep internal approve method but not exposed to HR users via JWT
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

    // New HR endpoints using X-N8N-TOKEN
    [HttpPost("{id}/hr/approve")]
    [AllowAnonymous]
    [RequireN8nToken]
    public async Task<IActionResult> HrApprove(int id)
    {
        try
        {
            await _shiftSwapService.ApproveByHrAsync(id);
            return Ok(new { message = "Approved" });
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

    [HttpPost("{id}/hr/reject")]
    [AllowAnonymous]
    [RequireN8nToken]
    public async Task<IActionResult> HrReject(int id)
    {
        try
        {
            await _shiftSwapService.RejectByHrAsync(id);
            return Ok(new { message = "Rejected" });
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
}
