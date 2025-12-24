namespace Api.Application.Interfaces;

public interface INotifier
{
    Task NotifyAsync(string eventType, object payload);
}
