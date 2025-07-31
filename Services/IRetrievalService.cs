using SPEAgentWithRetrieval.Models;

namespace SPEAgentWithRetrieval.Services;

public interface IRetrievalService
{
    Task<List<RetrievedContent>> SearchAsync(string query, CancellationToken cancellationToken = default);
}
