# Subcontractor Tracking Feature Update

## Overview

This document outlines the requirements and implementation details for the subcontractor tracking feature in the Job Tracker application. This feature will allow users to indicate whether work for each section is being performed in-house or by subcontractors, with different information display options based on this selection.

## Requirements

### Core Functionality

1. **Work Type Selection**
   - Each job section must include a checkbox/toggle to indicate:
     - In-house work
     - Subcontracted work

2. **Subcontractor Information**
   - When "Subcontractor" is selected, the following fields must be available:
     - Subcontractor company name
     - Point of contact name
     - Contact information (phone, email)
     - Contract reference number (optional)

3. **Dynamic Section View**
   - For in-house work: Display all detailed fields and subsections
   - For subcontracted work: Display a collapsed/simplified view with only:
     - Progress status (In Progress, Pass, Fail)
     - Inspection date
     - Reinspection date (if applicable)
     - Notes field
     - Subcontractor information

4. **Status Tracking**
   - Common status fields for both in-house and subcontracted work:
     - Progress status
     - Inspection results
     - Inspection dates
     - Notes

## Database Schema Updates

### Subcontractor Table
```
Subcontractors
- SubcontractorID (PK)
- CompanyName
- ContactName
- Phone
- Email
- Address
- LicenseNumber
- InsuranceInfo
- CreatedDate
- ModifiedDate
```

### Job Section Updates
```
JobSections
- SectionID (PK)
- JobID (FK)
- SectionType
- IsSubcontracted (boolean)
- SubcontractorID (FK, nullable)
- Status
- InspectionDate
- ReinspectionDate
- InspectionResult
- Notes
- ResponsiblePartyID
- CreatedDate
- ModifiedDate
```

### Section-Subcontractor Relationship
```
SectionSubcontractors
- SectionSubcontractorID (PK)
- SectionID (FK)
- SubcontractorID (FK)
- ContractReference
- StartDate
- ExpectedCompletionDate
- ActualCompletionDate
- Notes
- CreatedDate
- ModifiedDate
```

## UI Components

### Section Header Component
- Section title
- Work type toggle (In-house/Subcontractor)
- Expand/Collapse button
- Status indicator

### Subcontractor Selection Component
- Dropdown for existing subcontractors
- "Add New Subcontractor" option
- Quick view of subcontractor details
- Edit subcontractor information button

### Collapsed View Component
- Progress status dropdown
- Inspection date picker
- Reinspection date picker
- Notes field
- Subcontractor information display

### Expanded View Component
- All detailed fields for the section
- All subsections and their fields
- Material tracking
- Responsible party assignment
- Building code references
- AI assistant integration

## User Experience Flow

1. **Creating a New Job Section**
   - User creates a new section
   - Default view is expanded with "In-house" selected
   - User can toggle to "Subcontractor" if needed

2. **Converting to Subcontracted Work**
   - User toggles from "In-house" to "Subcontractor"
   - System prompts to select a subcontractor
   - View automatically collapses to simplified view
   - Detailed information is preserved but hidden

3. **Converting to In-house Work**
   - User toggles from "Subcontractor" to "In-house"
   - View automatically expands to show all fields
   - Previously entered detailed information becomes visible

4. **Tracking Subcontractor Progress**
   - Project manager updates status based on subcontractor reports
   - System tracks inspection dates and results
   - Notes field allows for communication records

## Implementation Considerations

1. **Data Preservation**
   - When switching between in-house and subcontracted work, all data should be preserved
   - No data loss should occur when changing work types

2. **Permission Controls**
   - Role-based permissions for changing work type
   - Restrictions on who can assign subcontractors

3. **Notification Integration**
   - Notifications for subcontractor assignments
   - Alerts for upcoming subcontractor inspections
   - Reminders for follow-up on failed inspections

4. **Reporting**
   - Subcontractor performance reports
   - In-house vs. subcontracted work analysis
   - Cost comparison reporting

## Sprint Backlog Updates

The following user stories should be added to the sprint backlog:

1. **User Story**: As a project manager, I need to indicate whether work is being done in-house or by subcontractors.
   - Task: Implement work type toggle in database schema
   - Task: Create work type selection UI component
   - Task: Develop logic for dynamic view switching

2. **User Story**: As a project manager, I need to manage subcontractor information.
   - Task: Create subcontractor database schema
   - Task: Implement subcontractor CRUD operations
   - Task: Develop subcontractor management UI

3. **User Story**: As a project manager, I need to view simplified information for subcontracted work.
   - Task: Implement collapsed view component
   - Task: Create logic for selective field display
   - Task: Develop status tracking for subcontracted work

4. **User Story**: As a project manager, I need to track inspection results for both in-house and subcontracted work.
   - Task: Implement common inspection tracking fields
   - Task: Create inspection result UI components
   - Task: Develop inspection notification system

These user stories should be integrated into Sprint 2 (Job Section Framework) to ensure the subcontractor tracking functionality is available early in the development process.
