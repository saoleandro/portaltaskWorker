using PortalWorkerTask.Domain.Enums;

namespace PortalWorkerTask.Domain.Entities;

public class DataTask
{
    public long Id { get; set; }
    public string? Description { get; private set; }
    public DateTime ValidateDate { get; private set; }
    public int Status { get; private set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public DataTask(string? description, DateTime validateDate, int status, DateTime createdAt, DateTime? updatedAt)
    {
        Description = description;
        ValidateDate = validateDate;
        Status = status;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public void SetStatus(int status)
    {
        Status = status;
    }
}
