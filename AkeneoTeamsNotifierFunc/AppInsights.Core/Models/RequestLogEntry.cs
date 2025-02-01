namespace AppInsights.Core.Models
{
    public class RequestLogEntry
    {
        public DateTimeOffset Timestamp { get; set; }
        public string Name { get; set; }
        public bool Success { get; set; }
        public TimeSpan Duration { get; set; }
        public string ResultCode { get; set; }
    }
} 