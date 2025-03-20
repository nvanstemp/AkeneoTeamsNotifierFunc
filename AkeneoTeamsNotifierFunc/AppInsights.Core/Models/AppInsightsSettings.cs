namespace AppInsights.Core.Models
{
    public class AppInsightsSettings
    {
        public string AppId { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://api.applicationinsights.io/v1/apps/";
        public TimeSpan DefaultTimeSpan { get; set; } = TimeSpan.FromHours(1);
    }
} 