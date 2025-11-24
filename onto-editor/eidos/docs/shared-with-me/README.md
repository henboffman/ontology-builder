# Shared With Me Feature

## Overview

The "Shared with Me" feature enables users to discover, manage, and access ontologies that have been shared with them through either direct share links or group memberships. This feature provides a dedicated dashboard section where users can view recently accessed shared ontologies with comprehensive filtering, sorting, and personalization options.

## Feature Documentation

This directory contains comprehensive documentation for the Shared With Me feature:

- **[requirements.md](./requirements.md)** - Complete requirements specification including user stories, acceptance criteria, and technical requirements
- **[architecture.md](./architecture.md)** - Technical architecture and design documentation
- **[design-decisions.md](./design-decisions.md)** - Key architectural decisions and rationale
- **[implementation.md](./implementation.md)** - Implementation guide and code documentation

## Quick Links

- [User Guide](../user-guides/WORKSPACES_AND_NOTES.md) - End-user documentation
- [Release Notes](../release-notes/) - Feature announcements and changelog

## Key Capabilities

### Access Types
Users can access ontologies through two mechanisms:
- **Share Links** - Direct sharing via unique URL tokens
- **Group Membership** - Access through user group permissions

### User Actions
- View recently accessed shared ontologies (default: 90 days)
- Pin favorite ontologies for quick access
- Hide unwanted ontologies from the list
- Dismiss ontologies permanently
- Filter by access type (share link vs group)
- Search by ontology name or description
- Sort by multiple criteria (last accessed, name, size, etc.)

### Visual Indicators
- Shared badge/icon on ontology cards
- Access type indicators (share link vs group)
- Owner information display
- Pin status visualization
- Concept and relationship counts

## Technical Stack

- **Backend**: .NET 9, Entity Framework Core, Minimal APIs
- **Frontend**: Blazor Server (pending implementation)
- **Database**: SQL Server with EF Core migrations
- **Architecture**: Service layer pattern with caching

## Current Status

**Phase**: Backend Complete, Frontend In Progress

**Completed:**
- ✅ Requirements gathering
- ✅ Architecture design
- ✅ Data models (SharedOntologyUserState, DTOs)
- ✅ Database migration
- ✅ Backend service (SharedOntologyService)
- ✅ API endpoints (8 endpoints)
- ✅ Documentation organization

**In Progress:**
- 🔄 Frontend components
- 🔄 Visual indicators and styling

**Pending:**
- ⏳ Unit and integration tests
- ⏳ End-to-end workflow testing

## Quick Start for Developers

### Run Database Migration

```bash
dotnet ef database update
```

### Access API Endpoints

Base URL: `/api/shared-ontologies`

**Get Shared Ontologies:**
```bash
POST /api/shared-ontologies
Content-Type: application/json

{
  "includeHidden": false,
  "pinnedOnly": false,
  "accessTypeFilter": null,
  "daysBack": 90,
  "searchTerm": "",
  "sortBy": "LastAccessed",
  "page": 1,
  "pageSize": 24
}
```

**Pin an Ontology:**
```bash
POST /api/shared-ontologies/{ontologyId}/pin
```

See [implementation.md](./implementation.md) for complete API documentation.

## Related Features

- **Ontology Sharing** - Share ontologies via links or groups
- **User Groups** - Manage group memberships
- **Collaboration Board** - Discover and join collaboration projects
- **Permissions System** - Fine-grained access control

## Development Timeline

- **November 18, 2025** - Initial requirements gathering and architecture
- **November 19, 2025** - Backend implementation complete
- **November 19, 2025** - Frontend implementation in progress

## Contributors

This feature was developed collaboratively with AI assistance (Claude Code) following a structured agent-based development process.

---

**Last Updated**: November 19, 2025
