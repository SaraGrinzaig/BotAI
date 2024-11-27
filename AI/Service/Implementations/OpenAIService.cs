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
    public class OpenAIService : BaseAIService
    {

        private const string OPENAI_API_URL = "https://api.openai.com/v1/chat/completions";
        private readonly string OPENAI_API_KEY;

        public OpenAIService(string factoryInfoPath, string inventoryPath, string ordersPath, IWebhookService reboxService, IConfiguration configuration)
            : base(factoryInfoPath, inventoryPath, ordersPath, reboxService) 
        {
            OPENAI_API_KEY = configuration["ApiKeys:OpenAI"];
        }

        protected override void SetupHttpClient()
        {
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {OPENAI_API_KEY}");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public override async Task<(string response, string missingDetail)> GetResponse(string message, string conversationHistory, bool includeOrderInfo, UserDetails userDetails)
        {
            try
            {
                SetupHttpClient();
                var prompt = BuildBasePromptAsync(message, conversationHistory, includeOrderInfo, userDetails).GetAwaiter().GetResult(); 
                var requestBody = new
                {
                    model = "gpt-4o-mini",
                    max_tokens = 1000,
                    messages = new[] { new { role = "user", content = prompt } }
                };

                var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(OPENAI_API_URL, content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"OpenAI API returned non-success status code: {response.StatusCode}");
                }

                var responseObject = JObject.Parse(responseString);
                if (responseObject["choices"] is JArray choicesArray && choicesArray.Count > 0 &&
                    choicesArray[0]["message"]["content"] != null)
                {
                    var aiResponse = choicesArray[0]["message"]["content"].ToString();
                    return (aiResponse, null);
                }
                else
                {
                    throw new JsonException("Unexpected JSON structure in OpenAI API response");
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Error in GetOpenAIResponse: {ex}");
                return ($"Error: {ex.Message}", null);
            }
        }
    }


}
