using SPEAgentWithRetrieval.Models;

namespace SPEAgentWithRetrieval.Services;

public interface IChatService
{
    Task<ChatResponse> ProcessChatAsync(ChatRequest request, CancellationToken cancellationToken = default);
}
