using Api.Application.DTOs.Shifts;

namespace Api.Application.Interfaces;

public interface IShiftService
{
    Task<List<ShiftDto>> GetMyShiftsAsync(int userId);
    Task<List<ShiftDto>> GetTeamShiftsAsync();
    Task<ShiftDto> CreateShiftAsync(int userId, CreateShiftRequest request);
    Task<ShiftDto> UpdateShiftAsync(int shiftId, UpdateShiftRequest request);
    Task DeleteShiftAsync(int shiftId);
}
