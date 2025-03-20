using AppInsights.Core.Models;

namespace AppInsights.Core.Services
{
    public interface ILogTypeIdentifier
    {
        LogType DetermineLogType(string message);
    }
}