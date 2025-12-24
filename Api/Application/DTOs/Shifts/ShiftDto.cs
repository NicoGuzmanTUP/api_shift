namespace Api.Application.DTOs.Shifts;

public class ShiftDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateOnly ShiftDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
}
