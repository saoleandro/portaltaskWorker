using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace PortalWorkerTask.Infra.Data;

public class ContextDb
{
    private readonly IConfiguration _configuration;
    private readonly string _connectionSqlString;

    public ContextDb(IConfiguration configuration)
    {
        _configuration = configuration;
        _connectionSqlString = _configuration.GetConnectionString("SqlServer");
    }

    public IDbConnection CreateConnectionSql()
        => new SqlConnection(_connectionSqlString);
}
