namespace SPEAgentWithRetrieval.Core.Models;

public class AzureAIFoundryOptions
{
    public const string SectionName = "AzureAIFoundry";
    
    public string ProjectEndpoint { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
}

public class Microsoft365Options
{
    public const string SectionName = "Microsoft365";
    
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string CopilotRetrievalEndpoint { get; set; } = "https://graph.microsoft.com/beta/copilot/retrieval";
    public string FilterExpression { get; set; } = string.Empty;
    public bool UseUserAuthentication { get; set; } = true;
    public string[] Scopes { get; set; } = { "https://graph.microsoft.com/Files.Read.All", "https://graph.microsoft.com/Sites.Read.All" };
}

public class ChatSettingsOptions
{
    public const string SectionName = "ChatSettings";
    
    public int MaxTokens { get; set; } = 1000;
    public float Temperature { get; set; } = 0.7f;
    public int TopK { get; set; } = 5;
}
