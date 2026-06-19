# Investigation Visualization Platform

A C# .NET 8 Web API application providing advanced visualization capabilities for insurance fraud investigation, featuring interactive network analysis, geospatial mapping, and temporal analysis.

## Architecture

- **Backend**: ASP.NET Core 8 Web API
- **Network Visualization**: vis-network.js (interactive graph visualization)
- **Geospatial**: Mapbox GL JS (maps, heatmaps, location markers)
- **Reporting**: Power BI Embedded (geospatial & temporal reports)
- **Graph Database**: Neo4j (entity relationships, network analysis)
- **Relational Database**: MySQL (claims, entities, alerts)

## Features

### Network Visualization (vis-network.js)
- Interactive network graphs for alert investigation
- Relationship exploration with configurable depth
- Entity type and relationship filtering
- Time-based network analysis
- Path analysis (shortest path between entities)
- Network metrics (degree centrality, density)

### Geospatial Visualization (Mapbox)
- Entity location mapping (providers, claimants)
- Claim incident location visualization
- Heatmap for claim density analysis
- GeoJSON API endpoints

### Temporal Analysis
- Claims timeline with filtering
- Alerts timeline by severity
- Activity summaries (daily/weekly/monthly)
- Date range filtering

### Power BI Integration
- Embedded Power BI reports for geospatial analysis
- Embedded Power BI reports for temporal visualization
- Azure AD authentication for secure embedding

## Prerequisites

- .NET 8 SDK
- Docker & Docker Compose
- Mapbox Access Token (for geospatial features)
- Power BI Pro/Premium license & Azure AD app (for Power BI embedding)

## Quick Start

### 1. Start Infrastructure (MySQL + Neo4j)

```bash
docker-compose up -d mysql neo4j
```

### 2. Configure Settings

Edit `src/Visualization.API/appsettings.json`:
- Set your Mapbox access token
- Configure Power BI settings (optional)

### 3. Run the API

```bash
cd src/Visualization.API
dotnet run
```

The application will:
- Auto-create the MySQL database schema
- Seed sample data (entities, claims, alerts)
- Seed Neo4j with graph relationships
- Serve the frontend at `http://localhost:5000`

### 4. Using Docker Compose (Full Stack)

```bash
docker-compose up --build
```

Access the application at `http://localhost:5000`

## API Endpoints

### Network (`/api/network`)
- `GET /api/network` - Full network graph with optional filters
- `GET /api/network/entity/{id}` - Entity-centered network
- `GET /api/network/path?from={id}&to={id}` - Shortest path
- `GET /api/network/metrics` - Network centrality metrics
- `GET /api/network/timeline?startDate=&endDate=` - Time-based network
- `POST /api/network/seed` - Seed Neo4j data

### Geospatial (`/api/geospatial`)
- `GET /api/geospatial/entities` - Entity locations (GeoJSON)
- `GET /api/geospatial/claims` - Claim locations (GeoJSON)
- `GET /api/geospatial/heatmap` - Heatmap data
- `GET /api/geospatial/config` - Mapbox configuration

### Temporal (`/api/temporal`)
- `GET /api/temporal/claims` - Claims timeline
- `GET /api/temporal/alerts` - Alerts timeline
- `GET /api/temporal/summary?period=month` - Activity summary

### Power BI (`/api/powerbi`)
- `GET /api/powerbi/embed/{reportId}` - Embed configuration
- `GET /api/powerbi/settings` - Report settings
- `GET /api/powerbi/embed/geospatial` - Geospatial report embed
- `GET /api/powerbi/embed/temporal` - Temporal report embed

### Data (`/api/data`)
- `GET /api/data/entities` - List entities
- `GET /api/data/claims` - List claims
- `GET /api/data/alerts` - List alerts
- `GET /api/data/stats` - Dashboard statistics
- `POST /api/data/seed` - Seed MySQL data

## Sample Data

The application includes pre-configured sample data:
- **5 Providers** (medical centers, auto repair shops)
- **8 Claimants** with varied locations across NYC
- **5 Policies** with different coverage types
- **8 Claims** (Auto, Health, Property) with various statuses
- **7 Alerts** (Fraud, Duplicate, Anomaly) with severity levels
- **18 Neo4j relationships** (FILED_CLAIM_AT, REFERRED_TO, KNOWS, SAME_ADDRESS)

## Configuration

### Mapbox
```json
{
  "Mapbox": {
    "AccessToken": "YOUR_MAPBOX_TOKEN",
    "Style": "mapbox://styles/mapbox/dark-v11"
  }
}
```

### Power BI
```json
{
  "PowerBI": {
    "TenantId": "your-azure-ad-tenant-id",
    "ClientId": "your-app-client-id",
    "ClientSecret": "your-app-secret",
    "WorkspaceId": "your-powerbi-workspace-id",
    "GeospatialReportId": "your-geospatial-report-id",
    "TemporalReportId": "your-temporal-report-id"
  }
}
```

## Swagger

API documentation is available at `/swagger` when the application is running.
