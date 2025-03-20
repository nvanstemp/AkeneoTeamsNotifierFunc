using AppInsights.Core.Models;

namespace AppInsights.Core.Services.Parsers.Implementations
{
    public class ErrorParser : BaseLogParser
    {
        public ErrorParser() : base(LogType.Error)
        {
        }

        public override string Query => @"
            traces
            | where message contains ""ERROR""
            | where message contains ""Failed""
            | where not(message contains ""0 failed"")
            | where not(message contains ""Failed Validation"")
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