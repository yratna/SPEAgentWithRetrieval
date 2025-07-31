# Azure AI Chat Agent with SharePoint RAG

This project implements a chat agent using Azure AI Foundry SDK that retrieves and grounds responses on SharePoint Embedded content through Microsoft 365 Copilot Retrieval API. Note that SPE datasource is in private preview for the Retrieval API. 

This agent uses Azure AI Foundry and Retrieval API to enable contract managers reason with their documents.

## Features

- **Azure AI Foundry Integration**: Uses Azure AI SDK for chat completions with configurable models
- **SharePoint Content Retrieval**: Leverages Microsoft 365 Copilot Retrieval API for content grounding
- **User Authentication**: Interactive browser authentication with token caching
- **Configurable Filtering**: SharePoint path-based content filtering
- **RAG Implementation**: Retrieval-augmented generation with proper source attribution

## Prerequisites

- .NET 8.0 SDK
- Azure AI Foundry resource
- Microsoft 365 tenant with SharePoint
- Azure App Registration with delegated permissions

## Setup

### 1. Clone the Repository

```bash
git clone <your-repo-url>
cd SPEAgentWithRetrieval
```

### 2. Configure Azure App Registration

1. Create an Azure App Registration in your tenant
2. Configure **Authentication**:
   - Enable "Allow public client flows" = **Yes**
   - Add redirect URI: `http://localhost`
3. Configure **API Permissions** (Delegated):
   - `Microsoft Graph` → `Files.Read.All`
   - `Microsoft Graph` → `Sites.Read.All`
   - Grant admin consent for these permissions

### 3. Configure Application Settings

1. Copy the example configuration:
   ```bash
   cp appsettings.example.json appsettings.json
   ```

2. Update `appsettings.json` with your values:
   ```json
   {
     "AzureAIFoundry": {
       "ProjectEndpoint": "https://your-foundry-resource.services.ai.azure.com",
       "ModelName": "gpt-4.1" //or your model name
     },
     "Microsoft365": {
       "TenantId": "your-tenant-id-guid",
       "ClientId": "your-client-id-guid",
       "FilterExpression": "path:\"https://your-tenant.sharepoint.com/your-content-path/\"" //or any SharePoint URL. Check Retrieval API documentation for the URL
     }
   }
   ```

### 4. Install Dependencies

```bash
dotnet restore
```

### 5. Build and Run

```bash
dotnet build
dotnet run
```

## Usage

1. **First Run**: The application will open a browser for Microsoft 365 authentication
2. **Subsequent Runs**: Tokens are cached, no re-authentication needed
3. **Ask Questions**: Type questions about your SharePoint content
4. **View Sources**: Responses include source document citations

## Architecture

### Overview
The application is structured around the following components:

#### 1. Retrieval Layer
- **Purpose**: Fetches content from SharePoint using Microsoft Graph API.
- **Key Component**: `CopilotRetrievalService.cs`
  - Retrieves SharePoint content.
  - Implements chunking and embedding strategies for retrieval-augmented generation (RAG).
  - Handles authentication and API calls using Microsoft Graph SDK.

#### 2. Synthesis Layer
- **Purpose**: Generates responses using Azure AI Foundry SDK.
- **Key Component**: `FoundryService.cs`
  - Synthesizes responses based on retrieved content.
  - Implements chat completions and content generation patterns.
  - Uses Azure AI SDK for .NET.

#### 3. Orchestration Layer
- **Purpose**: Coordinates retrieval and synthesis processes.
- **Key Component**: `ChatService.cs`
  - Sequentially orchestrates retrieval and synthesis.
  - Implements async/await patterns for I/O operations.
  - Handles error management and logging.

#### 4. Presentation Layer
- **Purpose**: Displays synthesized responses and sources to the user.
- **Key Component**: `Program.cs`
  - Manages user input and output.
  - Displays top sources and synthesized responses.

#### 5. Configuration and Logging
- **Purpose**: Manages application settings and logs.
- **Key Components**:
  - `appsettings.json`: Stores configuration settings.
  - `ILogger`: Implements structured logging for debugging and monitoring.

#### 6. Authentication
- **Purpose**: Ensures secure access to APIs.
- **Key Component**: Token caching with `InteractiveBrowserCredential`.

### Architecture Diagram

```
+---------------------+
|   Presentation      |
|      Layer          |
|   (Program.cs)      |
+---------------------+
          |
          v
+---------------------+
|   Orchestration     |
|      Layer          |
|   (ChatService.cs)  |
+---------------------+
          |
          v
+---------------------+       +---------------------+
|   Retrieval Layer   |       |   Synthesis Layer   |
| (CopilotRetrieval   |       |   (FoundryService)  |
|    Service.cs)      |       |                     |
+---------------------+       +---------------------+
          |                           |
          v                           v
+---------------------+       +---------------------+
| Microsoft Graph API |       | Azure AI Foundry    |
|   (SharePoint)      |       |   SDK              |
+---------------------+       +---------------------+
```

## Security

- **No Secrets in Code**: All sensitive configuration in `appsettings.json` (git-ignored)
- **Delegated Permissions**: Respects user's SharePoint access rights
- **Token Security**: Uses Azure Identity SDK for secure token handling

## Troubleshooting

### Authentication Issues
- Verify app registration has "Allow public client flows" enabled
- Ensure delegated permissions are granted with admin consent
- Check that redirect URI `http://localhost` is configured

### SharePoint Access
- Verify the user has access to the SharePoint content
- Check the `FilterExpression` path is correct
- Ensure `Sites.Read.All` and `Files.Read.All` permissions are granted

### Azure AI Foundry
- Verify the project endpoint URL is correct
- Ensure the model name matches your deployment
- Check Azure AI Foundry resource permissions

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Ensure `appsettings.json` is not committed
5. Submit a pull request

## License

This project is licensed under the MIT License.
