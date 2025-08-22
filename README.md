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

## Setup (Console Application)

> **📱 Application Type**: These setup instructions are specifically for the **Console Application** version of this project. Web application deployments require different Azure AD authentication configurations.

### 1. Clone the Repository

```bash
git clone <your-repo-url>
cd SPEAgentWithRetrieval
```

### 2. Configure Azure App Registration

> **📝 Important**: These instructions are specifically for **Console Applications**. Web applications require different authentication platform configurations (SPA or Web platform with different redirect URIs and flows).

1. **Create an Azure App Registration** in your tenant:
   - Go to [Azure Portal](https://portal.azure.com)
   - Navigate to **Azure Active Directory** → **App registrations**
   - Click **New registration**
   - Name: your app name
   - Supported account types: **Accounts in this organizational directory only (Single tenant)**
   - Click **Register**

2. **Configure Authentication Platform** (Critical for Console Applications):
   - Go to **Authentication** in the left menu
   - Under **Platform configurations**:
     - **Remove** any **Single-page application** platforms (these cause authentication conflicts)
     - Click **Add a platform** → **Mobile and desktop applications**
     - Set redirect URI to: `http://localhost`
     - Click **Configure**

3. **Configure API Permissions** (Delegated):
   - Go to **API permissions** in the left menu
   - Click **Add a permission** → **Microsoft Graph** → **Delegated permissions**
   - Add these permissions:
     - `Files.Read.All` (for SharePoint file access)
     - `Sites.Read.All` (for SharePoint site access)
     - 'FileStorageContainer.Selected` deleted and application (if you need SharePointEmbedded container access)
   - Click **Grant admin consent** for your organization

5. **Note Important IDs**:
   - Copy the **Application (client) ID** from the Overview page
   - Copy the **Directory (tenant) ID** from the Overview page
   - You'll need these for your `appsettings.json`

> **⚠️ Common Issue**: If you get authentication errors like `AADSTS9002327` or `AADSTS7000218`, it means your app registration is configured as a Single-Page Application instead of a Public Client. Make sure to remove all SPA platforms and only use Mobile/Desktop platform with `http://localhost` redirect URI.

> **🌐 Web Application Note**: If you're building a web application instead of a console app, you'll need to configure the authentication platform differently:
> - Use **Single-page application** or **Web** platform instead of Mobile/Desktop
> - Set appropriate redirect URIs for your web app (e.g., `https://localhost:5001/signin-oidc`)
> - **Do NOT** enable "Allow public client flows" for web applications

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

#### Error: `AADSTS9002327` - "Tokens issued for the 'Single-Page Application' client-type..."
**Cause**: App registration is configured as SPA instead of Public Client  
**Solution**: 
1. Go to Azure Portal → App registrations → Your app → Authentication
2. Remove all **Single-page application** platforms
3. Keep only **Mobile and desktop applications** with `http://localhost` redirect URI
4. Ensure **Allow public client flows** is **Enabled**

#### Error: `AADSTS7000218` - "The request body must contain the following parameter: 'client_assertion' or 'client_secret'"
**Cause**: App registration is configured as Confidential Client instead of Public Client  
**Solution**:
1. Go to Azure Portal → App registrations → Your app → Authentication
2. Set **Allow public client flows** to **Yes**
3. Use **Mobile and desktop applications** platform (not Web or SPA)

#### General Authentication Troubleshooting
- Verify app registration has "Allow public client flows" enabled
- Ensure delegated permissions are granted with admin consent
- Check that redirect URI `http://localhost` is configured
- Remove any SPA or Web platform configurations that might conflict

### SharePoint Access
- Verify the user has access to the SharePoint content
- Check the `FilterExpression` path is correct
- Ensure `Sites.Read.All` and `Files.ReadWrite.All` permissions are granted

### Azure AI Foundry
- Verify the project endpoint URL is correct
- Ensure the model name matches your deployment
- Check Azure AI Foundry resource permissions

## Quick Fix Scripts

For convenience, this repository includes automation scripts to fix common Azure AD app registration issues:

### Bash Script (macOS/Linux)
```bash
./fix-azure-app-registration.sh
```

### PowerShell Script (Windows/Cross-platform)
```powershell
./fix-azure-app-registration.ps1
```

These scripts will automatically:
- Remove SPA platform configurations
- Add Mobile/Desktop platform with correct redirect URI
- Enable public client flows
- Display current configuration for verification

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Ensure `appsettings.json` is not committed
5. Submit a pull request

## License

This project is licensed under the MIT License.
