namespace Api.Domain.Exceptions;

public class EmailAlreadyExistsException : Exception
{
    public EmailAlreadyExistsException(string email)
        : base($"El email '{email}' ya está registrado")
    {
    }
}
