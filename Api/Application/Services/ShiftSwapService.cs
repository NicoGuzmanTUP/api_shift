using Api.Application.DTOs.ShiftSwaps;
using Api.Application.Interfaces;
using Api.Domain.Exceptions;
using Api.Entities;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Api.Application.Services;

public class ShiftSwapService : IShiftSwapService
{
    private readonly shift_change_bdContext _context;
    private readonly INotifier _notifier;
    private readonly global::Serilog.ILogger _logger;

    public ShiftSwapService(shift_change_bdContext context, INotifier notifier)
    {
        _context = context;
        _notifier = notifier;
        _logger = Log.ForContext<ShiftSwapService>();
    }

    public async Task<ShiftSwapRequestDto> CreateSwapRequestAsync(int requesterId, CreateShiftSwapRequest request)
    {
        // Validar que el usuario solicitante existe
        var requester = await _context.Users.FindAsync(requesterId)
            ?? throw EntityNotFoundException.ForEntity("Usuario", requesterId);

        // Validar que el usuario destino existe
        var targetUser = await _context.Users.FindAsync(request.TargetUserId)
            ?? throw EntityNotFoundException.ForEntity("Usuario", request.TargetUserId);

        // Validar que los turnos existen
        var requesterShift = await _context.Shifts.FindAsync(request.RequesterShiftId)
            ?? throw EntityNotFoundException.ForEntity("Turno", request.RequesterShiftId);

        var targetShift = await _context.Shifts.FindAsync(request.TargetShiftId)
            ?? throw EntityNotFoundException.ForEntity("Turno", request.TargetShiftId);

        // Validar que el turno solicitante pertenece al usuario solicitante
        if (requesterShift.UserId != requesterId)
            throw new InvalidOperationException("No tienes permisos sobre este turno");

        // Validar que el turno destino pertenece al usuario destino
        if (targetShift.UserId != request.TargetUserId)
            throw new InvalidOperationException("El turno destino no pertenece al usuario especificado");

        var swapRequest = new ShiftSwapRequest
        {
            RequesterId = requesterId,
            TargetUserId = request.TargetUserId,
            RequesterShiftId = request.RequesterShiftId,
            TargetShiftId = request.TargetShiftId,
            Reason = request.Reason,
            Status = "PENDING",
            CreatedAt = DateTime.UtcNow
        };

        _context.ShiftSwapRequests.Add(swapRequest);
        await _context.SaveChangesAsync();

        _logger.Information("Solicitud de intercambio creada: {SwapId} por {RequesterId} para {TargetUserId}", 
            swapRequest.Id, requesterId, request.TargetUserId);

        // Notificar a n8n (asincrónico, fire-and-forget)
        await _notifier.NotifyAsync("SHIFT_SWAP_REQUESTED", new
        {
            swapRequest.Id,
            swapRequest.RequesterId,
            swapRequest.TargetUserId,
            RequesterName = requester.Name,
            TargetName = targetUser.Name
        });

        return MapToDto(swapRequest);
    }

    public async Task<ShiftSwapRequestDto> AcceptSwapAsync(int swapRequestId, int userId)
    {
        var swap = await _context.ShiftSwapRequests.FindAsync(swapRequestId)
            ?? throw EntityNotFoundException.ForEntity("Solicitud", swapRequestId);

        // Solo el usuario destino puede aceptar
        if (swap.TargetUserId != userId)
            throw new InvalidOperationException("No tienes permisos para aceptar esta solicitud");

        if (swap.Status != "PENDING")
            throw new InvalidOperationException($"No puedes aceptar una solicitud con estado {swap.Status}");

        swap.Status = "ACCEPTED";
        await _context.SaveChangesAsync();

        _logger.Information("Solicitud de intercambio aceptada: {SwapId} por usuario {UserId}", swapRequestId, userId);

        await _notifier.NotifyAsync("SHIFT_SWAP_ACCEPTED", new { swap.Id, swap.RequesterId, swap.TargetUserId });

        return MapToDto(swap);
    }

    public async Task<ShiftSwapRequestDto> RejectSwapAsync(int swapRequestId, int userId)
    {
        var swap = await _context.ShiftSwapRequests.FindAsync(swapRequestId)
            ?? throw EntityNotFoundException.ForEntity("Solicitud", swapRequestId);

        // Solo el usuario destino puede rechazar
        if (swap.TargetUserId != userId)
            throw new InvalidOperationException("No tienes permisos para rechazar esta solicitud");

        if (swap.Status != "PENDING")
            throw new InvalidOperationException($"No puedes rechazar una solicitud con estado {swap.Status}");

        swap.Status = "REJECTED";
        await _context.SaveChangesAsync();

        _logger.Information("Solicitud de intercambio rechazada: {SwapId} por usuario {UserId}", swapRequestId, userId);

        await _notifier.NotifyAsync("SHIFT_SWAP_REJECTED", new { swap.Id, swap.RequesterId, swap.TargetUserId });

        return MapToDto(swap);
    }

    public async Task<ShiftSwapRequestDto> ApproveSwapAsync(int swapRequestId)
    {
        var swap = await _context.ShiftSwapRequests.FindAsync(swapRequestId)
            ?? throw EntityNotFoundException.ForEntity("Solicitud", swapRequestId);

        if (swap.Status != "ACCEPTED")
            throw new InvalidOperationException("Solo se pueden aprobar solicitudes aceptadas");

        swap.Status = "APPROVED";
        await _context.SaveChangesAsync();

        _logger.Information("Solicitud de intercambio aprobada: {SwapId}", swapRequestId);

        await _notifier.NotifyAsync("SHIFT_SWAP_APPROVED", new { swap.Id, swap.RequesterId, swap.TargetUserId });

        return MapToDto(swap);
    }

    public async Task<ShiftSwapRequestDto> CancelSwapAsync(int swapRequestId, int userId)
    {
        var swap = await _context.ShiftSwapRequests.FindAsync(swapRequestId)
            ?? throw EntityNotFoundException.ForEntity("Solicitud", swapRequestId);

        // Solo el usuario solicitante puede cancelar
        if (swap.RequesterId != userId)
            throw new InvalidOperationException("No tienes permisos para cancelar esta solicitud");

        if (swap.Status == "CANCELLED" || swap.Status == "APPROVED")
            throw new InvalidOperationException($"No puedes cancelar una solicitud con estado {swap.Status}");

        swap.Status = "CANCELLED";
        await _context.SaveChangesAsync();

        _logger.Information("Solicitud de intercambio cancelada: {SwapId} por usuario {UserId}", swapRequestId, userId);

        await _notifier.NotifyAsync("SHIFT_SWAP_CANCELLED", new { swap.Id, swap.RequesterId, swap.TargetUserId });

        return MapToDto(swap);
    }

    public async Task<ShiftSwapRequestDto?> GetSwapByIdAsync(int swapRequestId)
    {
        var swap = await _context.ShiftSwapRequests.FindAsync(swapRequestId);
        return swap is null ? null : MapToDto(swap);
    }

    public async Task<List<ShiftSwapRequestDto>> GetPendingSwapsAsync()
    {
        return await _context.ShiftSwapRequests
            .Where(s => s.Status == "PENDING")
            .OrderBy(s => s.CreatedAt)
            .Select(s => new ShiftSwapRequestDto
            {
                Id = s.Id,
                RequesterId = s.RequesterId,
                TargetUserId = s.TargetUserId,
                RequesterShiftId = s.RequesterShiftId,
                TargetShiftId = s.TargetShiftId,
                Reason = s.Reason,
                Status = s.Status,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync();
    }

    private ShiftSwapRequestDto MapToDto(ShiftSwapRequest swap)
    {
        return new ShiftSwapRequestDto
        {
            Id = swap.Id,
            RequesterId = swap.RequesterId,
            TargetUserId = swap.TargetUserId,
            RequesterShiftId = swap.RequesterShiftId,
            TargetShiftId = swap.TargetShiftId,
            Reason = swap.Reason,
            Status = swap.Status,
            CreatedAt = swap.CreatedAt
        };
    }
}
