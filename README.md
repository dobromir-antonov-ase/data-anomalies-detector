# Data Anomalies Detector AI - Project Design Document

## 1. Introduction

### 1.1 Purpose
The Data Anomalies Detector AI is a comprehensive system designed to identify and analyze anomalies in automotive sales and finance data. It helps automotive dealerships detect irregularities in their operations, improve compliance, and optimize business processes through machine learning-powered analysis.

### 1.2 Scope
This system covers anomaly detection across dealer operations, finance submissions, and sales patterns to identify both individual anomalies and systemic issues.

## 2. System Architecture

### 2.1 High-Level Architecture
The application follows a client-server architecture with:
- Angular-based frontend (ASE.UI)
- .NET 9 API backend (ASE.API)
- In-memory database for development/demo

### 2.2 Deployment Architecture
- Frontend: GitHub Pages
- Backend: Azure App Service
- Communication: RESTful API with CORS

### 2.3 Component Diagram
```
┌────────────────────┐         ┌─────────────────────┐
│   ASE.UI (Angular) │         │ ASE.API (.NET Core) │
│                    │         │                     │
│  ┌──────────────┐  │         │ ┌───────────────┐   │
│  │   Core       │  │         │ │  Features     │   │
│  │   - Layout   │  │ HTTP/S  │ │  - Dealers    │   │
│  │   - Models   │◄─┼─────────┼─►- Submissions  │   │
│  │   - Services │  │         │ │  - Anomaly    │   │
│  └──────────────┘  │         │ │    Detection  │   │
│                    │         │ └───────────────┘   │
│  ┌──────────────┐  │         │                     │
│  │  Features    │  │         │ ┌───────────────┐   │
│  │  - Dashboard │  │         │ │  Services     │   │
│  │  - Dealers   │  │         │ │  - ML Models  │   │
│  │  - Anomalies │  │         │ │  - Data       │   │
│  └──────────────┘  │         │ │    Analysis   │   │
│                    │         │ └───────────────┘   │
└────────────────────┘         └─────────────────────┘
```

## 3. Frontend Design

### 3.1 UI Framework
- Angular 19.x with Material UI components
- Responsive layout with sidebar navigation
- Theme customization support

### 3.2 Key Components
- **Dashboard**: Overview of detected anomalies, statistics, charts
- **Dealers**: Management, view, and analysis of dealer data
- **Finance Submissions**: Review and management of finance documents
- **Templates**: Configuration of master templates for data validation
- **Anomaly Detection**: Visualization of detected patterns and anomalies
- **Query Builder**: AI-powered natural language query interface

### 3.3 State Management
- Angular services with reactive patterns
- HTTP client for API communication

## 4. Backend Design

### 4.1 API Architecture
- Minimal API approach using .NET 9
- Feature-based organization (vertical slices)
- RESTful endpoints with JSON responses

### 4.2 Key Features
- **Dealer Management**: CRUD operations for dealer entities
- **Finance Submissions**: Document handling and validation
- **Anomaly Detection**: Machine learning algorithms to identify:
  - Time-series anomalies
  - Data pattern irregularities
  - Cross-entity anomalies
- **Query Builder**: Natural language to data query processing

### 4.3 Data Access
- Entity Framework Core with InMemory provider (for demo)
- Structured data models with relationships

### 4.4 ML Capabilities
- Utilizing Microsoft.ML and ML.TimeSeries libraries
- Anomaly detection algorithms:
  - Spike Detection
  - Change Point Detection
  - Seasonal Trend Decomposition

## 5. Security Considerations

### 5.1 Authentication & Authorization
- HTTPS communication required
- CORS configured to restrict access to allowed domains

### 5.2 Data Protection
- Input validation and sanitization
- Rate limiting for query operations

## 6. Development & Deployment

### 6.1 Development Environment
- .NET 9 SDK
- Node.js and npm
- Visual Studio 2022 or VS Code

### 6.2 CI/CD Pipelines
- GitHub Actions for automated deployments
- Frontend deployment to GitHub Pages
- Backend deployment to Azure App Service

### 6.3 Testing Strategy
- Unit tests for business logic
- Integration tests for API endpoints
- UI component testing

## 7. Future Enhancements

### 7.1 Short-term
- Enhanced visualization capabilities
- Export and reporting features
- Expanded ML model accuracy

### 7.2 Long-term
- Real-time data processing
- Predictive analytics for future anomalies
- Multi-tenant support for larger deployments

## 8. Technical Specifications

### 8.1 Frontend
- Angular 19.x
- Material UI components
- ECharts for data visualization
- Typescript 5.7+

### 8.2 Backend
- .NET 9
- Microsoft.ML 3.0
- Entity Framework Core
- Minimal API pattern

### 8.3 Deployment
- GitHub Pages (UI)
- Azure App Service (API)
- GitHub Actions (CI/CD)

## 9. Conclusion

The Data Anomalies Detector AI provides automotive businesses with a powerful tool to identify irregularities in their operations, improve compliance, and gain insights from their data. The modern architecture ensures scalability and maintainability, while the ML-powered analysis offers sophisticated anomaly detection capabilities.



## Query Builder

The Query Builder is a powerful AI-assisted feature that translates natural language questions into structured database queries, making data exploration accessible to non-technical users.

### Key Capabilities

- **Natural Language Processing**: Convert plain English questions into SQL or LINQ queries
- **Voice Recognition**: Speak queries instead of typing using Azure Speech Services
- **Query Preview**: View generated queries with syntax highlighting
- **Result Visualization**: Display query results in tables with sorting capabilities
- **Multi-format Support**: Generate both SQL and LINQ query formats

### Technical Implementation

- **Frontend**: Angular component with voice recording capabilities and proper WAV encoding
- **Backend**: .NET service using AI to transform natural language into database queries
- **Speech Processing**:
  - Multiple fallback approaches for speech recognition
  - WAV header validation and repair
  - Push stream approach for problematic audio files
  - Diagnostic endpoints for troubleshooting

### Usage Examples

Users can ask questions like:
- "Show me sales data for the last quarter"
- "Find all dealers with compliance issues this month"
- "List the top 5 finance submissions with anomalies"
- "Compare performance between regions"

### Future Enhancements

#### Short-term
- **Direct Query Execution**: Enable immediate execution of generated queries against the database
- **Query History**: Save and recall previously executed queries
- **Enhanced Voice Recognition**: Support for industry-specific terminology
- **Export Options**: Download query results in multiple formats (CSV, Excel, PDF)

#### Long-term
- **Query Recommendations**: AI-suggested queries based on user behavior and data patterns
- **Natural Language Query Builder**: Interactive refinement of queries through conversation
- **Text-to-Prompt Integration**: Generate and execute complex analysis workflows from simple text descriptions
- **Visualization Suggestions**: Automatically recommend appropriate charts based on query results
- **Cross-Data Integration**: Query across multiple data sources with a single natural language request 