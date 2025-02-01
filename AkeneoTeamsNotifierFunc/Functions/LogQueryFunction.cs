using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using AppInsights.Core.Services;
using AkeneoTeamsNotifierFunc.Services;

namespace AkeneoTeamsNotifierFunc.Functions
{
    public class LogQueryFunction
    {
        private readonly AppInsightsLogParser _logParser;
        private readonly TeamsNotificationService _teamsService;
        private static readonly TimeSpan _queryTimeSpan = TimeSpan.FromHours(1);

        public LogQueryFunction(
            AppInsightsLogParser logParser,
            TeamsNotificationService teamsService)
        {
            _logParser = logParser;
            _teamsService = teamsService;
        }

        [Function("QueryLogs")]
        public async Task<HttpResponseData> RunAsync(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
        {
            try
            {
                var logs = await _logParser.GetTraceMessagesAsync(_queryTimeSpan);
                
                // Send to Teams
                await _teamsService.SendNotificationAsync(logs);
                
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new { 
                    message = "Logs have been sent to Teams channel",
                    timeSpan = $"Last {_queryTimeSpan.TotalHours} hour(s)"
                });
                return response;
            }
            catch (Exception ex)
            {
                var response = req.CreateResponse(HttpStatusCode.InternalServerError);
                await response.WriteStringAsync($"Error processing logs: {ex.Message}");
                return response;
            }
        }
    }
} 