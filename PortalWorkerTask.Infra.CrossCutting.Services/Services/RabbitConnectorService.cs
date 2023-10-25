using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PortalWorkerTask.Infra.CrossCutting.Services.Models;
using PortalWorkerTask.Infra.CrossCutting.Services.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System.Text;

namespace PortalWorkerTask.Infra.CrossCutting.Services.Services;

public class RabbitConnectorService : IRabbitConnectorService
{

    private ILogger<RabbitConnectorService> _logger;
    private IConnection conn;
    private IModel channel;
    private ConnectionFactory factory;
    private RabbitConfig _rabbitConfig;

    public RabbitConnectorService(
        IOptions<RabbitConfig> options,
        ILogger<RabbitConnectorService> logger)
    {
        _logger = logger;
        _rabbitConfig = options.Value;
    }

    public void ConnectQueue(Func<string, bool, Task> func)
    {
        var args = new Dictionary<string, object>();

        try
        {
            if (ConnectionExists())
            {

                channel = conn.CreateModel();

                channel.ExchangeDeclare(_rabbitConfig.Send?.ExchangeReprocess, ExchangeType.Topic, durable: true, autoDelete: false);
                channel.QueueDeclare(_rabbitConfig.Send?.QueueReprocess, durable: true, exclusive: false, autoDelete: false, arguments: new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", _rabbitConfig.Send?.ExchangeConsumer},
            { "x-dead-letter-routing-key", _rabbitConfig.Send?.RoutingKey},
            { "x-message-ttl", _rabbitConfig.Send?.RetryInMs}
        });

                channel.QueueBind(_rabbitConfig.Send?.QueueReprocess, _rabbitConfig.Send?.ExchangeReprocess, _rabbitConfig.Send?.RoutingKey);

                channel.ExchangeDeclare(_rabbitConfig.Send?.ExchangeConsumer, type: ExchangeType.Topic, durable: true, autoDelete: false, args);
                channel.ExchangeDeclare(_rabbitConfig.Send?.Exchange, type: ExchangeType.Topic, durable: true, autoDelete: false, args);
                channel.QueueDeclare(_rabbitConfig.Send?.Queue, durable: true, exclusive: false, autoDelete: false, arguments: new Dictionary<string, object>()
        {
             { "x-dead-letter-exchange", _rabbitConfig.Send?.ExchangeReprocess},
             { "x-dead-letter-routing-key", _rabbitConfig.Send?.RoutingKey},
        });

                channel.ExchangeBind(_rabbitConfig?.Send?.ExchangeConsumer, _rabbitConfig?.Send?.Exchange, _rabbitConfig?.Send?.RoutingKey);
                channel.QueueBind(_rabbitConfig.Send?.Queue, _rabbitConfig.Send?.ExchangeConsumer, _rabbitConfig.Send?.RoutingKey);

                channel.BasicQos(prefetchSize: 0, prefetchCount: 5, global: false);

                channel.QueueDeclare(_rabbitConfig.Send?.QueueError, durable: true, exclusive: false, autoDelete: false, arguments: null);

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.Received += async (ch, ea) =>
                {
                    await ProccessMessage(ea, func);
                };

                channel.BasicConsume(_rabbitConfig.Send?.Queue, false, consumer);
            }
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, $"Erro em Criar Exchange e Queue(Filas) no RabbitMQ. Ex: {ex.Message}");
        }
    }

    public void ConfigurationQueue()
    {
        factory = new ConnectionFactory
        {
            UserName = _rabbitConfig.User,
            Password = _rabbitConfig.Password,
            HostName = _rabbitConfig.Host,
            Port = _rabbitConfig.Port,
            VirtualHost = _rabbitConfig.VirtualHost,
            AutomaticRecoveryEnabled = true,
            RequestedHeartbeat = TimeSpan.FromSeconds(300),
            DispatchConsumersAsync = true
        };

        conn = factory.CreateConnection();
        channel = conn.CreateModel();

        conn.ConnectionShutdown += (x, y) => ReconnectRabbit();
        channel.ModelShutdown += (x, y) => ReconnectChannel();
    }

    private void ReconnectRabbit()
    {
        _logger.LogInformation(@"{Class} | {Method} | Tentando reconectar no RabbitMQ (CHANNEL)", nameof(RabbitConnectorService), nameof(ReconnectRabbit));

        factory = new ConnectionFactory
        {
            UserName = _rabbitConfig.User,
            Password = _rabbitConfig.Password,
            HostName = _rabbitConfig.Host,
            Port = _rabbitConfig.Port,
            VirtualHost = _rabbitConfig.VirtualHost,
            AutomaticRecoveryEnabled = true,
            RequestedHeartbeat = TimeSpan.FromSeconds(300),
            DispatchConsumersAsync = true
        };


        conn = factory.CreateConnection();
        channel = conn.CreateModel();
    }

    private void ReconnectChannel()
    {
        _logger.LogInformation(@"{Class} | {Method} | Tentando reconectar no RabbitMQ (CONNECTION)", nameof(RabbitConnectorService), nameof(ReconnectChannel));

        if (conn.IsOpen)
        {
            if(channel == null || !channel.IsOpen)
                channel = conn.CreateModel();
        }
        else
        {
            _logger.LogInformation(@"{Class} | {Method} | A conexão do Rabbit foi fechada. Tentando conexão", nameof(RabbitConnectorService), nameof(ReconnectChannel));

            ReconnectRabbit();

        }
    }

    private async Task ProccessMessage(BasicDeliverEventArgs ea, Func<string, bool, Task> func)
    {
        _logger.LogInformation(@"{Class} | {Method} | Pegando o próximo item da fila", nameof(RabbitConnectorService), nameof(ProccessMessage));

        var checkSendQueueError = AttemptsExceeded(ea);

        try
        {
            var message = Encoding.UTF8.GetString(ea.Body.ToArray());

            _logger.LogInformation(@"{Class} | {Method} | Mensagem a ser processada: {message}", nameof(RabbitConnectorService), nameof(ProccessMessage), message);

            var model = JsonConvert.DeserializeObject<MessageModel>(message);

            await func(message, checkSendQueueError);

            channel.BasicAck(ea.DeliveryTag, false);

        }
        catch (AlreadyClosedException exACE)
        {
            _logger.LogError(exACE, @"{Class} | {Method} | Erro decorrente da parada do serviço | Error:{error} | Stack: {stack}", nameof(RabbitConnectorService), nameof(ProccessMessage), exACE?.Message, exACE?.StackTrace);
            channel.BasicNack(ea.DeliveryTag, false, false);
        }
        catch (JsonReaderException exJson)
        {
            _logger.LogError(exJson, @"{Class} | {Method} | Mensagem em formato inválido | Error:{error} | Stack: {stack}", nameof(RabbitConnectorService), nameof(ProccessMessage), exJson?.Message, exJson?.StackTrace);
            channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(@"{Class} | {Method} | Mensagem inválida - Enviando para a fila de error | Error:{error} | Stack: {stack}", nameof(RabbitConnectorService), nameof(ProccessMessage), ex.Message, ex.StackTrace);

            channel.BasicPublish(string.Empty, _rabbitConfig.Send?.QueueError, null, ea.Body);
            channel.BasicAck(ea.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, @"{Class} | {Method} | Ocorreu um erro ao processar a mensagem. tag: {deliveryTag}", nameof(RabbitConnectorService), nameof(ProccessMessage), ea.DeliveryTag);

            if (checkSendQueueError)
                channel.BasicAck(ea.DeliveryTag, false);
            else
                channel.BasicNack(ea.DeliveryTag, false, false);
        }
    }

    private bool AttemptsExceeded(BasicDeliverEventArgs ea)
    {
        if (ea.BasicProperties.Headers is Dictionary<string, object> dic && dic.ContainsKey("x-death"))
        {
            if (ea.BasicProperties.Headers["x-death"] is List<object> xdeath && xdeath.Count > 0)
            {
                if (xdeath.FirstOrDefault() is Dictionary<string, object> last)
                {
                    long count = (long)last["count"];
                    if (count > _rabbitConfig.Send?.RetryAttemps)
                    {
                        _logger.LogInformation(@"{Class} | {Method} | Tentativa {count} de {RetryAttempts} - Encaminhada para fila de erro", nameof(RabbitConnectorService), nameof(AttemptsExceeded), count, _rabbitConfig.Send?.RetryAttemps);

                        channel.BasicPublish(string.Empty, _rabbitConfig.Send?.QueueError, null, ea.Body);
                        return true;
                    }
                }

            }
        }

        return false;
    }

    private bool ConnectionExists()
    {
        if (conn != null)
            return true;

        ConfigurationQueue();

        return conn != null;
    }
}
