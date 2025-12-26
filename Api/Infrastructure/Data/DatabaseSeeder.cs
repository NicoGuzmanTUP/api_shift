using Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(shift_change_bdContext context)
    {
        // Solo crear usuarios si NO existen
        if (!await context.Users.AnyAsync())
        {
            var users = new List<User>
            {
                new()
                {
                    Name = "Juan García",
                    Email = "juan@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                    Role = "EMPLOYEE"
                },
                new()
                {
                    Name = "María López",
                    Email = "maria@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                    Role = "EMPLOYEE"
                },
                new()
                {
                    Name = "Carlos Rodríguez",
                    Email = "carlos@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                    Role = "HR"
                }
            };

            await context.Users.AddRangeAsync(users);
            await context.SaveChangesAsync();
        }

        // Solo crear turnos si NO existen
        if (!await context.Shifts.AnyAsync())
        {
            var users = await context.Users.ToListAsync();

            var shifts = new List<Shift>
            {
                new()
                {
                    UserId = users[0].Id,
                    ShiftDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
                    StartTime = new TimeOnly(9, 0),
                    EndTime = new TimeOnly(17, 0),
                    Status = "ACTIVE"
                },
                new()
                {
                    UserId = users[0].Id,
                    ShiftDate = DateOnly.FromDateTime(DateTime.Now.AddDays(2)),
                    StartTime = new TimeOnly(14, 0),
                    EndTime = new TimeOnly(22, 0),
                    Status = "ACTIVE"
                },
                new()
                {
                    UserId = users[1].Id,
                    ShiftDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
                    StartTime = new TimeOnly(14, 0),
                    EndTime = new TimeOnly(22, 0),
                    Status = "ACTIVE"
                },
                new()
                {
                    UserId = users[1].Id,
                    ShiftDate = DateOnly.FromDateTime(DateTime.Now.AddDays(3)),
                    StartTime = new TimeOnly(9, 0),
                    EndTime = new TimeOnly(17, 0),
                    Status = "ACTIVE"
                }
            };

            await context.Shifts.AddRangeAsync(shifts);
            await context.SaveChangesAsync();
        }

    }

}
