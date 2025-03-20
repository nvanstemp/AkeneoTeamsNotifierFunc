using AppInsights.Core.Models;

namespace AppInsights.Core.Services
{
    public class LogTypeIdentifier : ILogTypeIdentifier
    {
        public LogType DetermineLogType(string message)
        {
            // Group vendor-related issues together
            if (message.Contains("Product is not assigned a vendor") ||
                message.Contains("IsPublished") ||
                message.Contains("Duplicate Model"))
                return LogType.VendorIssues;

            if (message.Contains("ERROR") || message.Contains("Failed"))
                return LogType.Error;
            if (message.Contains("Success"))
                return LogType.Success;
            if (message.Contains("Finished Sync Process"))
                return LogType.SyncProcess;
            if (message.Contains("Finished Translation Process"))
                return LogType.TranslationProcess;

            return LogType.Other;
        }
    }
}