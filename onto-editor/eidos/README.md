# Eidos Ontology Builder

[![Build and Deploy](https://github.com/henboffman/ontology-builder/actions/workflows/azure-deploy.yml/badge.svg)](https://github.com/henboffman/ontology-builder/actions/workflows/azure-deploy.yml)
[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Azure](https://img.shields.io/badge/Azure-Deployed-0078D4?logo=microsoftazure)](https://eidosonto.com)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

A modern web application for creating, managing, and visualizing ontologies built with Blazor Server and .NET 9.

🌐 **Live Site**: [https://eidosonto.com](https://eidosonto.com)

## Features

### Core Functionality
- **Visual Ontology Editor**: Interactive graph-based editor using Cytoscape.js
- **Concept Management**: Create and organize concepts with properties, relationships, and hierarchies
- **Real-time Collaboration**: Multi-user editing with SignalR
- **Collaborator Tracking**: View all users and guests with access to an ontology, including edit history and activity timelines
- **Activity Monitoring**: Track all changes with before/after snapshots for version control
- **Import/Export**: Support for TTL (Turtle) format
- **User Authentication**: OAuth support for GitHub, Google, and Microsoft

### User Experience
- **Dark Mode**: Built-in light/dark theme switching
- **Mobile Responsive**: Optimized for desktop, tablet, and mobile devices
- **Keyboard Shortcuts**: Efficient navigation and editing
- **Search & Filter**: Quick concept and relationship discovery

### Technical Features
- **Azure Integration**: Deployed on Azure App Service with SQL Database
- **Application Insights**: Monitoring and telemetry with cost controls
- **Security**: HTTPS-only, CORS protection, rate limiting, Key Vault integration
- **CI/CD**: Automated deployment via GitHub Actions

## Technology Stack

- **Frontend**: Blazor Server (.NET 9), Bootstrap 5, Cytoscape.js
- **Backend**: ASP.NET Core, Entity Framework Core
- **Database**: Azure SQL Database
- **Authentication**: ASP.NET Core Identity with OAuth
- **Hosting**: Azure App Service (Linux containers)
- **Monitoring**: Azure Application Insights
- **CI/CD**: GitHub Actions

## Getting Started

### Prerequisites
- .NET 9.0 SDK
- SQL Server or Docker (for local development)
- Azure CLI (for deployment)

### Local Development

1. **Clone the repository**
   ```bash
   git clone https://github.com/YOUR_USERNAME/ontology-builder.git
   cd ontology-builder
   ```

2. **Set up local database**
   ```bash
   # Option 1: Using Docker
   docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong!Passw0rd" \
     -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest

   # Option 2: Configure connection to existing SQL Server
   # Edit onto-editor/eidos/appsettings.Development.json
   ```

3. **Run database migrations**
   ```bash
   cd onto-editor/eidos
   dotnet ef database update
   ```

4. **Configure OAuth (optional)**
   ```bash
   # Set up user secrets for development
   dotnet user-secrets init
   dotnet user-secrets set "Authentication:GitHub:ClientId" "YOUR_CLIENT_ID"
   dotnet user-secrets set "Authentication:GitHub:ClientSecret" "YOUR_CLIENT_SECRET"
   # Repeat for Google and Microsoft OAuth
   ```

5. **Run the application**
   ```bash
   dotnet run
   ```

6. **Open browser**
   - Navigate to: https://localhost:7216

### Running Tests

```bash
cd Eidos.Tests
dotnet test
```

## Deployment

The application is configured for automated deployment to Azure using GitHub Actions.

See [DEPLOYMENT.md](DEPLOYMENT.md) for detailed setup instructions.

### Quick Deploy
1. Create GitHub repository and push code
2. Configure `AZURE_WEBAPP_PUBLISH_PROFILE` secret in GitHub
3. Merge to `main` branch - automatic deployment triggers

## Project Structure

```
ontology-builder/
├── onto-editor/
│   └── eidos/                 # Main Blazor application
│       ├── Components/        # Blazor components
│       ├── Data/             # EF Core DbContext and repositories
│       ├── Models/           # Domain models
│       ├── Services/         # Business logic
│       ├── Middleware/       # Custom middleware
│       ├── wwwroot/          # Static files, CSS, JS
│       └── Program.cs        # App configuration
├── Eidos.Tests/              # Unit and integration tests
├── .github/
│   └── workflows/            # GitHub Actions CI/CD
├── DEPLOYMENT.md             # Deployment guide
└── README.md                 # This file
```

## Key Components

### Graph Visualization (`wwwroot/js/graphVisualization.js`)
- Interactive node-based graph editor
- Touch-friendly for mobile devices
- Auto-resize and responsive layout

### Collaborator Tracking System
- **CollaboratorPanel Component**: View all users and guests with access to an ontology
- **Activity Tracking**: Records all changes with before/after snapshots for future version control
- **Edit Statistics**: Per-user breakdown of creates, updates, and deletes across concepts, relationships, and properties
- **Activity Timeline**: Recent changes with timestamps and descriptions
- **Permission Management**: View-only, View & Add, Can Edit, and Full Access levels
- **Guest Sessions**: Track anonymous users accessing via share links

### Authentication
- OAuth integration (GitHub, Google, Microsoft)
- Secure credential storage in Azure Key Vault
- Session management

### Database
- Entity Framework Core migrations
- Azure SQL Database with Managed Identity
- Optimized queries and indexing
- Activity tracking with versioning support

## Configuration

### Environment Variables
- `ASPNETCORE_ENVIRONMENT`: Development/Production
- `KeyVault__Uri`: Azure Key Vault endpoint
- `ConnectionStrings__DefaultConnection`: Database connection

### Azure Services
- **App Service**: eidos (Premium V2)
- **SQL Database**: eidos-p1 (GeneralPurpose)
- **Key Vault**: eidos
- **Application Insights**: eidos-insights (300 MB/day cap)

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

The CI will automatically build and test your changes. After approval and merge to `main`, changes deploy automatically to production.

## Security

- All secrets stored in Azure Key Vault
- HTTPS-only with TLS 1.2 minimum
- Rate limiting enabled
- Content Security Policy configured
- OAuth token validation
- SQL injection protection via EF Core

## License

[Your License Here]

## Support

For issues, questions, or contributions:
- Open an issue on GitHub
- Contact: [Your Contact Info]

## Acknowledgments

- Built with [Blazor](https://blazor.net)
- Graph visualization powered by [Cytoscape.js](https://js.cytoscape.org/)
- Hosted on [Azure](https://azure.microsoft.com)
