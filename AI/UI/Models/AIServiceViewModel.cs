namespace UI.Models
{
    public class AIServiceViewModel
    {
        public bool UseOpenAI { get; set; }
        public string AIServiceName => UseOpenAI ? "OpenAI" : "ClaudeAI";
        public string AIServiceType => UseOpenAI ? "OpenAI" : "ClaudeAI";
    }
}
