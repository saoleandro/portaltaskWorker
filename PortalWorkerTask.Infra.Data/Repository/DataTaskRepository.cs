using PortalWorkerTask.Domain.Entities;
using PortalWorkerTask.Domain.Interfaces.Repository;
using Dapper;
using Microsoft.Extensions.Logging;

namespace PortalWorkerTask.Infra.Data.Repository;

public class DataTaskRepository : IDataTaskRepository
{
    private readonly ILogger<DataTaskRepository> _logger;
    private readonly ContextDb _contexDb;

    const string query = @"Select
	                                d.ID,
	                                d.Description,
	                                d.ValidateDate,
                                    d.Status
                                From dbo.DataTask d
                                Where d.Description like %@description%";

    const string queryById = @"Select
	                                d.ID,
	                                d.Description,
	                                d.ValidateDate,
                                    d.Status
                                From dbo.DataTask d
                                Where d.id = @id";

    const string queryInsert = @"Insert into dbo.DataTask (description, validateDate, status, createdAt, updatedAt) 
                                 values (@description, @validateDate, @status, @createdAt, @updatedAt)";

    const string queryUpdate = @"Update dbo.DataTask  set
                                    description = @description,
                                    validateDate = @validateDate,
                                    status = @status,
                                    UpdatedAt = @updatedAt
                                 Where id = @id";

    public DataTaskRepository(ILogger<DataTaskRepository> logger, ContextDb contexDb)
    {
        _logger = logger;
        _contexDb = contexDb;   
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public async Task<bool> GetByDescriptionAsync(string description)
    {
        _logger.LogInformation("{Class} | {Method} | Consultando por descrição: {description}", nameof(DataTaskRepository), nameof(GetByDescriptionAsync), description);

        using (var connection = _contexDb.CreateConnectionSql())
        {
            connection.Open();
            return await connection.QueryFirstAsync<DataTask>(query, param: new { description }) != null;
        }
    }

    public async Task<DataTask> GetByIdAsync(long id)
    {
        _logger.LogInformation("{Class} | {Method} | Consultando por id: {id}", nameof(DataTaskRepository), nameof(GetByIdAsync), id);

        using (var connection = _contexDb.CreateConnectionSql())
        {
            connection.Open();
            return await connection.QueryFirstAsync<DataTask>(queryById, param: new { id });            
        }
    }

    public async Task CreateAsync(DataTask dataTask)
    {
        _logger.LogInformation("{Class} | {Method} | Iniciando", nameof(DataTaskRepository), nameof(CreateAsync));

        using (var connection = _contexDb.CreateConnectionSql())
        {
            connection.Open();
            var rowsAffected = await connection.ExecuteAsync(queryInsert, dataTask);

            _logger.LogInformation("{Class} | {Method} | Finalizando  | Linhas afetadas: {rowsAffected}", nameof(DataTaskRepository), nameof(CreateAsync), rowsAffected);
        }
    }
       

    public async Task UpdateAsync(DataTask dataTask)
    {
        _logger.LogInformation("{Class} | {Method} | Iniciando", nameof(DataTaskRepository), nameof(UpdateAsync));

        using (var connection = _contexDb.CreateConnectionSql())
        {
            connection.Open();

            var rowsAffected = await connection.ExecuteAsync(queryUpdate, dataTask);

            _logger.LogInformation("{Class} | {Method} | Finalizando  | Linhas afetadas: {rowsAffected}", nameof(DataTaskRepository), nameof(UpdateAsync), rowsAffected);
        }
    }
}
