using AppInsights.Core.Models;

namespace AppInsights.Core.Services.Parsers.Implementations
{
    public class DuplicateModelParser : BaseLogParser
    {
        public DuplicateModelParser() : base(LogType.DuplicateModel)
        {
        }

        public override string Query => @"
            traces
            | where message contains ""Duplicate Model""
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