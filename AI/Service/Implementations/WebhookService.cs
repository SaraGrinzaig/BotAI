using Service.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Service.interfaces;


namespace Service.Implementations
{
    public class WebhookService : IWebhookService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<WebhookService> _logger;
        private readonly string _webhookUrl;


        public WebhookService(HttpClient httpClient, ILogger<WebhookService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _webhookUrl = configuration["WebhookUrl"];
        }


        public async Task SendDataToWebhook(UserDetails userDetails)
        {
            try
            {
                _logger.LogInformation($"Attempting to send user details to webhook for user: {userDetails.Email}");

                var response = await _httpClient.PostAsJsonAsync(_webhookUrl, userDetails);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Successfully sent user details to webhook for user: {userDetails.Email}");
                }
                else
                {
                    _logger.LogWarning($"Failed to send user details to webhook. Status code: {response.StatusCode}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Webhook response: {responseContent}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending user details to webhook for user: {userDetails.Email}");
                throw;
            }
        }
    }
}
