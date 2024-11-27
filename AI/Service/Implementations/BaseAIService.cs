using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Service.interfaces;
using Service.Models;

public abstract class BaseAIService
{
    protected readonly HttpClient client;
    protected readonly string factoryInfoPath;
    protected readonly string inventoryPath;
    protected readonly string ordersPath;
    private readonly IWebhookService _reboxService;
    private JObject inventoryJson;



    protected BaseAIService(string factoryInfoPath, string inventoryPath, string ordersPath, IWebhookService reboxService)
    {
        this.factoryInfoPath = factoryInfoPath;
        this.inventoryPath = inventoryPath;
        this.ordersPath = ordersPath;
        this.client = new HttpClient();
        _reboxService = reboxService;

    }

    public abstract Task<(string response, string missingDetail)> GetResponse(string message, string conversationHistory, bool includeOrderInfo, UserDetails userDetails);

    protected abstract void SetupHttpClient();

    protected async Task<string> BuildBasePromptAsync(string message, string conversationHistory, bool includeOrderInfo, UserDetails userDetails)
    {
        var factoryInfo = await File.ReadAllTextAsync(factoryInfoPath);

        await DownloadInventory();
        var inventoryData = inventoryJson != null ? inventoryJson.ToString() : "Inventory data is not available";
        Trace.TraceInformation($"inventoryJson status: {inventoryJson != null}");

        // Summarize or truncate the inventory data if it's too long
        if (inventoryData.Length > 1000)
        {
            inventoryData = inventoryData.Substring(0, 1000) + "...";
        }

        var prompt = $"You are a customer service assistant for a computer software marketing business. Use the following information about the business to answer questions:\n{factoryInfo}\n\n";
        prompt += $"\n\nConversation History:\n{conversationHistory}\n\nCustomer Message:\n{message}\n\n";
        if (userDetails != null)
        {
            prompt += $"\n\nCustomer Details:\n First Name: {userDetails.FirstName}\n Last Name: {userDetails.LastName}";
        }
        else
        {
            prompt += "\n\nCustomer details are not available.";
        }
        prompt += $"\n\nIf the customer asks about an item or items from the inventory, use the following inventory information: {inventoryData}";

        if (includeOrderInfo)
        {
            var orderNumber = ExtractOrderNumber(message);
            var orderDetailsResult = GetOrderDetails(orderNumber);
            if (orderDetailsResult != null)
            {
                var jsonData = JsonConvert.SerializeObject(orderDetailsResult, new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });
                prompt += $"\n\nIf the customer asks about an order, use the following order information, and write a table with all the data in HTML in Bootstrap 5 RTL format in Hebrew: {jsonData}";
            }
        }

        if (message.ToLower().Contains("invoice") || message.ToLower().Contains("חשבונית") || message.ToLower().Contains("חשבוניות"))
        {
            var invoicesJson = await RetrievingInvoicesDataAsync();
            prompt += $"\n\nUse the following invoice information to find the customer's invoices by the email or phone number in the customer's message: and present the information clearly and neatly, with a line break between each item {invoicesJson}";
        }
        var softwareProducts = new List<string>();
        if (inventoryJson != null)
        {
            foreach (var item in inventoryJson["items"])
            {
                var description = item.First["description"]?.ToString();
                if (!string.IsNullOrEmpty(description) && (description.Contains("software", StringComparison.OrdinalIgnoreCase) || description.Contains("תוכנה", StringComparison.OrdinalIgnoreCase)))
                {
                    softwareProducts.Add(description);
                }
            }
        }
        if (softwareProducts.Count > 0)
        {
            prompt += "\n\nThe following products are available in our inventory:\n";
            foreach (var product in softwareProducts)
            {
                prompt += $"- {product}\n";
            }
        }
        else
        {
            prompt += "\n\nUnfortunately, I did not find specific products in our inventory.";
        }

        var queryType = ExtractQueryType(message);
        if (queryType == "price")
        {
            prompt += " Provide the price of the requested phone model.";
        }
        else if (queryType == "description")
        {
            prompt += " Provide a description of the requested phone model.";
        }
        else
        {
            prompt += " Provide the requested information.";
        }

        // Add the instructions for the AI to respond in Hebrew
        prompt += "\n\n Please respond in Hebrew.";
        prompt += "\n Do not include the customer's name in your answers.";
        prompt += "\n Do not say 'According to the information I received.'";
        prompt += "\n Answer the customer's question briefly and focused, with one or two sentences.";
        prompt += "\n Your response should not exceed 50 words.";
        prompt += "\n If you do not know the answer, say you do not know; do not provide an answer you are not sure of.";

        Trace.TraceInformation($"Prompt for AI: {prompt}");
        return prompt;
    }


    protected string ExtractQueryType(string message)
    {
        if (message.Contains("price") || message.Contains("מחיר"))
        {
            return "price";
        }
        else if (message.Contains("description") || message.Contains("תיאור"))
        {
            return "description";
        }
        else
        {
            return "general";
        }
    }

    protected string ExtractOrderNumber(string message)
    {
        if (!string.IsNullOrEmpty(message))
        {
            var match = Regex.Match(message, @"\d+");
            return match.Value.Trim();
        }
        return message;
    }

    protected object GetOrderDetails(string orderNumber)
    {
        // Implement your logic to get the order details from your database
        return null;
    }




    public bool AreUserDetailsComplete(UserDetails userDetails)
    {
        if (userDetails == null)
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(userDetails.FirstName) &&
               !string.IsNullOrWhiteSpace(userDetails.LastName) &&
               !string.IsNullOrWhiteSpace(userDetails.Email) &&
               !string.IsNullOrWhiteSpace(userDetails.Phone);
    }

    public async Task SendUserDetailsToWebhookIfComplete(UserDetails userDetails)
    {
        if (AreUserDetailsComplete(userDetails) && !userDetails.DetailsSentToWebhook)
        {
            await _reboxService.SendDataToWebhook(userDetails);
            userDetails.DetailsSentToWebhook = true;
        }
    }

    private async Task<bool> DownloadInventory()
    {
        try
        {
            var inventoryUrl = "https://api.icount.co.il/api/v3.php/inventory/get_items?cid=kenionLTD&user=gvia&pass=gvia";
            var inventoryData = await DownloadStringFromUrlAsync(inventoryUrl);
            Trace.TraceInformation($" inventory data: {inventoryData}"); // Log the raw inventory data
            //await File.WriteAllTextAsync(inventoryPath, inventoryData);
            inventoryJson = JObject.Parse(inventoryData);
            return true;
        }
        catch (Exception ex)
        {
            Trace.TraceError($"error in - DownloadInventory: {ex}");
            return false;
        }
    }

    private async Task<JObject> RetrievingInvoicesDataAsync()
    {
        try
        {
            var apiUrl = "https://api.icount.co.il/api/v3.php/doc/search?cid=kenionLTD&user=gvia&pass=gvia&status=0&limit=10&detail_level=10"; // Consider securing the credentials
            var invoicesData = await DownloadStringFromUrlAsync(apiUrl);
            Trace.TraceInformation($"Raw invoices data retrieved: {invoicesData}"); // Ensure no sensitive data is logged
            var invoicesJson = JObject.Parse(invoicesData);
            return invoicesJson;
        }
        catch (Exception ex)
        {
            Trace.TraceError($"Error in RetrievingInvoiceData: {ex}");
            throw; // Consider whether to rethrow or handle the exception gracefully here
        }
    }

    private async Task<string> DownloadStringFromUrlAsync(string url)
    {
        using var httpClient = new HttpClient();
        return await httpClient.GetStringAsync(url);
    }
}
