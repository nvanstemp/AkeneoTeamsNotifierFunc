using AppInsights.Core.Models;
using AppInsights.Core.Services.Parsers.Implementations;

namespace AppInsights.Core.Services.Parsers
{
    public interface ILogParserFactory
    {
        ILogParser GetParser(LogType logType);
    }

    public class LogParserFactory : ILogParserFactory
    {
        private readonly Dictionary<LogType, ILogParser> _parsers;

        public LogParserFactory()
        {
            _parsers = new Dictionary<LogType, ILogParser>
            {
                { LogType.VendorIssues, new VendorIssuesParser() },
                { LogType.IsPublished, new IsPublishedParser() },
                { LogType.VendorFailedValidation, new VendorFailedValidationParser() },
                { LogType.Error, new ErrorParser() },
                { LogType.DuplicateModel, new DuplicateModelParser() },
                { LogType.Success, new SuccessParser() },
                { LogType.SyncProcess, new SyncProcessParser() },
                { LogType.TranslationProcess, new TranslationProcessParser() }
            };
        }

        public ILogParser GetParser(LogType logType)
        {
            if (_parsers.TryGetValue(logType, out var parser))
            {
                return parser;
            }

            throw new ArgumentException($"No parser found for log type: {logType}", nameof(logType));
        }
    }
} 