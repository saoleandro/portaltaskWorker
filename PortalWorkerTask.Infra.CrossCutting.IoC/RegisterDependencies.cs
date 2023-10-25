using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PortalWorkerTask.Domain.Interfaces.Repository;
using PortalWorkerTask.Infra.CrossCutting.Services.Options;
using PortalWorkerTask.Infra.CrossCutting.Services.Services;
using PortalWorkerTask.Infra.Data;
using PortalWorkerTask.Infra.Data.Repository;

namespace PortalWorkerTask.Infra.CrossCutting.IoC;

public static class RegisterDependencies
{
    public static void RegisterInterfaces(this IServiceCollection servicesCollection, IConfiguration configuration)
    {
        servicesCollection.AddTransient<IRabbitConnectorService, RabbitConnectorService>();
        servicesCollection.AddTransient<IRabbitConnectorService, RabbitConnectorService>();
        servicesCollection.AddTransient<IDataTaskRepository, DataTaskRepository>();
        servicesCollection.AddSingleton<ContextDb>();

        servicesCollection.Configure<RabbitConfig>(configuration.GetSection("RabbitMQ"));


        servicesCollection.AddHealthChecks()
            .AddSqlServer(connectionString: configuration.GetConnectionString("SqlServer")!,
                          name: "SQL Server Instance")
            .AddRabbitMQ(
            new Uri($"amqp://{configuration.GetValue<string>("RabbitMQ:User")}:" +
                    $"{configuration.GetValue<string>("RabbitMQ:Password")}@" +
                    $"{configuration.GetValue<string>("RabbitMQ:Host")}:" +
                    $"{configuration.GetValue<string>("RabbitMQ:Port")}" +
                    $"{configuration.GetValue<string>("RabbitMQ:VirtualHost")}"),
            name: "RabbitMQ",
            failureStatus: HealthStatus.Degraded,
            timeout: TimeSpan.FromSeconds(1),
            tags: new string[] { "services" }
            );

        servicesCollection.AddSingleton(x =>
        {
            return new RabbitConfig
            {
                User = configuration.GetSection("RabbitMQ:User").Value,
                Password = configuration.GetSection("RabbitMQ:Password").Value,
                Port = Convert.ToInt16(configuration.GetSection("RabbitMQ:Port").Value),
                Host = configuration.GetSection("RabbitMQ:Host").Value,
                VirtualHost = configuration.GetSection("RabbitMQ:VirtualHost").Value,
                Send = new QueueConfig
                {
                    Queue = configuration.GetSection("RabbitMQ:QueueConfig:Send:Queue").Value,
                    QueueError = configuration.GetSection("RabbitMQ:QueueConfig:Send:QueueError").Value,
                    QueueReprocess = configuration.GetSection("RabbitMQ:QueueConfig:Send:QueueReprocess").Value,
                    Exchange = configuration.GetSection("RabbitMQ:QueueConfig:Send:Exchange").Value,
                    ExchangeReprocess = configuration.GetSection("RabbitMQ:QueueConfig:Send:ExchangeReprocess").Value,
                    RetryInMs = Convert.ToInt32(configuration.GetSection("RabbitMQ:QueueConfig:Send:RetryInMs").Value),
                    ErrorRetryInMs = Convert.ToInt64(configuration.GetSection("RabbitMQ:QueueConfig:Send:ErrorRetryInMs").Value),
                    RetryAttemps = Convert.ToInt16(configuration.GetSection("RabbitMQ:QueueConfig:Send:RetryAttemps").Value),
                    RoutingKey = configuration.GetSection("RabbitMQ:QueueConfig:Send:RoutingKey").Value
                }
            };
        });
      
    }
}
