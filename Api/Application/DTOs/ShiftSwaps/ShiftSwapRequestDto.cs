namespace Api.Application.DTOs.ShiftSwaps;

public class ShiftSwapRequestDto
{
    public int Id { get; set; }
    public int RequesterId { get; set; }
    public int TargetUserId { get; set; }
    public int RequesterShiftId { get; set; }
    public int TargetShiftId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
