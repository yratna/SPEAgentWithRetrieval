# Azure AI Chat Agent with SharePoint RAG

This project implements a chat agent using Azure AI Foundry SDK that retrieves and grounds responses on SharePoint content through Microsoft 365 Copilot APIs.

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
       "ModelName": "gpt-4.1"
     },
     "Microsoft365": {
       "TenantId": "your-tenant-id-guid",
       "ClientId": "your-client-id-guid",
       "FilterExpression": "path:\"https://your-tenant.sharepoint.com/your-content-path/\""
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

### Services

- **`ChatService`**: Orchestrates the RAG pipeline
- **`FoundryService`**: Handles Azure AI Foundry interactions
- **`CopilotRetrievalService`**: Manages SharePoint content retrieval

### Authentication

- **User Context**: Uses `InteractiveBrowserCredential` for delegated permissions
- **Token Caching**: Automatic token refresh without re-authentication
- **Scoped Access**: Only accesses content the user has permissions to view

### Configuration

- **Strongly-typed**: Uses IOptions pattern for type-safe configuration
- **Environment-aware**: Supports multiple configuration files
- **Sensitive Data**: Protected through .gitignore and example files

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
