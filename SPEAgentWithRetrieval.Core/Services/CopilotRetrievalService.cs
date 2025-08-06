using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using SPEAgentWithRetrieval.Core.Models;
using System.Text.Json;
using System.Text;
using System.Net.Http.Headers;

namespace SPEAgentWithRetrieval.Core.Services;

public class CopilotRetrievalService : IRetrievalService
{
    private readonly Microsoft365Options _microsoft365Options;
    private readonly ChatSettingsOptions _chatSettings;
    private readonly ILogger<CopilotRetrievalService> _logger;
    private readonly ITokenProvider _tokenProvider;

    public CopilotRetrievalService(
        IOptions<Microsoft365Options> microsoft365Options,
        IOptions<ChatSettingsOptions> chatSettings,
        ITokenProvider tokenProvider,
        ILogger<CopilotRetrievalService> logger)
    {
        _microsoft365Options = microsoft365Options.Value;
        _chatSettings = chatSettings.Value;
        _logger = logger;
        _tokenProvider = tokenProvider;
    }

    public async Task<List<RetrievedContent>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Searching for query: {Query}", query);

            // Prepare the retrieval request body
            var requestBody = new
            {
                queryString = query,
                datasource = "sharepoint",
                filterExpression = _microsoft365Options.FilterExpression,
                maximumNumberOfResults = _chatSettings.TopK,
                resourceMetadata = new[] { "title", "author", "lastModifiedDateTime" }
            };

            // Convert to JSON
            var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });

            // Create HTTP request
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, _microsoft365Options.CopilotRetrievalEndpoint)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            // Get token from token provider
            var token = await _tokenProvider.GetTokenAsync(cancellationToken);
            
            if (string.IsNullOrEmpty(token))
            {
                throw new InvalidOperationException("Authentication token is required but was not available");
            }
            
            httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            // Send request
            var httpClient = new HttpClient();
            var response = await httpClient.SendAsync(httpRequestMessage, cancellationToken);
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Retrieval API call returned Unauthorized (401): {Error}", errorContent);
                throw new InvalidOperationException("Authentication failed with Microsoft Graph API. Token may be invalid or expired.");
            }
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Retrieval API call failed with status: {StatusCode}, Error: {Error}", response.StatusCode, errorContent);
                return new List<RetrievedContent>();
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var retrievalResponse = JsonSerializer.Deserialize<CopilotRetrievalResponse>(responseContent, 
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            var retrievedContent = new List<RetrievedContent>();

            if (retrievalResponse?.RetrievalHits != null)
            {
                foreach (var hit in retrievalResponse.RetrievalHits)
                {
                    var content = string.Join("\n", hit.Extracts?.Select(e => e.Text) ?? new List<string>());
                    
                    retrievedContent.Add(new RetrievedContent
                    {
                        Title = hit.ResourceMetadata?.Title ?? "Unknown",
                        Content = content,
                        Url = hit.WebUrl ?? "",
                        Source = "SharePoint"
                    });
                }
            }

            _logger.LogInformation("Retrieved {Count} items", retrievedContent.Count);
            return retrievedContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving content for query: {Query}", query);
            return new List<RetrievedContent>();
        }
    }
}

// Response models for the Copilot Retrieval API
public class CopilotRetrievalResponse
{
    public List<RetrievalHit>? RetrievalHits { get; set; }
}

public class RetrievalHit
{
    public string? WebUrl { get; set; }
    public List<TextExtract>? Extracts { get; set; }
    public string? ResourceType { get; set; }
    public ResourceMetadata? ResourceMetadata { get; set; }
}

public class TextExtract
{
    public string? Text { get; set; }
}

public class ResourceMetadata
{
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? LastModifiedDateTime { get; set; }
}
