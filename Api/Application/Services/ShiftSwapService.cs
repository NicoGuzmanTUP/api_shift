using Api.Application.DTOs.ShiftSwaps;
using Api.Application.Interfaces;
using Api.Domain.Enums;
using Api.Domain.Exceptions;
using Api.Entities;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Globalization;

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
            Status = SwapRequestStatus.PENDING.ToString(),
            CreatedAt = DateTime.UtcNow
        };

        _context.ShiftSwapRequests.Add(swapRequest);
        await _context.SaveChangesAsync();

        _logger.Information($"Solicitud de intercambio creada: {swapRequest.Id} por {swapRequest.RequesterId} para {swapRequest.TargetUserId}", 
            swapRequest.Id, requesterId, request.TargetUserId);

        // Prepare formatted shift info
        string requesterShiftDate = requesterShift.ShiftDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string requesterShiftTime = $"{requesterShift.StartTime.ToString("HH:mm", CultureInfo.InvariantCulture)} - {requesterShift.EndTime.ToString("HH:mm", CultureInfo.InvariantCulture)}";

        string targetShiftDate = targetShift.ShiftDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string targetShiftTime = $"{targetShift.StartTime.ToString("HH:mm", CultureInfo.InvariantCulture)} - {targetShift.EndTime.ToString("HH:mm", CultureInfo.InvariantCulture)}";
        // Notificar a n8n (asincrónico, fire-and-forget) including emails and shift info
        await _notifier.NotifyAsync("SHIFT_SWAP_REQUESTED", new
        {
            swapRequest.Id,
            swapRequest.RequesterId,
            swapRequest.TargetUserId,
            RequesterName = requester.Name,
            TargetName = targetUser.Name,
            RequesterEmail = requester.Email,
            TargetEmail = targetUser.Email,
            ShiftDate = requesterShiftDate,
            ShiftTime = requesterShiftTime,
            ShiftDateTarget = targetShiftDate,
            ShiftTimeTarget = targetShiftTime,
            Reason = swapRequest.Reason
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

        if (swap.Status != SwapRequestStatus.PENDING.ToString())
            throw new InvalidOperationException($"No puedes aceptar una solicitud con estado {swap.Status}");

        // Set to PENDING_HR_APPROVAL instead of ACCEPTED
        swap.Status = SwapRequestStatus.PENDING_HR_APPROVAL.ToString();
        await _context.SaveChangesAsync();

        _logger.Information("Solicitud de intercambio marcada para aprobación HR: {SwapId} por usuario {UserId}", swapRequestId, userId);

        // Load related data to build normalized payload
        var requester = await _context.Users.FindAsync(swap.RequesterId)
            ?? throw EntityNotFoundException.ForEntity("Usuario", swap.RequesterId);
        var targetUser = await _context.Users.FindAsync(swap.TargetUserId)
            ?? throw EntityNotFoundException.ForEntity("Usuario", swap.TargetUserId);

        var requesterShift = await _context.Shifts.FindAsync(swap.RequesterShiftId)
            ?? throw EntityNotFoundException.ForEntity("Turno", swap.RequesterShiftId);
        var targetShift = await _context.Shifts.FindAsync(swap.TargetShiftId)
            ?? throw EntityNotFoundException.ForEntity("Turno", swap.TargetShiftId);

        string requesterShiftDate = requesterShift.ShiftDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string requesterShiftTime = $"{requesterShift.StartTime:HH:mm} - {requesterShift.EndTime:HH:mm}";

        string targetShiftDate = targetShift.ShiftDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string targetShiftTime = $"{targetShift.StartTime:HH:mm} - {targetShift.EndTime:HH:mm}";

        await _notifier.NotifyAsync("SHIFT_SWAP_PENDING_HR", new
        {
            swap.Id,
            swap.RequesterId,
            swap.TargetUserId,
            RequesterName = requester.Name,
            TargetName = targetUser.Name,
            RequesterEmail = requester.Email,
            TargetEmail = targetUser.Email,
            ShiftDate = requesterShiftDate,
            ShiftTime = requesterShiftTime,
            ShiftDateTarget = targetShiftDate,
            ShiftTimeTarget = targetShiftTime,
            Reason = swap.Reason
        });

        return MapToDto(swap);
    }

    public async Task<ShiftSwapRequestDto> RejectSwapAsync(int swapRequestId, int userId, string? reason = null)
    {
        var swap = await _context.ShiftSwapRequests.FindAsync(swapRequestId)
            ?? throw EntityNotFoundException.ForEntity("Solicitud", swapRequestId);

        // Solo el usuario destino puede rechazar
        if (swap.TargetUserId != userId)
            throw new InvalidOperationException("No tienes permisos para rechazar esta solicitud");

        if (swap.Status != SwapRequestStatus.PENDING.ToString())
            throw new InvalidOperationException($"No puedes rechazar una solicitud con estado {swap.Status}");

        // Sólo actualizar campos permitidos por el usuario: Status y CreatedAt
        swap.Status = SwapRequestStatus.REJECTED.ToString();
        swap.CreatedAt = DateTime.UtcNow;

        // El campo Reason puede ser provisto por el usuario; si es null, dejar vacío
        swap.Reason = reason ?? string.Empty;

        await _context.SaveChangesAsync();

        _logger.Information("Solicitud de intercambio rechazada: {SwapId} por usuario {UserId}", swapRequestId, userId);

        // Build normalized payload matching SHIFT_SWAP_REQUESTED structure
        var requester = await _context.Users.FindAsync(swap.RequesterId);
        var targetUser = await _context.Users.FindAsync(swap.TargetUserId);
        var requesterShift = await _context.Shifts.FindAsync(swap.RequesterShiftId);
        var targetShift = await _context.Shifts.FindAsync(swap.TargetShiftId);

        string shiftDate = requesterShift.ShiftDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string shiftTime = $"{requesterShift.StartTime:HH:mm} - {requesterShift.EndTime:HH:mm}";

        string targetShiftDate = targetShift.ShiftDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string targetShiftTime = $"{targetShift.StartTime.ToString("HH:mm", CultureInfo.InvariantCulture)} - {targetShift.EndTime.ToString("HH:mm", CultureInfo.InvariantCulture)}";
        await _notifier.NotifyAsync("SHIFT_SWAP_REJECTED", new
        {
            swap.Id,
            swap.RequesterId,
            swap.TargetUserId,
            RequesterName = requester?.Name,
            TargetName = targetUser?.Name,
            RequesterEmail = requester?.Email,
            TargetEmail = targetUser?.Email,
            ShiftDate = shiftDate,
            ShiftTime = shiftTime,
            ShiftDateTarget = targetShiftDate,
            ShiftTimeTarget = targetShiftTime,
            Reason = swap.Reason
        });

        return MapToDto(swap);
    }

    public async Task<ShiftSwapRequestDto> ApproveSwapAsync(int swapRequestId)
    {
        var swap = await _context.ShiftSwapRequests.FindAsync(swapRequestId)
            ?? throw EntityNotFoundException.ForEntity("Solicitud", swapRequestId);

        if (swap.Status != SwapRequestStatus.ACCEPTED.ToString() && swap.Status != SwapRequestStatus.PENDING_HR_APPROVAL.ToString())
            throw new InvalidOperationException("Solo se pueden aprobar solicitudes aceptadas o pendientes de HR");

        swap.Status = SwapRequestStatus.APPROVED.ToString();
        await _context.SaveChangesAsync();

        _logger.Information("Solicitud de intercambio aprobada: {SwapId}", swapRequestId);

        // Build requester and target shift info
        var requesterShift = await _context.Shifts.FindAsync(swap.RequesterShiftId)
            ?? throw EntityNotFoundException.ForEntity("Turno", swap.RequesterShiftId);
        var targetShift = await _context.Shifts.FindAsync(swap.TargetShiftId)
            ?? throw EntityNotFoundException.ForEntity("Turno", swap.TargetShiftId);

        string requesterShiftDate = requesterShift.ShiftDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string requesterShiftTime = $"{requesterShift.StartTime:HH:mm} - {requesterShift.EndTime:HH:mm}";

        string targetShiftDate = targetShift.ShiftDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string targetShiftTime = $"{targetShift.StartTime:HH:mm} - {targetShift.EndTime:HH:mm}";

        await _notifier.NotifyAsync("SHIFT_SWAP_APPROVED", new
        {
            swap.Id,
            swap.RequesterId,
            swap.TargetUserId,
            ShiftDateRequester = requesterShiftDate,
            ShiftTimeRequester = requesterShiftTime,
            ShiftDateTarget = targetShiftDate,
            ShiftTimeTarget = targetShiftTime
        });

        return MapToDto(swap);
    }

    public async Task<ShiftSwapRequestDto> CancelSwapAsync(int swapRequestId, int userId)
    {
        var swap = await _context.ShiftSwapRequests.FindAsync(swapRequestId)
            ?? throw EntityNotFoundException.ForEntity("Solicitud", swapRequestId);

        // Solo el usuario solicitante puede cancelar
        if (swap.RequesterId != userId)
            throw new InvalidOperationException("No tienes permisos para cancelar esta solicitud");

        if (swap.Status == SwapRequestStatus.APPROVED.ToString())
            throw new InvalidOperationException($"No puedes cancelar una solicitud con estado {swap.Status}");

        swap.Status = SwapRequestStatus.CANCELLED.ToString();
        await _context.SaveChangesAsync();

        _logger.Information("Solicitud de intercambio cancelada: {SwapId} por usuario {UserId}", swapRequestId, userId);

        await _notifier.NotifyAsync("SHIFT_SWAP_CANCELLED", new { swap.Id, swap.RequesterId, swap.TargetUserId });

        return MapToDto(swap);
    }

    // New HR methods
    public async Task ApproveByHrAsync(int swapRequestId)
    {
        var swap = await _context.ShiftSwapRequests.FindAsync(swapRequestId)
            ?? throw EntityNotFoundException.ForEntity("Solicitud", swapRequestId);

        if (swap.Status != SwapRequestStatus.PENDING_HR_APPROVAL.ToString())
            throw new InvalidOperationException("Solo se pueden aprobar solicitudes pendientes de HR");

        swap.Status = SwapRequestStatus.APPROVED.ToString();

        // Apply shift swap logic: swap shifts between users
        var requesterShift = await _context.Shifts.FindAsync(swap.RequesterShiftId)
            ?? throw EntityNotFoundException.ForEntity("Turno", swap.RequesterShiftId);
        var targetShift = await _context.Shifts.FindAsync(swap.TargetShiftId)
            ?? throw EntityNotFoundException.ForEntity("Turno", swap.TargetShiftId);

        var tempUser = requesterShift.UserId;
        requesterShift.UserId = targetShift.UserId;
        targetShift.UserId = tempUser;

        await _context.SaveChangesAsync();

        _logger.Information("Solicitud de intercambio aprobada por HR: {SwapId}", swapRequestId);

        var reqShift = await _context.Shifts.FindAsync(swap.RequesterShiftId);
        var tgtShift = await _context.Shifts.FindAsync(swap.TargetShiftId);

        string reqShiftDate = reqShift.ShiftDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string reqShiftTime = $"{reqShift.StartTime:HH:mm} - {reqShift.EndTime:HH:mm}";

        string tgtShiftDate = tgtShift.ShiftDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string tgtShiftTime = $"{tgtShift.StartTime:HH:mm} - {tgtShift.EndTime:HH:mm}";

        await _notifier.NotifyAsync("SHIFT_SWAP_APPROVED", new
        {
            swap.Id,
            swap.RequesterId,
            swap.TargetUserId,
            ShiftDateRequester = reqShiftDate,
            ShiftTimeRequester = reqShiftTime,
            ShiftDateTarget = tgtShiftDate,
            ShiftTimeTarget = tgtShiftTime
        });
    }

    public async Task RejectByHrAsync(int swapRequestId)
    {
        var swap = await _context.ShiftSwapRequests.FindAsync(swapRequestId)
            ?? throw EntityNotFoundException.ForEntity("Solicitud", swapRequestId);

        if (swap.Status != SwapRequestStatus.PENDING_HR_APPROVAL.ToString())
            throw new InvalidOperationException("Solo se pueden rechazar solicitudes pendientes de HR");

        swap.Status = SwapRequestStatus.REJECTED.ToString();
        await _context.SaveChangesAsync();

        _logger.Information("Solicitud de intercambio rechazada por HR: {SwapId}", swapRequestId);

        await _notifier.NotifyAsync("SHIFT_SWAP_REJECTED_BY_HR", new { swap.Id, swap.RequesterId, swap.TargetUserId });
    }

    public async Task<ShiftSwapRequestDto?> GetSwapByIdAsync(int swapRequestId)
    {
        var swap = await _context.ShiftSwapRequests.FindAsync(swapRequestId);
        return swap is null ? null : MapToDto(swap);
    }

    public async Task<List<ShiftSwapRequestDto>> GetPendingSwapsAsync()
    {
        return await _context.ShiftSwapRequests
            .Where(s => s.Status == SwapRequestStatus.PENDING.ToString())
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
                CreatedAt = (DateTime)s.CreatedAt
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
            CreatedAt = (DateTime)swap.CreatedAt
        };
    }
}
