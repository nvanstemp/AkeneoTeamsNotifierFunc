using AppInsights.Core.Models;

namespace AppInsights.Core.Services.Parsers.Implementations
{
    public class VendorIssuesParser : BaseLogParser
    {
        public VendorIssuesParser() : base(LogType.VendorIssues)
        {
        }

        public override string Query => @"
            traces
            | where message contains ""Product is not assigned a vendor""
            | where timestamp > ago({0}h)
            | order by timestamp desc
            | project timestamp, message";

        public override LogEntry ParseLog(DateTime timestamp, string message)
        {
            return new LogEntry
            {
                Timestamp = timestamp,
                Message = message,
                LogType = LogType
            };
        }
    }
} 