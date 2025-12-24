namespace Api.Domain.Exceptions;

public class EntityNotFoundException : Exception
{
    public EntityNotFoundException(string message)
        : base(message)
    {
    }

    public static EntityNotFoundException ForEntity(string entityName, int id)
        => new($"{entityName} con ID {id} no encontrado");
}
