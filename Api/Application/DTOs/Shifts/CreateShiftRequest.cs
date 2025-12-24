namespace Api.Application.DTOs.Shifts;

public class CreateShiftRequest
{
    public DateOnly ShiftDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}
