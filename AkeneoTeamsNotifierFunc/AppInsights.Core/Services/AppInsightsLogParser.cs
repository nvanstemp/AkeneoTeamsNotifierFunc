using AppInsights.Core.Models;
using System.Text.RegularExpressions;
using System.Text.Json;
using AppInsights.Core.Services.Parsers;

namespace AppInsights.Core.Services
{
    public class AppInsightsLogParser
    {
        private readonly HttpClient _httpClient;
        private readonly AppInsightsSettings _settings;
        private static readonly Regex VendorCodeRegex = new Regex(@"Code:\s*([^,\s]+)", RegexOptions.Compiled);
        private static readonly Regex IssueRegex = new Regex(@"Issue:\s*(.+)$", RegexOptions.Compiled);
        private readonly ILogParserFactory _parserFactory;

        public AppInsightsLogParser(AppInsightsSettings settings, ILogParserFactory parserFactory)
        {
            _settings = settings;
            _parserFactory = parserFactory;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_settings.BaseUrl)
            };
            
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _settings.ApiKey);
        }

        public async Task<Dictionary<LogType, IEnumerable<LogEntry>>> GetTraceMessagesAsync(TimeSpan timeSpan)
        {
            var logsByType = new Dictionary<LogType, IEnumerable<LogEntry>>();

            foreach (LogType logType in Enum.GetValues(typeof(LogType)))
            {
                var parser = _parserFactory.GetParser(logType);
                var formattedQuery = string.Format(parser.Query, timeSpan.TotalHours);
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
                var url = $"{_settings.AppId}/query?timespan={timespanParam}&query={encodedQuery}";

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