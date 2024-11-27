using Microsoft.Extensions.Configuration;
using Service.interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Implementations
{
    public class AIServiceFactory
    {
        private readonly string factoryInfoPath;
        private readonly string inventoryPath;
        private readonly string ordersPath;
        private readonly IWebhookService webhookService;
        private readonly IConfiguration configuration;

        public AIServiceFactory(string factoryInfoPath, string inventoryPath, string ordersPath, IWebhookService webhookService, IConfiguration configuration)
        {
            this.factoryInfoPath = factoryInfoPath;
            this.inventoryPath = inventoryPath;
            this.ordersPath = ordersPath;
            this.webhookService = webhookService;
            this.configuration = configuration;
        }

        public BaseAIService CreateAIService(bool useOpenAI)
        {
            return useOpenAI
                ? new OpenAIService(factoryInfoPath, inventoryPath, ordersPath, webhookService, configuration)
                : new ClaudeService(factoryInfoPath, inventoryPath, ordersPath, webhookService, configuration);
        }
    }
}
