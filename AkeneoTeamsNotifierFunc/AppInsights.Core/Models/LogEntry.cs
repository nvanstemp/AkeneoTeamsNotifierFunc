using System;

namespace AppInsights.Core.Models
{
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? VendorCode { get; set; }
        public LogType LogType { get; set; }
    }
} 