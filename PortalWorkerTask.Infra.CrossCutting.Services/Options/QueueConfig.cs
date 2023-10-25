namespace PortalWorkerTask.Infra.CrossCutting.Services.Options;

public class QueueConfig
{
    public string? Exchange { get; set; }
    public string? ExchangeConsumer { get; set; }
    public string? ExchangeReprocess { get; set; }
    public string? Queue { get; set; }
    public string? QueueReprocess { get; set; }
    public string? QueueError { get; set; }
    public string? RoutingKey { get; set; }
    public int RetryInMs { get; set; }
    public long ErrorRetryInMs { get; set; }
    public int RetryAttemps { get; set; }
}

