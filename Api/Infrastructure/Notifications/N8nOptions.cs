namespace Api.Infrastructure.Notifications;

public class N8nOptions
{
    public const string SectionName = "N8n";

    public string WebhookUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}
