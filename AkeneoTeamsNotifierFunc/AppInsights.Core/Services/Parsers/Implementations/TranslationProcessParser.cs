using AppInsights.Core.Models;

namespace AppInsights.Core.Services.Parsers.Implementations
{
    public class TranslationProcessParser : BaseLogParser
    {
        public TranslationProcessParser() : base(LogType.TranslationProcess)
        {
        }

        public override string Query => @"
            traces
            | where message contains ""Finished Translation""
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