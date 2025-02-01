using AppInsights.Core.Models;
using System.Text;

namespace AkeneoTeamsNotifierFunc.Services
{
    public class TeamsNotificationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _webhookUrl;
        private const int MaxTeamsMessageLength = 1000; // Define the maximum length for a Teams message

        public TeamsNotificationService(string webhookUrl)
        {
            _webhookUrl = webhookUrl;
            _httpClient = new HttpClient();
        }

        public async Task SendNotificationAsync(Dictionary<LogType, IEnumerable<LogEntry>> logsByType)
        {
            foreach (var (logType, logs) in logsByType)
            {
                if (!logs.Any()) continue;

                var title = GetTitleForLogType(logType);
                var messages = new List<StringBuilder> { new StringBuilder() };
                messages[0].AppendLine($"**{title}**\n");
                var currentMessage = messages[0];

                void EnsureMessageSize(string lineToAdd)
                {
                    if (currentMessage.Length + lineToAdd.Length > MaxTeamsMessageLength)
                    {
                        currentMessage = new StringBuilder();
                        currentMessage.AppendLine($"**{title}** (Continued)\n");
                        messages.Add(currentMessage);
                    }
                }

                if (logType == LogType.VendorIssues)
                {
                    var vendorGroups = logs.GroupBy(log => log.VendorCode ?? "No Code", StringComparer.OrdinalIgnoreCase);
                    foreach (var vendorGroup in vendorGroups.OrderBy(g => g.Key))
                    {
                        var line = $"Vendor: {vendorGroup.Key}: - {vendorGroup.First().Message}\n";
                        EnsureMessageSize(line);
                        currentMessage.Append(line);
                    }
                }
                else if (logType == LogType.DuplicateModel)
                {
                    var modelGroups = logs.GroupBy(log => log.VendorCode ?? "No Code", StringComparer.OrdinalIgnoreCase);
                    foreach (var modelGroup in modelGroups.OrderBy(g => g.Key))
                    {
                        var headerLine = $"Model Code: {modelGroup.Key}\n";
                        EnsureMessageSize(headerLine);
                        currentMessage.Append(headerLine);

                        foreach (var log in modelGroup.OrderBy(l => l.Timestamp))
                        {
                            var line = $"- {log.Timestamp:yyyy-MM-dd HH:mm:ss}: {log.Message}\n";
                            EnsureMessageSize(line);
                            currentMessage.Append(line);
                        }
                        currentMessage.AppendLine();
                    }
                }
                else if (NeedsGroupingByVendor(logType))
                {
                    var vendorGroups = logs.GroupBy(log => log.VendorCode ?? "No Code", StringComparer.OrdinalIgnoreCase);
                    foreach (var vendorGroup in vendorGroups.OrderBy(g => g.Key))
                    {
                        var headerLine = $"Code: {vendorGroup.Key}\n";
                        EnsureMessageSize(headerLine);
                        currentMessage.Append(headerLine);

                        foreach (var log in vendorGroup.OrderBy(l => l.Timestamp))
                        {
                            var line = $"- {log.Timestamp:yyyy-MM-dd HH:mm:ss}: {log.Message}\n";
                            EnsureMessageSize(line);
                            currentMessage.Append(line);
                        }
                        currentMessage.AppendLine();
                    }
                }
                else
                {
                    foreach (var log in logs.OrderBy(l => l.Timestamp))
                    {
                        var line = $"- {log.Timestamp:yyyy-MM-dd HH:mm:ss}: {log.Message}\n";
                        EnsureMessageSize(line);
                        currentMessage.Append(line);
                    }
                }

                // Send all messages for this log type
                for (int i = 0; i < messages.Count; i++)
                {
                    var messageText = messages[i].ToString();
                    if (messages.Count > 1)
                    {
                        messageText = $"Part {i + 1}/{messages.Count}\n{messageText}";
                    }
                    
                    if (messageText.Length > 0)
                    {
                        await SendTeamsMessageAsync(messageText);
                        if (i < messages.Count - 1)
                        {
                            await Task.Delay(1000); // Small delay between messages to maintain order
                        }
                    }
                }
            }
        }

        private LogType DetermineLogType(string message)
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

        private string GetTitleForLogType(LogType logType) => logType switch
        {
            LogType.VendorIssues => "⚠️ Vendor Issues",
            LogType.IsPublished => "📢 Publication Status",
            LogType.DuplicateModel => "🔄 Duplicate Models",
            LogType.VendorFailedValidation => "❌ Validation Failed",
            LogType.Error => "❌ Errors",
            LogType.Success => "✅ Success",
            LogType.SyncProcess => "🔄 Sync Process Updates",
            LogType.TranslationProcess => "🌐 Translation Process Updates",
            LogType.Other => "ℹ️ Other Updates",
            _ => "ℹ️ System Updates"
        };

        private bool NeedsGroupingByVendor(LogType logType) => logType switch
        {
            LogType.VendorIssues => true,
            LogType.IsPublished => true,
            LogType.DuplicateModel => true,
            LogType.VendorFailedValidation => true,
            LogType.Error => true,
            _ => false
        };

        private async Task SendTeamsMessageAsync(string messageText)
        {
            var message = new { text = messageText };
            var json = System.Text.Json.JsonSerializer.Serialize(message);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            await _httpClient.PostAsync(_webhookUrl, content);
        }
    }
} 