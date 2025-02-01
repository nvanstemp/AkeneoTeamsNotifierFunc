using AppInsights.Core.Models;

namespace AppInsights.Core.Services
{
    public interface ILogParser
    {
        string Query { get; }
        LogType LogType { get; }
        LogEntry ParseLog(DateTime timestamp, string message);
    }
} 