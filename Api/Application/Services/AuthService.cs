using Api.Application.DTOs.Auth;
using Api.Application.Interfaces;
using Api.Domain.Exceptions;
using Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Application.Services;

public class AuthService : IAuthService
{
    private readonly shift_change_bdContext _context;
    private readonly IJwtTokenGenerator _tokenGenerator;

    public AuthService(shift_change_bdContext context, IJwtTokenGenerator tokenGenerator)
    {
        _context = context;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user is null)
            throw new InvalidCredentialsException();

        // Validar contraseña con BCrypt
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new InvalidCredentialsException();

        var token = _tokenGenerator.GenerateToken(user.Id, user.Email, user.Role);

        return new LoginResponse
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email,
            Name = user.Name,
            Role = user.Role
        };
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        // Validar que el email no exista
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (existingUser is not null)
            throw new EmailAlreadyExistsException(request.Email);

        // Hash de la contraseña con BCrypt
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        // Crear nuevo usuario
        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            PasswordHash = passwordHash,
            Role = request.Role
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return new RegisterResponse
        {
            UserId = user.Id,
            Email = user.Email,
            Name = user.Name,
            Role = user.Role,
            Message = "Usuario creado exitosamente"
        };
    }
}
