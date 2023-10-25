namespace PortalWorkerTask.Domain.Interfaces.Repository;

public interface IDataTaskRepository
{
    Task<Entities.DataTask> GetByIdAsync(long id);
    Task<bool> GetByDescriptionAsync(string description);
    Task CreateAsync(Entities.DataTask dataTask);
    Task UpdateAsync(Entities.DataTask dataTask);
}

