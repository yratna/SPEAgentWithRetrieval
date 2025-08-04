using Azure.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using SPEAgentWithRetrieval.Core.Models;
using Microsoft.Graph;
using System.Text.Json;
using System.Text;
using System.Net.Http.Headers;

namespace SPEAgentWithRetrieval.Core.Services;

public class CopilotRetrievalService : IRetrievalService
{
    private readonly GraphServiceClient _graphClient;
    private readonly Microsoft365Options _microsoft365Options;
    private readonly ChatSettingsOptions _chatSettings;
    private readonly ILogger<CopilotRetrievalService> _logger;
    private readonly Azure.Core.TokenCredential? _credential;
    private readonly Func<Task<string>>? _tokenProvider;
    private InteractiveBrowserCredential? _browserCredential;

    public CopilotRetrievalService(
        IOptions<Microsoft365Options> microsoft365Options,
        IOptions<ChatSettingsOptions> chatSettings,
        ILogger<CopilotRetrievalService> logger,
        Func<Task<string>>? tokenProvider = null)
    {
        _microsoft365Options = microsoft365Options.Value;
        _chatSettings = chatSettings.Value;
        _logger = logger;
        _tokenProvider = tokenProvider;

        if (_tokenProvider == null)
        {
            // Use Interactive Browser Authentication for user context - store as field for reuse
            _credential = _microsoft365Options.UseUserAuthentication
                ? new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions
                {
                    TenantId = _microsoft365Options.TenantId,
                    ClientId = _microsoft365Options.ClientId,
                    RedirectUri = new Uri("http://localhost")
                })
                : new DefaultAzureCredential();

            _graphClient = new GraphServiceClient(_credential, _microsoft365Options.Scopes);
        }
        else
        {
            // Use provided token provider with custom auth provider
            _graphClient = new GraphServiceClient(new HttpClient(new CustomAuthenticationProvider(_tokenProvider)));
        }
    }

    // Custom authentication provider class
    private class CustomAuthenticationProvider : DelegatingHandler
    {
        private readonly Func<Task<string>> _tokenProvider;

        public CustomAuthenticationProvider(Func<Task<string>> tokenProvider)
        {
            _tokenProvider = tokenProvider;
            InnerHandler = new HttpClientHandler();
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await _tokenProvider();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await base.SendAsync(request, cancellationToken);
        }
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

            // Send request through Graph client
            var httpClient = new HttpClient();
            var token = await GetAccessTokenAsync();
            httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            var response = await httpClient.SendAsync(httpRequestMessage, cancellationToken);
            
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

    private async Task<string> GetAccessTokenAsync()
    {
        // If token provider is available, use it
        if (_tokenProvider != null)
        {
            return await _tokenProvider();
        }

        // Otherwise use the credential
        if (_credential != null)
        {
            var token = await _credential.GetTokenAsync(
                new Azure.Core.TokenRequestContext(_microsoft365Options.Scopes),
                CancellationToken.None);
            return token.Token;
        }

        // Fallback to interactive browser credential
        _browserCredential ??= new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions
        {
            TenantId = _microsoft365Options.TenantId,
            ClientId = _microsoft365Options.ClientId,
            RedirectUri = new Uri("http://localhost")
        });
        
        var fallbackToken = await _browserCredential.GetTokenAsync(
            new Azure.Core.TokenRequestContext(_microsoft365Options.Scopes),
            CancellationToken.None);
        return fallbackToken.Token;
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
