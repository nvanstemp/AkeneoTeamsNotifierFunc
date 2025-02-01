using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using AppInsights.Core.Models;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Text.Json;

namespace AppInsights.Core.Services
{
    public class AppInsightsLogParser
    {
        private readonly HttpClient _httpClient;
        private static readonly string _appId = "3dd18304-53cf-4d7c-8c49-2417c15598dc";
        private static readonly string _apiKey = "hk7dp6cjsfoh7vs2xaad3q1posf7d61b2886n312";
        private static readonly Regex VendorCodeRegex = new Regex(@"Code:\s*([^,\s]+)", RegexOptions.Compiled);
        private static readonly Regex IssueRegex = new Regex(@"Issue:\s*(.+)$", RegexOptions.Compiled);
        private readonly Dictionary<LogType, string> _queries;
        private static readonly TimeSpan DefaultTimeSpan = TimeSpan.FromHours(1);

        public AppInsightsLogParser(string connectionString)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://api.applicationinsights.io/v1/apps/")
            };
            
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);

            _queries = new Dictionary<LogType, string>
            {
                { LogType.VendorIssues, @"
                    traces
                    | where message contains ""Product is not assigned a vendor""
                    | where timestamp > ago({0}h)
                    | order by timestamp desc
                    | project timestamp, message" },

                { LogType.IsPublished, @"
                    traces
                    | where message contains ""IsPublished""
                    | where timestamp > ago({0}h)
                    | order by timestamp desc
                    | project timestamp, message" },

                { LogType.VendorFailedValidation, @"
                    traces
                    | where message contains ""Failed Validation""
                    | where timestamp > ago({0}h)
                    | order by timestamp desc
                    | project timestamp, message" },

                { LogType.Error, @"
                    traces
                    | where message contains ""ERROR""
                    | where message contains ""Failed""
                    | where not(message contains ""0 failed"")
                    | where not(message contains ""Failed Validation"")
                    | where timestamp > ago({0}h)
                    | order by timestamp desc
                    | project timestamp, message" },

                { LogType.DuplicateModel, @"
                    traces
                    | where message contains ""Duplicate Model""
                    | where timestamp > ago({0}h)
                    | order by timestamp desc
                    | project timestamp, message" },

                { LogType.Success, @"
                    traces
                    | where message contains ""Success""
                    | where timestamp > ago({0}h)
                    | order by timestamp desc
                    | project timestamp, message" },

                { LogType.SyncProcess, @"
                    traces
                    | where message contains ""Finished Sync""
                    | where timestamp > ago({0}h)
                    | order by timestamp desc
                    | project timestamp, message" },

                { LogType.TranslationProcess, @"
                    traces
                    | where message contains ""Finished Translation""
                    | where timestamp > ago({0}h)
                    | order by timestamp desc
                    | project timestamp, message" }
            };
        }

        public async Task<Dictionary<LogType, IEnumerable<LogEntry>>> GetTraceMessagesAsync(TimeSpan timeSpan)
        {
            var logsByType = new Dictionary<LogType, IEnumerable<LogEntry>>();

            foreach (var (logType, query) in _queries)
            {
                var formattedQuery = string.Format(query, timeSpan.TotalHours);
                var logs = await ExecuteQueryAsync(formattedQuery, timeSpan);
                logsByType[logType] = logs;
            }

            return logsByType;
        }

        private async Task<List<LogEntry>> ExecuteQueryAsync(string query, TimeSpan timeSpan)
        {
            try
            {
                var encodedQuery = Uri.EscapeDataString(query);
                var timespanParam = $"PT{timeSpan.TotalHours}H";
                var url = $"{_appId}/query?timespan={timespanParam}&query={encodedQuery}";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<QueryResult>(content, options);

                var logs = new List<LogEntry>();
                
                if (result?.Tables?[0]?.Rows != null)
                {
                    foreach (var row in result.Tables[0].Rows)
                    {
                        if (row.Count >= 2)
                        {
                            var timestamp = DateTime.Parse(row[0].ToString());
                            var originalMessage = row[1].ToString();
                            var (vendorCode, message) = ParseLogMessage(originalMessage);
                            
                            logs.Add(new LogEntry 
                            { 
                                Timestamp = timestamp,
                                Message = message,
                                VendorCode = vendorCode
                            });
                        }
                    }
                }

                return logs;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error executing query: {ex.Message}", ex);
            }
        }

        private (string? vendorCode, string message) ParseLogMessage(string originalMessage)
        {
            // Extract vendor code
            var vendorCode = ExtractVendorCode(originalMessage);
            
            // Extract issue message
            var issueMatch = IssueRegex.Match(originalMessage);
            var message = issueMatch.Success ? issueMatch.Groups[1].Value.Trim() : originalMessage;
            
            return (vendorCode, message);
        }

        private string? ExtractVendorCode(string message)
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

        public async Task<IDictionary<string, List<LogEntry>>> GetGroupedVendorIssuesAsync(TimeSpan timeSpan)
        {
            var logsByType = await GetTraceMessagesAsync(timeSpan);
            
            // Get only vendor-related logs (VendorIssues and DuplicateModel types)
            var vendorLogs = logsByType
                .Where(kvp => kvp.Key == LogType.VendorIssues || kvp.Key == LogType.DuplicateModel)
                .SelectMany(kvp => kvp.Value)
                .Where(log => !string.IsNullOrEmpty(log.VendorCode))
                .ToList();

            return vendorLogs
                .GroupBy(log => log.VendorCode!)
                .ToDictionary(
                    group => group.Key,
                    group => group.ToList()
                );
        }
    }

    public class QueryResult
    {
        public List<Table> Tables { get; set; }
    }

    public class Table
    {
        public string Name { get; set; }
        public List<Column> Columns { get; set; }
        public List<List<object>> Rows { get; set; }
    }

    public class Column
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }
} 