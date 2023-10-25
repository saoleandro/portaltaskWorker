using Newtonsoft.Json;
using PortalWorkerTask.Domain.Entities;
using PortalWorkerTask.Domain.Enums;
using PortalWorkerTask.Domain.Interfaces.Repository;
using PortalWorkerTaskConsumer.Application.Interfaces;

namespace PortalWorkerTaskConsumer.Application;

public class ProccessMessage : IProccessMessage
{
    private readonly ILogger<ProccessMessage> _logger;
    private readonly IDataTaskRepository _taskRepository;

    public ProccessMessage(ILogger<ProccessMessage> logger, IDataTaskRepository taskRepository)
    {
        _logger = logger;
        _taskRepository = taskRepository;
    }

    public async Task ProcessMessage(string message, bool isError)
    {
        _logger.LogInformation(@"Mensagem recebida: {message}", message);

        var messageDataTask = JsonConvert.DeserializeObject<DataTask>(message);

        if (messageDataTask == null || string.IsNullOrWhiteSpace(messageDataTask.Description))
            throw new ArgumentException("A mensagem não possui dados necessários para o processamento", nameof(message));

        if (isError)
        {
            _logger.LogInformation("Marcando o data task {id} como erro", messageDataTask.Id);

            messageDataTask.SetStatus((int)StatusEnum.Error);

            await _taskRepository.UpdateAsync(messageDataTask);

            return;
        }

        _logger.LogInformation("Verificando se existe o data task cadastrado");

        if(messageDataTask.Id == 0)
        {
            _logger.LogInformation("Incluindo o data task.");

            var createModel = new DataTask(
                messageDataTask.Description, 
                messageDataTask.ValidateDate, 
                messageDataTask.Status,
                messageDataTask.CreatedAt,
                messageDataTask.UpdatedAt);
            
            await _taskRepository.CreateAsync(createModel);
        }
        else
        {
            _logger.LogInformation("Alterando o data task | Id: {id}", messageDataTask.Id);

            await _taskRepository.UpdateAsync(messageDataTask);
        }
    }
}
