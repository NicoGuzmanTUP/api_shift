namespace Api.Domain.Exceptions;

public class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException(string message = "Email o contraseña inválidos")
        : base(message)
    {
    }
}
