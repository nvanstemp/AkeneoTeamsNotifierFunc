using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using AppInsights.Core.Services;
using AppInsights.Core.Models;
using AkeneoTeamsNotifierFunc.Services;
using AppInsights.Core.Services.Parsers;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services.AddLogging();
        services.AddHttpClient();
        services.Configure<AppInsightsSettings>(context.Configuration.GetSection("AppInsights"));
        services.AddSingleton<ILogParserFactory, LogParserFactory>();
        services.AddScoped<AppInsightsLogParser>();
        services.AddScoped<TeamsNotificationService>();

        var appInsightsSettings = context.Configuration.GetSection("AppInsights").Get<AppInsightsSettings>();
        if (appInsightsSettings == null)
        {
            throw new InvalidOperationException("AppInsights settings are not configured");
        }
        services.AddSingleton(appInsightsSettings);

        services.AddSingleton<ILogTypeIdentifier, LogTypeIdentifier>();
            
        var teamsWebhookUrl = context.Configuration["TeamsWebhookUrl"];
        if (string.IsNullOrEmpty(teamsWebhookUrl))
        {
            throw new InvalidOperationException("TeamsWebhookUrl is not configured");
        }
        services.AddSingleton<TeamsNotificationService>(sp =>
            new TeamsNotificationService(
                teamsWebhookUrl,
                sp.GetRequiredService<ILogTypeIdentifier>()));
    })
    .Build();

await host.RunAsync();