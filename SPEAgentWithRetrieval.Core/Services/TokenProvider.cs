using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Options;
using SPEAgentWithRetrieval.Core.Models;

namespace SPEAgentWithRetrieval.Core.Services;

public interface ITokenProvider
{
    Task<string> GetTokenAsync(CancellationToken cancellationToken = default);
    void SetExternalToken(string token);
}

public class TokenProvider : ITokenProvider
{
    private readonly Microsoft365Options _microsoft365Options;
    private readonly TokenCredential _credential;
    private string? _externalToken;

    public TokenProvider(IOptions<Microsoft365Options> microsoft365Options)
    {
        _microsoft365Options = microsoft365Options.Value;

        // Configure the credential based on settings
        _credential = _microsoft365Options.UseUserAuthentication
            ? new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions
            {
                TenantId = _microsoft365Options.TenantId,
                ClientId = _microsoft365Options.ClientId,
                RedirectUri = new Uri("http://localhost:5001/auth-callback"),
                TokenCachePersistenceOptions = new TokenCachePersistenceOptions
                {
                    Name = "SPEAgentAuthCache",
                    UnsafeAllowUnencryptedStorage = true // For development only
                },
                BrowserCustomization = new BrowserCustomizationOptions
                {
                    UseEmbeddedWebView = false
                }
            })
            : new DefaultAzureCredential();
    }

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        // If an external token was provided (e.g., from the web UI), use it
        if (!string.IsNullOrEmpty(_externalToken))
        {
            return _externalToken;
        }

        try
        {
            // Otherwise, get a token from the credential
            var token = await _credential.GetTokenAsync(
                new TokenRequestContext(_microsoft365Options.Scopes),
                cancellationToken);
                
            return token.Token;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to acquire token: {ex.Message}", ex);
        }
    }

    public void SetExternalToken(string token)
    {
        _externalToken = token;
    }
}
