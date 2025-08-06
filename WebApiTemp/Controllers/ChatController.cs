using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SPEAgentWithRetrieval.Core.Models;
using SPEAgentWithRetrieval.Core.Services;

namespace WebApiTemp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatController> _logger;
    private readonly Microsoft365Options _microsoft365Options;

    public ChatController(
        IChatService chatService,
        IOptions<Microsoft365Options> microsoft365Options,
        ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _microsoft365Options = microsoft365Options.Value;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Chat([FromBody] WebChatRequest request)
    {
        try
        {
            // Check if authentication is required and token is provided
            if (!_microsoft365Options.AllowAnonymousRequests && string.IsNullOrEmpty(request.AccessToken))
            {
                _logger.LogWarning("Authentication token is required but not provided");
                return Unauthorized(new { message = "Authentication token is required" });
            }
            
            // Set the token in the token provider if provided
            if (!string.IsNullOrEmpty(request.AccessToken))
            {
                _logger.LogInformation("Access token provided from web client");
                var tokenProvider = HttpContext.RequestServices.GetRequiredService<ITokenProvider>();
                tokenProvider.SetExternalToken(request.AccessToken);
            }
            else
            {
                _logger.LogInformation("No access token provided, will attempt to use interactive auth if enabled");
            }
            
            // Pass the user's message to the chat service
            var chatRequest = new ChatRequest { Message = request.Message };
            var response = await _chatService.ProcessChatAsync(chatRequest);
            
            return Ok(response);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("token", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError(ex, "Authentication error processing chat request");
            return Unauthorized(new { message = "Authentication failed. Please sign in again." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat request");
            return StatusCode(500, new { message = "An error occurred while processing your request" });
        }
    }
}

// New request model to accept access tokens from the web frontend
public class WebChatRequest
{
    public string Message { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
}
