using Api.Application.DTOs.ShiftSwaps;

namespace Api.Application.Interfaces;

public interface IShiftSwapService
{
    Task<ShiftSwapRequestDto> CreateSwapRequestAsync(int requesterId, CreateShiftSwapRequest request);
    Task<ShiftSwapRequestDto> AcceptSwapAsync(int swapRequestId, int userId);
    Task<ShiftSwapRequestDto> RejectSwapAsync(int swapRequestId, int userId);
    Task<ShiftSwapRequestDto> ApproveSwapAsync(int swapRequestId);
    Task<ShiftSwapRequestDto> CancelSwapAsync(int swapRequestId, int userId);
    Task<ShiftSwapRequestDto?> GetSwapByIdAsync(int swapRequestId);
    Task<List<ShiftSwapRequestDto>> GetPendingSwapsAsync();
    Task ApproveByHrAsync(int swapRequestId);
    Task RejectByHrAsync(int swapRequestId);
}
