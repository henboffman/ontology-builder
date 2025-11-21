# Eidos

**Shape the essence of knowledge**

A modern, production-ready web-based ontology editor built with Blazor Server and .NET 9. Create, visualize, and manage ontologies with an intuitive interface featuring interactive graph visualization, TTL import/export, OAuth authentication, and enterprise-grade security.

## Features

### Core Functionality

- **Interactive Graph Visualization**: Real-time force-directed graph using D3.js with drag-and-drop node positioning
- **Multiple View Modes**: Switch between Graph, List, TTL, Notes, and Template views
- **Multi-Format Export**: Export ontologies in TTL (Turtle), JSON, or CSV formats for different use cases
- **TTL Import**: Import standard Turtle (TTL) format ontology files with dotNetRDF
- **Custom Concept Templates**: Create reusable concept templates organized by category and type
- **Relationship Management**: Define and visualize relationships between concepts with custom labels
- **Recent Ontologies Sidebar**: Quick access to your 10 most recently updated ontologies

### User Interface

- **Responsive Design**: Works on desktop and mobile devices
- **Toast Notifications**: Non-intrusive feedback for user actions
- **Confirm Dialogs**: Safe deletion and destructive action confirmations
- **Ontology Settings**: Configure namespace, author, license, tags, and notes
- **Real-time Updates**: Interactive server-side rendering for instant feedback
- **Keyboard Shortcuts**: Comprehensive keyboard shortcuts for power users (press `?` to view)
- **Search and Filter**: Real-time search across concepts with instant filtering
- **Help System**: Built-in keyboard shortcuts dialog and comprehensive user guide

### Security & Authentication

- **OAuth 2.0 Integration**: Login with GitHub, Google, or Microsoft accounts
- **ASP.NET Core Identity**: Industry-standard user authentication and authorization
- **Azure Key Vault**: Secure secrets management for production deployments
- **Rate Limiting**: Protection against DDoS and brute force attacks
- **Security Headers**: CSP, HSTS, X-Frame-Options, and more
- **HTTPS Enforcement**: Automatic HTTP to HTTPS redirection
- **Strong Password Requirements**: Enforced password complexity and lockout policies

### Data Management

- **Dual Database Support**:
  - **Development**: SQL Server via Docker (production parity)
  - **Production**: Azure SQL Database with high availability
- **Entity Framework Core**: Robust ORM with migrations and DbContextFactory
- **Time-based Formatting**: Human-readable timestamps ("2h ago", "3d ago")
- **Automatic Tracking**: CreatedAt and UpdatedAt timestamps on all entities
- **Data Persistence**: Docker volumes for local development data retention

## Technology Stack

### Backend

- **.NET 9**: Latest .NET framework with C# 13
- **Blazor Server**: Interactive server-side web framework with SignalR
- **ASP.NET Core Identity**: Authentication and user management
- **Entity Framework Core 9**: ORM with migrations and DbContextFactory

### Database

- **SQL Server 2022**: Production database (Azure SQL Database)
- **Docker SQL Server**: Local development with production parity
- **Azure Key Vault**: Secure secrets and connection string management

### Security

- **OAuth 2.0 Providers**: GitHub, Google, Microsoft authentication
- **AspNetCoreRateLimit**: IP-based rate limiting middleware
- **Azure Identity**: Managed Identity and DefaultAzureCredential

### Data Processing

- **dotNetRDF**: RDF and TTL file processing library
- **D3.js**: Force-directed graph visualization
- **Bootstrap 5**: Responsive UI framework with Bootstrap Icons

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for local SQL Server)
- A modern web browser (Chrome, Firefox, Safari, or Edge)
- [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) (optional, for Key Vault integration)

## Getting Started

### Quick Start (5 minutes)

1. **Clone the repository:**

```bash
git clone <repository-url>
cd onto-editor
```

2. **Start SQL Server:**

```bash
docker-compose up -d
```

3. **Configure OAuth (required):**

Create OAuth apps and store credentials in User Secrets:

```bash
# GitHub OAuth (required)
dotnet user-secrets set "Authentication:GitHub:ClientId" "your-client-id"
dotnet user-secrets set "Authentication:GitHub:ClientSecret" "your-client-secret"

# Google OAuth (optional)
dotnet user-secrets set "Authentication:Google:ClientId" "your-client-id"
dotnet user-secrets set "Authentication:Google:ClientSecret" "your-client-secret"
```

See [SECURITY-SETUP.md](SECURITY-SETUP.md) for detailed OAuth setup instructions.

4. **Run the application:**

```bash
dotnet run
```

5. **Open your browser:**

```
http://localhost:5026
```

### Development Workflow

**Daily workflow:**

```bash
# Morning: Start SQL Server
docker-compose up -d

# Run with hot reload
dotnet watch run

# Evening: Stop SQL Server (optional - keeps data)
docker-compose down
```

**Database management:**

```bash
# View SQL Server status
docker-compose ps

# View SQL Server logs
docker-compose logs -f sqlserver

# Reset database (deletes all data)
docker-compose down -v
docker-compose up -d
```

See [DOCKER-SETUP.md](DOCKER-SETUP.md) for complete Docker SQL Server documentation.

## Documentation

### User Documentation

- **[USER_GUIDE.md](USER_GUIDE.md)**: Comprehensive user guide covering all features, workflows, and best practices
- **Keyboard Shortcuts**: Press `?` in the application to view all keyboard shortcuts
- **In-app Help**: Built-in help dialogs and tooltips throughout the interface

### Developer Documentation

- **[DOCKER-SETUP.md](DOCKER-SETUP.md)**: Complete Docker SQL Server setup, troubleshooting, and daily workflow
- **[SECURITY-SETUP.md](SECURITY-SETUP.md)**: OAuth configuration, secrets management, and security best practices
- **[AZURE-DEPLOYMENT.md](AZURE-DEPLOYMENT.md)**: Step-by-step Azure deployment guide with infrastructure setup
- **[AZURE-QUICK-START.md](AZURE-QUICK-START.md)**: 15-minute Azure deployment quick start guide
- **[KEYVAULT-TESTING.md](KEYVAULT-TESTING.md)**: Testing Azure Key Vault integration locally

## Keyboard Shortcuts

Press `?` (question mark) while using the application to see the full keyboard shortcuts dialog. Here are some frequently used shortcuts:

### Navigation

- `Alt+G` - Switch to Graph view
- `Alt+L` - Switch to List view
- `Alt+T` - Switch to TTL view
- `Alt+N` - Switch to Notes view
- `Alt+P` - Switch to Templates view

### Editing

- `Ctrl+K` - Add new concept
- `Ctrl+R` - Add new relationship
- `Ctrl+I` - Import TTL
- `Ctrl+,` - Open ontology settings

### Search

- `Ctrl+F` - Focus search box (in List view)

### General

- `?` - Show keyboard shortcuts help
- `Esc` - Close dialogs

For a complete list of shortcuts and detailed usage instructions, see the [User Guide](USER_GUIDE.md#keyboard-shortcuts).

## Project Structure

```
onto-editor/
├── Components/
│   ├── Layout/
│   │   ├── MainLayout.razor          # Main application layout
│   │   └── NavMenu.razor             # Sidebar navigation with recent ontologies
│   ├── Pages/
│   │   ├── Home.razor                # Landing page
│   │   ├── OntologyView.razor        # Main ontology editor
│   │   └── GraphVisualization.razor  # Standalone graph view
│   ├── Ontology/
│   │   ├── ConceptEditor.razor       # Add/edit concepts
│   │   ├── RelationshipEditor.razor  # Add/edit relationships
│   │   ├── GraphView.razor           # D3.js graph visualization
│   │   ├── ListView.razor            # Table view of concepts
│   │   ├── TtlView.razor             # TTL format view/export
│   │   ├── NotesView.razor           # Ontology notes editor
│   │   ├── TemplateManager.razor     # Custom concept templates
│   │   ├── ViewModeSelector.razor    # View mode toggle buttons
│   │   ├── OntologyHeader.razor      # Ontology title and metadata
│   │   ├── OntologySettingsDialog.razor  # Settings modal
│   │   └── TtlImportDialog.razor     # TTL import modal
│   └── Shared/
│       ├── ToastNotification.razor   # Toast notification component
│       └── ConfirmDialog.razor       # Confirmation dialog component
├── Services/
│   ├── OntologyService.cs            # Core ontology business logic
│   ├── OntologyTemplateService.cs    # Template management
│   ├── TtlExportService.cs           # Export to TTL format
│   ├── TtlImportService.cs           # Import from TTL files
│   ├── ToastService.cs               # Toast notification service
│   └── ConfirmService.cs             # Confirmation dialog service
├── Models/
│   ├── OntologyModels.cs             # Domain models (Ontology, Concept, Relationship)
│   └── ViewMode.cs                   # View mode enumeration
├── Data/
│   └── OntologyDbContext.cs          # Entity Framework database context
├── wwwroot/
│   ├── css/                          # Custom stylesheets
│   └── js/
│       └── graphVisualization.js     # D3.js graph implementation
├── Program.cs                        # Application entry point
└── OntologyBuilder.csproj            # Project file
```

## Usage Guide

### Creating an Ontology

1. Click "Create New Ontology" on the home page
2. Enter a name for your ontology
3. Optionally configure settings (namespace, author, license)

### Adding Concepts

1. Switch to Graph or List view
2. Click "Add Concept"
3. Enter the concept name
4. Select a category and type (or create custom ones via Templates)
5. Add optional description and examples

### Creating Relationships

1. In Graph or List view, click "Add Relationship"
2. Select the source concept
3. Select the target concept
4. Choose a relationship type
5. Optionally add a custom label

### Importing TTL Files

1. Click "Import TTL" in the ontology view
2. Paste your TTL content
3. Click "Import" to parse and load concepts/relationships

### Exporting to TTL

1. Switch to TTL view mode
2. Click "Copy TTL" to copy to clipboard
3. Or manually copy the displayed TTL output

### Using Templates

1. Switch to Templates view
2. Click "Add Template" to create a custom concept template
3. Specify category, type, description, examples, and color
4. Templates will appear in the concept editor dropdown

## Database Schema

The application uses SQL Server (Docker for dev, Azure SQL for prod) with the following tables:

### Core Tables

- **AspNetUsers**: User accounts with ASP.NET Core Identity
  - Id, Username, DisplayName, Email, PasswordHash, SecurityStamp, etc.

- **Ontologies**: Top-level ontology metadata
  - Id, UserId, Name, Namespace, Author, License, Tags, Notes, UsesBFO, UsesProvO, CreatedAt, UpdatedAt

- **Concepts**: Ontology concepts/classes
  - Id, OntologyId, Name, Category, Type, Definition, SimpleExplanation, Examples, Color, SourceOntology, PositionX, PositionY, CreatedAt

- **Relationships**: Connections between concepts
  - Id, OntologyId, SourceConceptId, TargetConceptId, RelationType, Label, Description, OntologyUri, Strength, CreatedAt

- **Properties**: Concept properties/attributes
  - Id, ConceptId, Name, Value, DataType, Description

- **CustomConceptTemplates**: Reusable concept templates
  - Id, OntologyId, Category, Type, Description, Examples, Color, CreatedAt, UpdatedAt

### Supporting Tables

- **OntologyLinks**: External ontology references
  - Id, OntologyId, Uri, Name, Prefix, Description, ConceptsImported, ImportedConceptCount

- **FeatureToggles**: Application feature flags
  - Id, Key, Name, Description, IsEnabled, Category, CreatedAt, UpdatedAt

- **Users**: Legacy user table (being phased out in favor of AspNetUsers)
  - Id, Username, DisplayName, Email, CreatedAt, LastLoginAt

## Configuration

### Development Configuration (`appsettings.Development.json`)

```json
{
  "DetailedErrors": true,
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=EidosDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;"
  },
  "KeyVault": {
    "Uri": ""
  }
}
```

### Production Configuration

In production, configure via:

- **Azure App Service Configuration**: Set connection strings and app settings
- **Azure Key Vault**: Store secrets securely
- **Managed Identity**: Automatic authentication for Azure resources

See [AZURE-DEPLOYMENT.md](AZURE-DEPLOYMENT.md) for complete production setup.

## Development

### Adding New Features

1. **New Components**: Add Razor components in `Components/` folder
2. **New Services**: Create services in `Services/` folder and register in `Program.cs`
3. **Database Changes**: Update models in `Models/` and create migrations:

   ```bash
   dotnet ef migrations add YourMigrationName
   dotnet ef database update
   ```

### Code Style

- Use nullable reference types (`#nullable enable`)
- Follow C# naming conventions (PascalCase for public members)
- Keep components focused and single-purpose
- Use dependency injection for services

## Deployment

### Azure Deployment

Deploy to Azure App Service with these steps:

1. **Quick Deployment (15 minutes)**: Follow [AZURE-QUICK-START.md](AZURE-QUICK-START.md)
2. **Full Deployment Guide**: See [AZURE-DEPLOYMENT.md](AZURE-DEPLOYMENT.md) for complete infrastructure setup

**What gets deployed:**

- Azure App Service (Linux, .NET 9)
- Azure SQL Database (production database)
- Azure Key Vault (secrets management)
- Managed Identity (secure authentication)
- Custom domain with SSL (optional)

**Before deploying:**

- Rotate OAuth credentials (see [SECURITY-SETUP.md](SECURITY-SETUP.md))
- Store secrets in Azure Key Vault
- Configure App Service settings
- Set up GitHub OAuth with Azure domain

## Troubleshooting

### Docker SQL Server Issues

**Container won't start:**

```bash
# Check Docker is running
docker info

# View container logs
docker-compose logs sqlserver

# Restart Docker Desktop
```

**Port 1433 already in use:**

```bash
# Use different port in docker-compose.yml
ports:
  - "1434:1433"

# Update connection string
Server=localhost,1434;...
```

**Connection refused:**

```bash
# Wait for SQL Server to start (can take 10-20s)
docker-compose logs -f sqlserver

# Look for: "SQL Server is now ready for client connections"
```

See [DOCKER-SETUP.md](DOCKER-SETUP.md#troubleshooting) for complete troubleshooting guide.

### Database Issues

**Reset local database:**

```bash
# Delete Docker volume (destroys all data)
docker-compose down -v
docker-compose up -d
```

**Migration errors:**

```bash
# Create new migration
dotnet ef migrations add YourMigrationName

# Apply migrations
dotnet ef database update
```

### Authentication Issues

**OAuth errors:**

- Verify Client ID and Client Secret in User Secrets
- Check OAuth redirect URIs match your domain
- Ensure GitHub/Google/Microsoft OAuth app is configured

**Key Vault connection errors:**

```bash
# Verify Azure CLI login
az login

# Check Key Vault access
az keyvault secret list --vault-name eidos
```

### Port Already in Use

```bash
# Kill process on port 5026
lsof -ti:5026 | xargs kill -9

# Or use different port
dotnet run --urls "http://localhost:5001"
```

### Graph Not Rendering

1. Check browser console for JavaScript errors
2. Ensure D3.js is loading from CDN
3. Verify CSP headers allow unpkg.com
4. Check that concepts and relationships exist

## Contributing

Contributions are welcome! Please follow these steps:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License and Usage

This project is open source and available under the MIT License, with the following usage restrictions:

### Commercial Use Policy

**Effective Date: November 20, 2025**

- **Future Commercial Use**: Commercial use of this software is **prohibited** for any usage beginning on or after November 20, 2025.
- **Past Commercial Use**: Any commercial use that began prior to November 20, 2025 is **permitted perpetually** under the original MIT License terms.

### Permitted Use

The following uses are permitted without restriction:

- Personal projects and learning
- Academic and research purposes
- Non-profit organizations
- Educational institutions
- Open source projects
- Internal business tools (non-commercial deployment)

### Definition of Commercial Use

Commercial use includes, but is not limited to:

- Selling the software or derivative works
- Using the software to provide paid services
- Incorporating the software into commercial products
- Deploying the software in revenue-generating applications
- Using the software at or for a commercial entity

### Grandfathering Clause

Organizations or individuals who began using this software for commercial purposes prior to November 20, 2025 may continue such use indefinitely under the original MIT License terms, including the right to receive updates and modifications.

## Security

This project implements enterprise-grade security practices:

- **OAuth 2.0**: Industry-standard authentication
- **Rate Limiting**: 100 requests/min general, 5 attempts/5min login
- **Security Headers**: CSP, HSTS, X-Frame-Options, X-Content-Type-Options
- **HTTPS Enforcement**: Automatic redirection with HSTS
- **Strong Passwords**: 8+ chars, uppercase, lowercase, numbers, symbols
- **Account Lockout**: 5 failed attempts = 15 minute lockout
- **Secure Cookies**: HttpOnly, Secure, SameSite=Lax
- **Azure Key Vault**: Production secrets management
- **Managed Identity**: Passwordless Azure authentication

See [SECURITY-SETUP.md](SECURITY-SETUP.md) for security configuration and best practices.

## Acknowledgments

**Eidos** (εἶδος) - from ancient Greek, meaning "form, essence, ideal form"

- Built with [Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor) and [.NET 9](https://dotnet.microsoft.com/download/dotnet/9.0)
- Graph visualization powered by [D3.js](https://d3js.org/)
- RDF processing by [dotNetRDF](https://dotnetrdf.org/)
- Authentication via [ASP.NET Core Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity)
- Icons from [Bootstrap Icons](https://icons.getbootstrap.com/)
- Hosted on [Azure App Service](https://azure.microsoft.com/en-us/products/app-service/)

## Support

For issues, questions, or suggestions:

- Open an issue on the GitHub repository
- See documentation in the `/docs` folder
- Review security practices in [SECURITY-SETUP.md](SECURITY-SETUP.md)

---

**Eidos** - Shape the essence of knowledge

Built with .NET 9, Blazor Server, and Azure
