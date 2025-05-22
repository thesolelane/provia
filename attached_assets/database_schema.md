# Job Tracker Application Database Schema

## Overview

This document outlines the comprehensive database schema for the Job Tracker application. The schema is designed to support all identified requirements, including job management, section tracking, subcontractor management, building code references, and AI assistant integration.

## Core Tables

### Users

```sql
CREATE TABLE Users (
    UserID INT AUTO_INCREMENT PRIMARY KEY,
    ADUserName VARCHAR(100) NOT NULL,
    FirstName VARCHAR(50) NOT NULL,
    LastName VARCHAR(50) NOT NULL,
    Email VARCHAR(100) NOT NULL,
    Phone VARCHAR(20),
    Role VARCHAR(50) NOT NULL,
    Department VARCHAR(50),
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    ModifiedDate DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);
```

### Jobs

```sql
CREATE TABLE Jobs (
    JobID INT AUTO_INCREMENT PRIMARY KEY,
    JobNumber VARCHAR(20) NOT NULL UNIQUE,
    JobName VARCHAR(100) NOT NULL,
    Address VARCHAR(200) NOT NULL,
    City VARCHAR(50) NOT NULL,
    State VARCHAR(2) NOT NULL,
    ZipCode VARCHAR(10) NOT NULL,
    CustomerID INT,
    ProjectManagerID INT,
    StartDate DATE,
    TargetCompletionDate DATE,
    ActualCompletionDate DATE,
    Status VARCHAR(20) NOT NULL,
    Priority VARCHAR(20),
    Description TEXT,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    ModifiedDate DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (CustomerID) REFERENCES Customers(CustomerID),
    FOREIGN KEY (ProjectManagerID) REFERENCES Users(UserID)
);
```

### Customers

```sql
CREATE TABLE Customers (
    CustomerID INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    ContactPerson VARCHAR(100),
    Phone VARCHAR(20),
    Email VARCHAR(100),
    Address VARCHAR(200),
    City VARCHAR(50),
    State VARCHAR(2),
    ZipCode VARCHAR(10),
    CustomerType VARCHAR(50),
    Notes TEXT,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    ModifiedDate DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);
```

### JobSections

```sql
CREATE TABLE JobSections (
    SectionID INT AUTO_INCREMENT PRIMARY KEY,
    JobID INT NOT NULL,
    SectionType VARCHAR(50) NOT NULL,
    SectionName VARCHAR(100) NOT NULL,
    Description TEXT,
    Status VARCHAR(20) NOT NULL,
    IsSubcontracted BOOLEAN DEFAULT FALSE,
    SubcontractorID INT,
    ResponsiblePartyID INT,
    StartDate DATE,
    TargetCompletionDate DATE,
    ActualCompletionDate DATE,
    InspectionDate DATE,
    InspectionResult VARCHAR(20),
    ReinspectionDate DATE,
    Notes TEXT,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    ModifiedDate DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (JobID) REFERENCES Jobs(JobID),
    FOREIGN KEY (SubcontractorID) REFERENCES Subcontractors(SubcontractorID),
    FOREIGN KEY (ResponsiblePartyID) REFERENCES Users(UserID)
);
```

### Subcontractors

```sql
CREATE TABLE Subcontractors (
    SubcontractorID INT AUTO_INCREMENT PRIMARY KEY,
    CompanyName VARCHAR(100) NOT NULL,
    ContactName VARCHAR(100) NOT NULL,
    Phone VARCHAR(20),
    Email VARCHAR(100),
    Address VARCHAR(200),
    City VARCHAR(50),
    State VARCHAR(2),
    ZipCode VARCHAR(10),
    LicenseNumber VARCHAR(50),
    InsuranceInfo TEXT,
    Notes TEXT,
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    ModifiedDate DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);
```

### SectionSubsections

```sql
CREATE TABLE SectionSubsections (
    SubsectionID INT AUTO_INCREMENT PRIMARY KEY,
    SectionID INT NOT NULL,
    SubsectionType VARCHAR(50) NOT NULL,
    SubsectionName VARCHAR(100) NOT NULL,
    Description TEXT,
    Status VARCHAR(20) NOT NULL,
    ResponsiblePartyID INT,
    StartDate DATE,
    CompletionDate DATE,
    Notes TEXT,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    ModifiedDate DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (SectionID) REFERENCES JobSections(SectionID),
    FOREIGN KEY (ResponsiblePartyID) REFERENCES Users(UserID)
);
```

## Time Tracking and Assignment

### TimeEntries

```sql
CREATE TABLE TimeEntries (
    EntryID INT AUTO_INCREMENT PRIMARY KEY,
    UserID INT NOT NULL,
    JobID INT NOT NULL,
    SectionID INT,
    ClockInTime DATETIME NOT NULL,
    ClockOutTime DATETIME,
    TotalHours DECIMAL(6,2),
    Notes TEXT,
    CardSharkSyncStatus VARCHAR(20),
    CardSharkSyncTime DATETIME,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    ModifiedDate DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (UserID) REFERENCES Users(UserID),
    FOREIGN KEY (JobID) REFERENCES Jobs(JobID),
    FOREIGN KEY (SectionID) REFERENCES JobSections(SectionID)
);
```

### JobAssignments

```sql
CREATE TABLE JobAssignments (
    AssignmentID INT AUTO_INCREMENT PRIMARY KEY,
    JobID INT NOT NULL,
    SectionID INT,
    UserID INT NOT NULL,
    AssignmentDate DATE NOT NULL,
    Status VARCHAR(20) NOT NULL,
    Notes TEXT,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    ModifiedDate DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (JobID) REFERENCES Jobs(JobID),
    FOREIGN KEY (SectionID) REFERENCES JobSections(SectionID),
    FOREIGN KEY (UserID) REFERENCES Users(UserID)
);
```

## Materials and Requirements

### Materials

```sql
CREATE TABLE Materials (
    MaterialID INT AUTO_INCREMENT PRIMARY KEY,
    SectionID INT NOT NULL,
    SubsectionID INT,
    MaterialName VARCHAR(100) NOT NULL,
    MaterialType VARCHAR(50) NOT NULL,
    Quantity DECIMAL(10,2) NOT NULL,
    Unit VARCHAR(20) NOT NULL,
    EstimatedCost DECIMAL(10,2),
    ActualCost DECIMAL(10,2),
    Status VARCHAR(20) NOT NULL,
    Notes TEXT,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    ModifiedDate DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (SectionID) REFERENCES JobSections(SectionID),
    FOREIGN KEY (SubsectionID) REFERENCES SectionSubsections(SubsectionID)
);
```

### Permits

```sql
CREATE TABLE Permits (
    PermitID INT AUTO_INCREMENT PRIMARY KEY,
    JobID INT NOT NULL,
    SectionID INT,
    PermitType VARCHAR(50) NOT NULL,
    PermitNumber VARCHAR(50) NOT NULL,
    ApplicationDate DATE,
    ApprovalDate DATE,
    ExpirationDate DATE,
    Status VARCHAR(20) NOT NULL,
    ResponsiblePartyID INT,
    InspectorName VARCHAR(100),
    InspectorOffice VARCHAR(100),
    InspectorPhone VARCHAR(20),
    Notes TEXT,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    ModifiedDate DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (JobID) REFERENCES Jobs(JobID),
    FOREIGN KEY (SectionID) REFERENCES JobSections(SectionID),
    FOREIGN KEY (ResponsiblePartyID) REFERENCES Users(UserID)
);
```

## Building Code and AI Integration

### BuildingCodes

```sql
CREATE TABLE BuildingCodes (
    CodeID INT AUTO_INCREMENT PRIMARY KEY,
    CodeReference VARCHAR(50) NOT NULL,
    Title VARCHAR(200) NOT NULL,
    Description TEXT NOT NULL,
    Category VARCHAR(50) NOT NULL,
    Subcategory VARCHAR(50),
    ApplicableSections VARCHAR(200),
    EffectiveDate DATE,
    ExpirationDate DATE,
    IsActive BOOLEAN DEFAULT TRUE,
    FullText TEXT,
    PlainLanguageSummary TEXT,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    ModifiedDate DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);
```

### SectionCodeReferences

```sql
CREATE TABLE SectionCodeReferences (
    ReferenceID INT AUTO_INCREMENT PRIMARY KEY,
    SectionID INT NOT NULL,
    SubsectionID INT,
    CodeID INT NOT NULL,
    Relevance TEXT,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    ModifiedDate DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (SectionID) REFERENCES JobSections(SectionID),
    FOREIGN KEY (SubsectionID) REFERENCES SectionSubsections(SubsectionID),
    FOREIGN KEY (CodeID) REFERENCES BuildingCodes(CodeID)
);
```

### AIAssistantLogs

```sql
CREATE TABLE AIAssistantLogs (
    LogID INT AUTO_INCREMENT PRIMARY KEY,
    UserID INT NOT NULL,
    JobID INT,
    SectionID INT,
    QueryText TEXT NOT NULL,
    ResponseText TEXT NOT NULL,
    QueryTimestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
    RelatedCodes TEXT,
    Feedback VARCHAR(20),
    FeedbackNotes TEXT,
    FOREIGN KEY (UserID) REFERENCES Users(UserID),
    FOREIGN KEY (JobID) REFERENCES Jobs(JobID),
    FOREIGN KEY (SectionID) REFERENCES JobSections(SectionID)
);
```

## Integration and Notifications

### ExternalDocuments

```sql
CREATE TABLE ExternalDocuments (
    DocumentID INT AUTO_INCREMENT PRIMARY KEY,
    JobID INT NOT NULL,
    SectionID INT,
    DocumentType VARCHAR(50) NOT NULL,
    DocumentName VARCHAR(100) NOT NULL,
    ExternalSystem VARCHAR(50) NOT NULL,
    ExternalReference VARCHAR(200) NOT NULL,
    LastSyncDate DATETIME,
    SyncStatus VARCHAR(20),
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    ModifiedDate DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (JobID) REFERENCES Jobs(JobID),
    FOREIGN KEY (SectionID) REFERENCES JobSections(SectionID)
);
```

### CalendarEvents

```sql
CREATE TABLE CalendarEvents (
    EventID INT AUTO_INCREMENT PRIMARY KEY,
    JobID INT NOT NULL,
    SectionID INT,
    EventType VARCHAR(50) NOT NULL,
    EventTitle VARCHAR(100) NOT NULL,
    EventDescription TEXT,
    StartDateTime DATETIME NOT NULL,
    EndDateTime DATETIME NOT NULL,
    Location VARCHAR(200),
    ResponsiblePartyID INT,
    ExternalCalendarID VARCHAR(200),
    SyncStatus VARCHAR(20),
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    ModifiedDate DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (JobID) REFERENCES Jobs(JobID),
    FOREIGN KEY (SectionID) REFERENCES JobSections(SectionID),
    FOREIGN KEY (ResponsiblePartyID) REFERENCES Users(UserID)
);
```

### Notifications

```sql
CREATE TABLE Notifications (
    NotificationID INT AUTO_INCREMENT PRIMARY KEY,
    UserID INT NOT NULL,
    JobID INT,
    SectionID INT,
    NotificationType VARCHAR(50) NOT NULL,
    Title VARCHAR(100) NOT NULL,
    Message TEXT NOT NULL,
    IsRead BOOLEAN DEFAULT FALSE,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    ExpirationDate DATETIME,
    FOREIGN KEY (UserID) REFERENCES Users(UserID),
    FOREIGN KEY (JobID) REFERENCES Jobs(JobID),
    FOREIGN KEY (SectionID) REFERENCES JobSections(SectionID)
);
```

## Section-Specific Tables

### FramingSections

```sql
CREATE TABLE FramingSections (
    FramingID INT AUTO_INCREMENT PRIMARY KEY,
    SectionID INT NOT NULL,
    InteriorFramingStartDate DATE,
    InteriorFramingCompletionDate DATE,
    ExteriorFramingStartDate DATE,
    ExteriorFramingCompletionDate DATE,
    LoadBearingModifications BOOLEAN DEFAULT FALSE,
    FireBlockingInstalled BOOLEAN DEFAULT FALSE,
    WallFramingMaterial VARCHAR(50),
    RoofFramingMaterial VARCHAR(50),
    SheathinType VARCHAR(50),
    WeatherBarrierType VARCHAR(50),
    InspectionDate DATE,
    InspectionStatus VARCHAR(20),
    ReinspectionDate DATE,
    Notes TEXT,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    ModifiedDate DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (SectionID) REFERENCES JobSections(SectionID)
);
```

### PlumbingSections

```sql
CREATE TABLE PlumbingSections (
    PlumbingID INT AUTO_INCREMENT PRIMARY KEY,
    SectionID INT NOT NULL,
    WaterSupplyMaterial VARCHAR(50),
    WaterSupplySize VARCHAR(20),
    DrainMaterial VARCHAR(50),
    GasLineMaterial VARCHAR(50),
    PressureTestDate DATE,
    PressureTestResult VARCHAR(20),
    AirTestDate DATE,
    AirTestResult VARCHAR(20),
    InspectionDate DATE,
    InspectionStatus VARCHAR(20),
    ReinspectionDate DATE,
    Notes TEXT,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    ModifiedDate DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (SectionID) REFERENCES JobSections(SectionID)
);
```

### ElectricalSections

```sql
CREATE TABLE ElectricalSections (
    ElectricalID INT AUTO_INCREMENT PRIMARY KEY,
    SectionID INT NOT NULL,
    ServiceSize INT,
    PanelLocation VARCHAR(100),
    SubpanelRequired BOOLEAN DEFAULT FALSE,
    CircuitSchedule TEXT,
    LowVoltageSystems TEXT,
    InspectionDate DATE,
    InspectionStatus VARCHAR(20),
    ReinspectionDate DATE,
    Notes TEXT,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    ModifiedDate DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (SectionID) REFERENCES JobSections(SectionID)
);
```

## Indexes

```sql
-- Performance indexes
CREATE INDEX idx_jobs_status ON Jobs(Status);
CREATE INDEX idx_jobs_projectmanager ON Jobs(ProjectManagerID);
CREATE INDEX idx_jobsections_job ON JobSections(JobID);
CREATE INDEX idx_jobsections_status ON JobSections(Status);
CREATE INDEX idx_jobsections_type ON JobSections(SectionType);
CREATE INDEX idx_jobsections_subcontracted ON JobSections(IsSubcontracted);
CREATE INDEX idx_timeentries_user ON TimeEntries(UserID);
CREATE INDEX idx_timeentries_job ON TimeEntries(JobID);
CREATE INDEX idx_timeentries_date ON TimeEntries(ClockInTime);
CREATE INDEX idx_jobassignments_user ON JobAssignments(UserID);
CREATE INDEX idx_jobassignments_date ON JobAssignments(AssignmentDate);
CREATE INDEX idx_materials_section ON Materials(SectionID);
CREATE INDEX idx_permits_job ON Permits(JobID);
CREATE INDEX idx_permits_status ON Permits(Status);
CREATE INDEX idx_buildingcodes_category ON BuildingCodes(Category);
CREATE INDEX idx_buildingcodes_sections ON BuildingCodes(ApplicableSections);
CREATE INDEX idx_notifications_user ON Notifications(UserID);
CREATE INDEX idx_notifications_read ON Notifications(IsRead);
```

## Database Relationships Diagram

```
Users 1──────┐
             │
             │ N
             ▼
Customers 1──┐   ┌───────1 Users (ProjectManager)
             │   │
             │   │
             │   │
             ▼   ▼
             Jobs N─────────┐
               │            │
               │            │
    ┌──────────┘            │
    │                       │
    │                       │
    ▼                       ▼
JobSections N───────────N TimeEntries
    │    │                  ▲
    │    │                  │
    │    └──────────────────┘
    │
    │     ┌───────────1 Subcontractors
    │     │
    │     ▼
    │     SectionSubsections
    │            │
    │            │
    │            ▼
    └────────N Materials
    │
    │
    ▼
SectionCodeReferences N────────1 BuildingCodes
    ▲
    │
    │
    │
AIAssistantLogs
```

## Notes on Schema Design

1. **Flexibility**: The schema is designed to be flexible, allowing for future expansion and additional fields as needed.

2. **Normalization**: The database is normalized to reduce redundancy while maintaining practical performance considerations.

3. **Subcontractor Tracking**: The `IsSubcontracted` flag in JobSections allows for dynamic views based on whether work is done in-house or by subcontractors.

4. **Section-Specific Tables**: For sections with unique field requirements, separate tables are created and linked to the main JobSections table.

5. **Building Code Integration**: The BuildingCodes and SectionCodeReferences tables allow for contextual display of relevant code information.

6. **AI Assistant Support**: The AIAssistantLogs table tracks interactions with the AI assistant for auditing and improvement purposes.

7. **External Integrations**: Tables for ExternalDocuments and CalendarEvents support integration with Microsoft Office and Google services.

## Implementation Considerations

1. **Migrations**: Use Entity Framework Core migrations to manage schema changes over time.

2. **Seed Data**: Prepare seed data for lookup tables and testing.

3. **Soft Deletes**: Consider implementing soft delete patterns for important entities.

4. **Auditing**: Add auditing capabilities for tracking changes to critical data.

5. **Performance**: Monitor query performance and add additional indexes as needed based on actual usage patterns.

6. **Security**: Implement row-level security where appropriate for multi-user access control.

7. **Backup Strategy**: Establish regular backup procedures with point-in-time recovery capabilities.
