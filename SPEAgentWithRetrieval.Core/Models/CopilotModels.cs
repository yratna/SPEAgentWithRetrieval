namespace SPEAgentWithRetrieval.Core.Models;

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
