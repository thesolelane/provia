# Job Tracker App Architecture Design

## Overview
This document outlines the architecture for a job progress tracking application designed to run on a Windows server with MySQL database. The application will serve employees tracking construction/renovation jobs through multiple stages, with integrations to various Microsoft and Google services, as well as a commercial product called CardShark.

## System Architecture

### High-Level Architecture
The application will follow a three-tier architecture:

1. **Presentation Layer**: Web-based user interface accessible to employees
2. **Application Layer**: Business logic, integration services, and API endpoints
3. **Data Layer**: MySQL database for persistent storage

### Component Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Job Tracker Application                       │
└───────────────────────────────────┬─────────────────────────────────┘
                                    │
┌───────────────────────────────────┼─────────────────────────────────┐
│  ┌─────────────────┐  ┌───────────┴───────────┐  ┌─────────────────┐│
│  │  User Interface │  │   Business Logic      │  │  Data Access    ││
│  │                 │  │                       │  │                 ││
│  │ - Dashboard     │  │ - Job Management      │  │ - MySQL ORM     ││
│  │ - Job Tracking  │  │ - User Management     │  │ - Data Models   ││
│  │ - Time Tracking │  │ - Workflow Engine     │  │ - Query Service ││
│  │ - Reports       │  │ - Notification System │  │                 ││
│  └─────────────────┘  └───────────────────────┘  └─────────────────┘│
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
                                    │
┌───────────────────────────────────┼─────────────────────────────────┐
│                     Integration Services Layer                       │
│                                                                     │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────┐  │
│  │ Microsoft Office│  │ Google Services │  │ CardShark           │  │
│  │ Integration     │  │ Integration     │  │ Integration         │  │
│  │                 │  │                 │  │                     │  │
│  │ - Excel API     │  │ - Google Docs   │  │ - API Client        │  │
│  │ - Word API      │  │ - Calendar API  │  │ - Data Sync         │  │
│  └─────────────────┘  └─────────────────┘  └─────────────────────┘  │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
                                    │
┌───────────────────────────────────┼─────────────────────────────────┐
│                      Authentication & Security                       │
│                                                                     │
│  ┌─────────────────────────┐  ┌───────────────────────────────────┐ │
│  │ Active Directory        │  │ Security Services                 │ │
│  │ Integration             │  │                                   │ │
│  │                         │  │ - Encryption                      │ │
│  │ - User Authentication   │  │ - Audit Logging                   │ │
│  │ - Role Management       │  │ - Access Control                  │ │
│  └─────────────────────────┘  └───────────────────────────────────┘ │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

## Database Schema Design

The database will be structured to support the job tracking workflow with the following main entities:

1. **Users**: Employee information and authentication details
2. **Jobs**: Core job information including number, name, address, customer details
3. **JobSections**: The 11 different job sections (Permit, Demolition, etc.)
4. **SectionStatus**: Status tracking for each section (completed, incomplete, in progress, denied)
5. **TimeEntries**: Clock in/out records for employees
6. **Materials**: Required materials for each job section
7. **Assignments**: Job assignments to employees
8. **Documents**: Links to related documents in external systems

### Entity Relationship Diagram (Simplified)

```
┌─────────────┐       ┌─────────────┐       ┌─────────────────┐
│   Users     │       │    Jobs     │       │  JobSections    │
│             │       │             │       │                 │
│ - UserID    │       │ - JobID     │       │ - SectionID     │
│ - Name      │◄─────►│ - JobNumber │◄─────►│ - JobID         │
│ - Email     │       │ - JobName   │       │ - SectionType   │
│ - Role      │       │ - Address   │       │ - Description   │
└─────────────┘       │ - City      │       │ - Status        │
                      │ - State     │       │ - ResponsibleID │
                      │ - CustomerID│       └─────────────────┘
                      └─────────────┘               │
                            ▲                       │
                            │                       │
                            │                       ▼
┌─────────────┐       ┌─────────────┐       ┌─────────────────┐
│ TimeEntries │       │ Customers   │       │ Materials       │
│             │       │             │       │                 │
│ - EntryID   │       │ - CustomerID│       │ - MaterialID    │
│ - UserID    │       │ - Name      │       │ - SectionID     │
│ - JobID     │       │ - Contact   │       │ - Name          │
│ - ClockIn   │       │ - Phone     │       │ - Quantity      │
│ - ClockOut  │       │ - Email     │       │ - Description   │
└─────────────┘       └─────────────┘       └─────────────────┘
```

## Integration Architecture

### Microsoft Office Integration
- **Excel Integration**: Using Microsoft Graph API or Office JS API for reading/writing Excel files
- **Word Integration**: Using Microsoft Graph API for document generation and template filling

### Google Services Integration
- **Google Docs**: Using Google Docs API for document creation and editing
- **Google Calendar**: Using Google Calendar API for scheduling and reminders

### CardShark Integration
- API-based integration for employee clock in/out functionality
- Data synchronization for job locations and assignments

## Security Architecture

### Authentication
- Active Directory integration for employee authentication
- Role-based access control (RBAC) for feature access
- JWT token-based API security

### Data Security
- Encryption for sensitive data at rest
- TLS/SSL for data in transit
- Input validation and sanitization
- Parameterized queries to prevent SQL injection

## User Interface Architecture

### Main Components
1. **Dashboard**: Overview of assigned jobs and their statuses
2. **Job Detail View**: Comprehensive view of all job sections and their statuses
3. **Time Tracking**: Clock in/out interface with job assignment
4. **Section Management**: Interface for updating section statuses and materials
5. **Reports**: Generation of progress reports and exports

### Responsive Design
- The UI will be designed to work on both desktop and mobile devices
- Progressive enhancement for better experience on larger screens

## API Architecture

### Internal APIs
- RESTful API endpoints for all core functionality
- GraphQL API for complex data queries and reporting

### External APIs
- Secure endpoints for integration with CardShark
- Webhook receivers for external system notifications

## Workflow Engine

A dedicated workflow engine will manage the progression of jobs through the 11 defined sections:

1. Permit
2. Demolition
3. Rough Plumbing
4. Rough Electrical
5. Insulation
6. Sheetrock
7. Paint Prep
8. Finish Install
9. Finish Paint, Flooring
10. Kitchen & Bath Fixtures
11. Misc (Mirrors, closet fixtures, etc.)

Each section will have its own status tracking, responsible parties, and material requirements.

## Notification System

- Email notifications for status changes
- In-app notifications for assignments and updates
- Calendar event creation for scheduled inspections and deadlines

## Scalability Considerations

- Horizontal scaling for web tier
- Database optimization for large job histories
- Caching strategy for frequently accessed data
- Asynchronous processing for integration tasks

## Deployment Architecture

- Windows Server with IIS
- MySQL database server
- Scheduled backup system
- Monitoring and logging infrastructure
