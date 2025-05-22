# Job Tracker Application

## Overview

The Job Tracker Application is a .NET Core based system designed for construction and renovation project management. It enables tracking job progress across multiple sections, managing subcontractors, employee time tracking, and integrates with MS Office, Google services, and a third-party system called CardShark. The application includes an AI assistant that provides guidance on Massachusetts building codes.

## User Preferences

Preferred communication style: Simple, everyday language.

## System Architecture

The application follows a three-tier architecture:

1. **Presentation Layer**: React-based web application using Material-UI
2. **Application Layer**: ASP.NET Core Web API with C#
3. **Data Layer**: MySQL database for persistent storage

The system uses Entity Framework Core as the ORM to interact with the database. Authentication is handled through Active Directory integration with JWT token issuance for subsequent API calls.

## Key Components

### Backend Structure

- **Controllers**: REST API endpoints for the various entities (Jobs, Sections, Employees, TimeTracking, etc.)
- **Services**: Business logic separated into service classes:
  - Integration services for Microsoft Office, Google services, and CardShark
  - BuildingCodeService for managing building code references
  - AIAssistantService for providing AI-powered assistance
  - ActiveDirectoryService for authentication
- **Models**: Entity classes that represent the domain objects
- **Data**: Database context and migration management

### Frontend Structure

- React-based SPA with component organization mirroring the backend entities
- Services layer handling API communication
- Material-UI for component styling
- State management using React hooks or Redux (both patterns present in the codebase)

### Database Schema

The database design includes these main entities:

- **Jobs**: Core entity representing construction/renovation projects
- **JobSections**: Various phases of each job (11 main sections as per requirements)
- **Employees**: Workers assigned to jobs
- **TimeEntries**: Clock in/out records for employees
- **Subcontractors**: External companies working on job sections
- **BuildingCodes**: Massachusetts building code references

## Data Flow

1. Users authenticate via Active Directory
2. After authentication, users receive a JWT token for API access
3. The frontend makes API calls to manage jobs, sections, employees, and other entities
4. Real-time data is synchronized with external systems:
   - Microsoft Office for document generation
   - Google services for docs and calendar
   - CardShark for commercial integration
5. AI assistance is provided by integrating with OpenAI API
6. Building code references are stored in the database and served to users as needed

## External Dependencies

### Authentication
- Active Directory for user authentication
- JWT Bearer tokens for API authorization

### External Services
- Microsoft Graph API for Office integration
- Google API for Docs and Calendar
- CardShark API for commercial integration
- OpenAI API for AI assistant functionality

### Frontend Dependencies
- React for UI components
- Material-UI for styling
- Axios for API communication

### Backend Dependencies
- Entity Framework Core for database access
- ASP.NET Core for API and server hosting
- Various Microsoft and third-party libraries for integration

## Deployment Strategy

The application is configured to run on a .NET Core runtime environment:

- ASP.NET Core web server hosting the API endpoints
- MySQL database for data storage
- The application is configured to run on port 5000
- In Replit, the app is started with `dotnet run --urls=http://0.0.0.0:5000`
- The build and run workflow is managed through the .replit configuration

## Development Workflow

The development process for this application follows these steps:

1. Set up the database with the required schema
2. Implement core entities and relationships
3. Add business logic in services
4. Create API controllers for frontend interaction
5. Develop React frontend components
6. Integrate with external services
7. Add automated testing
8. Deploy and monitor

The project appears to be using a feature-driven approach, where each major feature (jobs, sections, etc.) has its own set of controllers, services, and frontend components.