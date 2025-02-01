using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using AppInsights.Core.Services;
using Microsoft.Extensions.Logging;
using AkeneoTeamsNotifierFunc.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddLogging();
        
        services.AddSingleton<AppInsightsLogParser>(sp => 
            new AppInsightsLogParser(Environment.GetEnvironmentVariable("ApplicationInsights:ConnectionString")));
            
        services.AddSingleton<TeamsNotificationService>(sp =>
            new TeamsNotificationService(Environment.GetEnvironmentVariable("TeamsWebhookUrl")));
    })
    .Build();

await host.RunAsync();
