using System.Text.RegularExpressions;
using AppInsights.Core.Models;

namespace AppInsights.Core.Services.Parsers
{
    public abstract class BaseLogParser : ILogParser
    {
        protected static readonly Regex VendorCodeRegex = new Regex(@"Code:\s*([^,\s]+)", RegexOptions.Compiled);
        protected static readonly Regex IssueRegex = new Regex(@"Issue:\s*(.+)$", RegexOptions.Compiled);

        protected readonly LogType _logType;

        protected BaseLogParser(LogType logType)
        {
            _logType = logType;
        }

        public LogType LogType => _logType;

        public abstract string Query { get; }

        public virtual LogEntry ParseLog(DateTime timestamp, string originalMessage)
        {
            var (vendorCode, message) = ParseLogMessage(originalMessage);
            
            return new LogEntry 
            { 
                Timestamp = timestamp,
                Message = message,
                VendorCode = vendorCode
            };
        }

        protected virtual (string? vendorCode, string message) ParseLogMessage(string originalMessage)
        {
            var vendorCode = ExtractVendorCode(originalMessage);
            var issueMatch = IssueRegex.Match(originalMessage);
            var message = issueMatch.Success ? issueMatch.Groups[1].Value.Trim() : originalMessage;
            
            return (vendorCode, message);
        }

        protected virtual string? ExtractVendorCode(string message)
        {
            var match = VendorCodeRegex.Match(message);
            if (match.Success)
            {
                var code = match.Groups[1].Value.Trim();
                if (!string.IsNullOrEmpty(code))
                {
                    return code;
                }
            }
            return null;
        }
    }
} 