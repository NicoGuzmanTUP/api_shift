using Api.Application.Interfaces;
using Microsoft.Extensions.Options;
using Serilog;
using System.Text;
using System.Text.Json;

namespace Api.Infrastructure.Notifications;

public class N8nNotifier : INotifier
{
    private readonly N8nOptions _options;
    private readonly HttpClient _httpClient;
    private readonly global::Serilog.ILogger _logger;

    public N8nNotifier(
        IOptions<N8nOptions> options,
        HttpClient httpClient)
    {
        _options = options.Value;
        _httpClient = httpClient;
        _logger = Log.ForContext<N8nNotifier>();
    }

    public async Task NotifyAsync(string eventType, object payload)
    {
        try
        {
            if (string.IsNullOrEmpty(_options.WebhookUrl))
            {
                _logger.Warning("N8n webhook URL is not configured");
                return;
            }

            var eventPayload = new
            {
                eventType,
                timestamp = DateTime.UtcNow,
                data = payload
            };

            var content = new StringContent(
                JsonSerializer.Serialize(eventPayload),
                Encoding.UTF8,
                "application/json");

            if (!string.IsNullOrEmpty(_options.ApiKey))
            {
                _httpClient.DefaultRequestHeaders.Remove("X-API-Key");
                _httpClient.DefaultRequestHeaders.Add("X-API-Key", _options.ApiKey);
            }

            // Fire-and-forget: no esperamos la respuesta
            _ = _httpClient.PostAsync(_options.WebhookUrl, content)
                .ContinueWith(task =>
                {
                    if (task.IsCompletedSuccessfully)
                    {
                        var response = task.Result;
                        if (!response.IsSuccessStatusCode)
                        {
                            _logger.Error(
                                "N8n webhook failed for event {EventType} with status {StatusCode}: {ReasonPhrase}",
                                eventType,
                                response.StatusCode,
                                response.ReasonPhrase);
                        }
                        else
                        {
                            _logger.Information("N8n event sent successfully: {EventType}", eventType);
                        }
                    }
                    else if (task.IsFaulted)
                    {
                        _logger.Error(task.Exception, "Error sending N8n webhook for event {EventType}", eventType);
                    }
                });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error preparing N8n notification for event {EventType}", eventType);
        }
    }
}
