using Api.Application.DTOs.Shifts;
using Api.Application.Interfaces;
using Api.Domain.Exceptions;
using Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Application.Services;

public class ShiftService : IShiftService
{
    private readonly shift_change_bdContext _context;
    private readonly ILogger<ShiftService> _logger;

    public ShiftService(shift_change_bdContext context, ILogger<ShiftService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<ShiftDto>> GetMyShiftsAsync(int userId)
    {
        return await _context.Shifts
            .Where(s => s.UserId == userId)
            .OrderBy(s => s.ShiftDate)
            .ThenBy(s => s.StartTime)
            .Select(s => new ShiftDto
            {
                Id = s.Id,
                UserId = s.UserId,
                ShiftDate = s.ShiftDate,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                Status = s.Status
            })
            .ToListAsync();
    }

    public async Task<List<ShiftDto>> GetTeamShiftsAsync()
    {
        return await _context.Shifts
            .OrderBy(s => s.ShiftDate)
            .ThenBy(s => s.StartTime)
            .Select(s => new ShiftDto
            {
                Id = s.Id,
                UserId = s.UserId,
                ShiftDate = s.ShiftDate,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                Status = s.Status
            })
            .ToListAsync();
    }

    public async Task<ShiftDto> CreateShiftAsync(int userId, CreateShiftRequest request)
    {
        // Validaciones simples
        if (request.ShiftDate < DateOnly.FromDateTime(DateTime.Now))
            throw new InvalidOperationException("No puedes crear turnos en fechas pasadas");

        if (request.StartTime >= request.EndTime)
            throw new InvalidOperationException("La hora de inicio debe ser menor a la de fin");

        var shift = new Shift
        {
            UserId = userId,
            ShiftDate = request.ShiftDate,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Status = "ACTIVE"
        };

        _context.Shifts.Add(shift);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Turno creado para usuario {UserId}: {ShiftId}", userId, shift.Id);

        return new ShiftDto
        {
            Id = shift.Id,
            UserId = shift.UserId,
            ShiftDate = shift.ShiftDate,
            StartTime = shift.StartTime,
            EndTime = shift.EndTime,
            Status = shift.Status
        };
    }

    public async Task<ShiftDto> UpdateShiftAsync(int shiftId, UpdateShiftRequest request)
    {
        var shift = await _context.Shifts.FindAsync(shiftId)
            ?? throw EntityNotFoundException.ForEntity("Turno", shiftId);

        // Validaciones simples
        if (request.ShiftDate.HasValue && request.ShiftDate.Value < DateOnly.FromDateTime(DateTime.Now))
            throw new InvalidOperationException("No puedes asignar fechas pasadas");

        if (request.StartTime.HasValue && request.EndTime.HasValue && request.StartTime.Value >= request.EndTime.Value)
            throw new InvalidOperationException("La hora de inicio debe ser menor a la de fin");

        // Actualizar solo campos proporcionados
        if (request.ShiftDate.HasValue)
            shift.ShiftDate = request.ShiftDate.Value;

        if (request.StartTime.HasValue)
            shift.StartTime = request.StartTime.Value;

        if (request.EndTime.HasValue)
            shift.EndTime = request.EndTime.Value;

        if (!string.IsNullOrEmpty(request.Status))
            shift.Status = request.Status;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Turno actualizado: {ShiftId}", shiftId);

        return new ShiftDto
        {
            Id = shift.Id,
            UserId = shift.UserId,
            ShiftDate = shift.ShiftDate,
            StartTime = shift.StartTime,
            EndTime = shift.EndTime,
            Status = shift.Status
        };
    }

    public async Task DeleteShiftAsync(int shiftId)
    {
        var shift = await _context.Shifts.FindAsync(shiftId)
            ?? throw EntityNotFoundException.ForEntity("Turno", shiftId);

        _context.Shifts.Remove(shift);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Turno eliminado: {ShiftId}", shiftId);
    }
}
