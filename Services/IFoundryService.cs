using SPEAgentWithRetrieval.Models;

namespace SPEAgentWithRetrieval.Services;

public interface IFoundryService
{
    Task<string> GenerateResponseAsync(string userMessage, List<RetrievedContent> context, CancellationToken cancellationToken = default);
}
