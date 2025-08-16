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
            ? CreateUserCredential()
            : new DefaultAzureCredential();
    }

    private TokenCredential CreateUserCredential()
    {
        try
        {
            // First try InteractiveBrowserCredential
            return new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions
            {
                TenantId = _microsoft365Options.TenantId,
                ClientId = _microsoft365Options.ClientId,
                RedirectUri = new Uri("http://localhost"),
                TokenCachePersistenceOptions = new TokenCachePersistenceOptions
                {
                    Name = "SPEAgentAuthCache",
                    UnsafeAllowUnencryptedStorage = true // For development only
                },
                BrowserCustomization = new BrowserCustomizationOptions
                {
                    UseEmbeddedWebView = false
                }
            });
        }
        catch (Exception)
        {
            // Fallback to DeviceCodeCredential if InteractiveBrowserCredential fails
            Console.WriteLine("Falling back to Device Code authentication...");
            return new DeviceCodeCredential(new DeviceCodeCredentialOptions
            {
                TenantId = _microsoft365Options.TenantId,
                ClientId = _microsoft365Options.ClientId,
                TokenCachePersistenceOptions = new TokenCachePersistenceOptions
                {
                    Name = "SPEAgentAuthCache",
                    UnsafeAllowUnencryptedStorage = true
                },
                DeviceCodeCallback = (code, cancellation) =>
                {
                    Console.WriteLine($"\nTo authenticate, please visit: {code.VerificationUri}");
                    Console.WriteLine($"And enter the code: {code.UserCode}");
                    Console.WriteLine("Waiting for authentication to complete...");
                    return Task.CompletedTask;
                }
            });
        }
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
        catch (AuthenticationFailedException ex) when (ex.Message.Contains("AADSTS9002327"))
        {
            throw new InvalidOperationException(
                "Authentication failed: The Azure AD app registration is configured as a Single-Page Application (SPA) " +
                "but this console application requires a Public Client configuration. " +
                "Please update the app registration in Azure Portal:\n" +
                "1. Go to Azure Portal > Azure Active Directory > App registrations\n" +
                "2. Find your app and go to Authentication\n" +
                "3. Remove Single-page application platform\n" +
                "4. Add Mobile and desktop applications platform with redirect URI: http://localhost\n" +
                "5. Set 'Allow public client flows' to Yes", ex);
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
