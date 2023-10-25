namespace PortalWorkerTaskConsumer.Application.Interfaces;

public interface IProccessMessage
{
    Task ProcessMessage(string message, bool isError);
}
