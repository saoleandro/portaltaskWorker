using PortalWorkerTaskConsumer;
using PortalWorkerTaskConsumer.Application.Interfaces;
using PortalWorkerTaskConsumer.Application;
using Serilog;
using PortalWorkerTask.Infra.CrossCutting.IoC;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        IConfiguration configuration = hostContext.Configuration;
        var loggerPath = configuration.GetValue<string>("LoggerBasePath");
        var template = configuration.GetValue<string>("LoggerFileTemplate");
        var shortDate = DateTime.Now.ToString("yyyy-MM-dd_HH");
        var fileName = $"{loggerPath}\\{shortDate}.log";

        Log.Logger = new LoggerConfiguration()
                     .ReadFrom.Configuration(configuration)
                     .WriteTo.Console(outputTemplate: template)
                     .WriteTo.File(fileName, outputTemplate: template)
                     .CreateLogger();

        services.AddHostedService<Worker>();
        services.AddTransient<IMessageConsumer, MessageConsumer>();
        services.AddTransient<IProccessMessage, ProccessMessage>();

        services.RegisterInterfaces(hostContext.Configuration);

    })
    .UseSerilog()
    .Build();

await host.RunAsync();