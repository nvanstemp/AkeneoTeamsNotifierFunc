namespace AppInsights.Core.Models
{
    public class ExceptionLogEntry
    {
        public DateTimeOffset Timestamp { get; set; }
        public string Type { get; set; }
        public string Message { get; set; }
        public string Details { get; set; }
    }
} 