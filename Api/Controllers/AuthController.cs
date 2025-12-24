using Api.Application.DTOs.Auth;
using Api.Application.Interfaces;
using Api.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        try
        {
            var response = await _authService.LoginAsync(request);
            return Ok(response);
        }
        catch (InvalidCredentialsException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Endpoint para registrar nuevo usuario.
    /// NOTA: Este endpoint debe ser deshabilitado en producción.
    /// Solo para desarrollo/testing.
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<RegisterResponse>> Register(RegisterRequest request)
    {
        try
        {
            // Validaciones básicas
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { message = "El nombre es requerido" });

            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest(new { message = "El email es requerido" });

            if (string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { message = "La contraseña es requerida" });

            if (request.Password.Length < 6)
                return BadRequest(new { message = "La contraseña debe tener al menos 6 caracteres" });

            var response = await _authService.RegisterAsync(request);
            return CreatedAtAction(nameof(Register), response);
        }
        catch (EmailAlreadyExistsException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
