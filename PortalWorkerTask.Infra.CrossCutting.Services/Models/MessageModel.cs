namespace PortalWorkerTask.Infra.CrossCutting.Services.Models;

public class MessageModel
{
    public long Id { get; set; }
    public string Description { get; set; }
    public DateTime? ValidateDate { get; set; }
    public Int16 Status { get; set; }
}
