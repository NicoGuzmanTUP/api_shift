namespace Api.Application.DTOs.Shifts;

public class UpdateShiftRequest
{
    public DateOnly? ShiftDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public string? Status { get; set; }
}
