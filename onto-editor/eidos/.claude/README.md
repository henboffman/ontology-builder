# Claude Code Context for Eidos

This directory contains comprehensive context files for Claude Code to better understand and work with the Eidos Ontology Builder project.

## Files in this Directory

### 📄 `project.json`
Structured metadata about the Eidos project including:
- Framework and language information
- Key services and components
- Quick start commands
- Recent feature history
- Naming conventions

### 📚 `guidelines.md`
Comprehensive development guidelines covering:
- Code style and naming conventions
- Async/await patterns
- Dependency injection best practices
- Error handling strategies
- Logging standards
- Permission checking patterns
- Database query optimization
- Blazor component guidelines
- SignalR hub patterns
- Testing practices
- Security best practices
- Git commit message format
- Code review checklist

### 🏗️ `architecture.md`
Detailed system architecture documentation:
- Layered architecture overview
- Technology stack deep dive
- Service layer patterns
- Repository pattern implementation
- Real-time collaboration architecture
- Permission system design
- Collaboration workflow diagrams
- Design patterns used
- Data flow examples
- Scalability considerations
- Security architecture
- Performance optimizations
- Testing architecture
- Deployment architecture

### 💡 `prompts.json`
Pre-built prompts for common development tasks:
- Add new service with repository
- Create Blazor page component
- Database migration workflow
- Permission checking implementation
- SignalR hub method creation
- Debug permission issues
- Add test coverage
- Optimize query performance
- Extend collaboration features
- Setup development environment

## Root Level Context Files

### 📖 `../claude.md`
Main project overview and context file including:
- Project overview and purpose
- Complete technology stack
- Project structure
- All core features (10 major feature areas)
- Key services descriptions
- Database schema
- Development workflow
- Important patterns and conventions
- Recent major features with dates
- Testing information
- Security considerations
- Performance optimizations
- Known issues and limitations
- Deployment configuration
- Documentation references

### 📝 `../DEVELOPMENT_LEDGER.md`
Development history and technical changelog with:
- Dated entries for all major features
- Technical implementation details
- Files modified for each feature
- Bug fixes and their resolutions
- Performance improvements
- Testing additions
- Future improvement suggestions

## How to Use These Files

### For Claude Code
These files provide context for Claude Code to:
1. Understand project structure and conventions
2. Generate code that follows established patterns
3. Provide accurate suggestions and fixes
4. Maintain consistency across the codebase
5. Reference common development workflows

### For Developers
Use these files to:
1. Onboard new team members quickly
2. Reference coding standards
3. Understand system architecture
4. Find examples of common patterns
5. Debug issues with context-aware help

## Quick Reference

### Common Commands
```bash
# Start development
dotnet restore
dotnet ef database update
dotnet run

# Run tests
dotnet test

# Create migration
dotnet ef migrations add MigrationName

# Build for production
dotnet build --configuration Release
```

### Key Service Locations
- **Business Logic**: `Services/`
- **Data Access**: `Data/Repositories/`
- **Real-time**: `Hubs/OntologyHub.cs`
- **UI Components**: `Components/`
- **Models**: `Models/`

### Important Patterns
- Always use `OntologyPermissionService` for access control
- Use `IDbContextFactory` for scoped contexts
- Include comprehensive logging with structured placeholders
- Wrap critical operations in try-catch with user-friendly errors
- Use `AsNoTracking()` for read-only queries

## Updating This Context

When adding major features:
1. Update `../claude.md` with high-level feature description
2. Update `../DEVELOPMENT_LEDGER.md` with detailed technical entry
3. Add relevant guidelines to `guidelines.md` if new patterns introduced
4. Update `architecture.md` if system design changed
5. Consider adding new prompts to `prompts.json` for the feature

## Related Documentation

- **User Guide**: Available at `/user-guide` in the running application
- **Release Notes**: Available at `/release-notes` in the running application
- **API Documentation**: XML comments throughout codebase
- **Database Schema**: See `Models/` directory and migration files

---

**Last Updated**: October 31, 2025
**Maintained by**: Development Team
**Questions?**: See `../claude.md` for contact information
