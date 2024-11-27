using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Service.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Service.interfaces;
using Microsoft.Extensions.Configuration;

namespace Service.Implementations
{
    public class ClaudeService : BaseAIService
    {

        private const string CLAUDE_API_URL = "https://api.anthropic.com/v1/messages";
        private readonly string CLAUDE_API_KEY;

        public ClaudeService(string factoryInfoPath, string inventoryPath, string ordersPath, IWebhookService reboxService, IConfiguration configuration)
            : base(factoryInfoPath, inventoryPath, ordersPath, reboxService)
        {
            CLAUDE_API_KEY = configuration["ApiKeys:Claude"];
        }

        protected override void SetupHttpClient()
        {
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("x-api-key", CLAUDE_API_KEY);
            client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CLAUDE_API_KEY);
        }


        public override async Task<(string response, string missingDetail)> GetResponse(string message, string conversationHistory, bool includeOrderInfo, UserDetails userDetails)
        {
            try
            {
                SetupHttpClient();
                var prompt = BuildBasePromptAsync(message, conversationHistory, includeOrderInfo, userDetails).GetAwaiter().GetResult();
                var requestBody = new
                {
                    model = "claude-3-opus-20240229",
                    max_tokens = 1000,
                    messages = new[] { new { role = "user", content = prompt } }
                };

                var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(CLAUDE_API_URL, content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Claude API returned non-success status code: {response.StatusCode}");
                }

                var responseObject = JObject.Parse(responseString);
                if (responseObject["content"] is JArray contentArray && contentArray.Count > 0 &&
                    contentArray[0]["text"] != null)
                {
                    var aiResponse = contentArray[0]["text"].ToString();

                    // Check if user details are complete and send to Rebox if they are
                    //await SendUserDetailsToWebhookIfComplete(userDetails);

                    return (aiResponse, null);
                }
                else
                {
                    throw new JsonException("Unexpected JSON structure in Claude API response");
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Error in GetClaudeResponse: {ex}");
                return ($"Error: {ex.Message}", null);
            }
        }
    }


}
