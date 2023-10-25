using PortalWorkerTask.Infra.CrossCutting.Services.Options;
using PortalWorkerTask.Infra.CrossCutting.Services.Services;
using PortalWorkerTaskConsumer.Application.Interfaces;

namespace PortalWorkerTaskConsumer.Application;

public class MessageConsumer : IMessageConsumer
{
    private readonly ILogger<MessageConsumer> _logger;
    private readonly IRabbitConnectorService _rabbitConnectorService;
    private readonly IProccessMessage _proccessMessage;

    public MessageConsumer(
        ILogger<MessageConsumer> logger,
        IRabbitConnectorService rabbitConnectorService,
        IProccessMessage proccessMessage)
    {
        _logger = logger;
        _rabbitConnectorService = rabbitConnectorService;
        _proccessMessage = proccessMessage;
    }

    public void Proccess()
    {
        _logger.LogInformation("Processo iniciado");

        try
        {
            _rabbitConnectorService.ConnectQueue(_proccessMessage.ProcessMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ocorreu um erro inesperado");
        }
    }
}


