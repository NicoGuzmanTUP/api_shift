namespace Api.Application.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(int userId, string email, string role);
}
