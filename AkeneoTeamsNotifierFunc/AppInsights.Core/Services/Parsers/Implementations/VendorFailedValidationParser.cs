using AppInsights.Core.Models;

namespace AppInsights.Core.Services.Parsers.Implementations
{
    public class VendorFailedValidationParser : BaseLogParser
    {
        public VendorFailedValidationParser() : base(LogType.VendorFailedValidation)
        {
        }

        public override string Query => @"
            traces
            | where message contains ""Failed Validation""
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