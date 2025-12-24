namespace Api.Application.DTOs.ShiftSwaps;

public class CreateShiftSwapRequest
{
    public int TargetUserId { get; set; }
    public int RequesterShiftId { get; set; }
    public int TargetShiftId { get; set; }
    public string Reason { get; set; } = string.Empty;
}
