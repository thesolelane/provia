# PROVIA - Construction Management Platform

## Overview
PROVIA is an enterprise-grade, multi-tenant SaaS platform for construction management (and any workflow-driven industry), developed by Cooperanth Consulting LLC. It supports field workers, supervisors, subcontractors, and company admins across multiple client companies. The platform streamlines construction project workflows from job creation and scope definition to permit tracking, inspections, and subcontractor management. Key features include a Massachusetts code engine for compliance, a comprehensive role-based access control system, multi-company subcontractor management, and an offline-first mobile synchronization architecture.

## Deployment Strategy
- **Development/Build**: Replit (this environment) - code, build, push to GitHub only
- **Production**: Docker server (self-hosted) - runs the actual SaaS platform
- **Architecture**: Single shared PostgreSQL + single .NET app serves all tenants via CompanyId isolation

## Clean State (March 2026)
- All test data cleared - database is clean
- Single admin account: `admin@provia.app` / `Provia2024!` (Company: PROVIA Platform, CompanyId: 5)
- Removed: all debug/test HTML pages, duplicate SMS services, junk controllers
- Removed: WeatherForecastController, TestController, SmsTestController, QuickAuthController, SeedController, InitializationController

## User Preferences
- Framework: .NET 6.0 (locked to Replit environment availability)
- Brand: PROVIA (3-arrow logo, orange/teal theme, white background)
- Security: Enterprise-grade (no test credentials, all secrets in env vars)
- Database: Shared tenant model with CompanyId filtering + sub-contractor multi-company
- Deployment: Docker on own server; Replit for development only
- Mobile: Offline-first architecture with batch sync
- Vision: Universal workflow automation platform (not just construction)

## System Architecture

**Technology Stack:**
- Backend: .NET 6.0 (C#, Entity Framework Core)
- Database: PostgreSQL
- Frontend: React, Node.js, HTML/CSS/JS
- Authentication: JWT (1-hour expiration), BCrypt
- Messaging: SendGrid (email), Twilio/Textedly (SMS)
- Mobile: Offline-first with local sync queue

**UI/UX Decisions:**
- **Branding:** Custom 3-arrow SVG logo (orange/blue/navy), white background with orange (#FF9500)/teal (#2F5A7E) gradient UI.
- **Role-Based Dashboards:** Five distinct interfaces with unique themes (Admin purple, Foreman navy, Sub-Contractor orange) accessible via a single login, auto-routing users based on their role.

**Technical Implementations:**
- **Multi-Tenancy:** Implemented with a shared PostgreSQL database and CompanyId-based isolation via a TenantContext service.
- **Massachusetts Code Engine:** Utilizes `CodeBook` and `CodeRule` tables to auto-generate permits and inspections based on selected job scopes, ensuring compliance with MA jurisdiction codes (e.g., 780 CMR Building, 248 CMR Plumbing/Gas).
- **Multi-Company Subcontractors:** A `SubContractorCompany` junction table enables subcontractors to work for multiple general contractors.
- **Offline-First Mobile:** Achieved through a `SyncQueueItem` for pending operations and a `MobileSyncService` for batch processing, supporting field workers in areas with limited connectivity.
- **Job Bidding System:** Facilitates a workflow where subcontractors bid on jobs, foremen accept bids with advance payments, and supervisors approve completion.
- **Multi-Stage Inspections:** Supports `ROUGH`, `SECOND`, `FINISH`, and `FINAL_SIGNOFF` stages with integrated permit document tracking.
- **Role Structure:** A consolidated role system (`Master Admin`, `Supervisor`, `Foreman`, `Field Operator`, `Subcontractor`) with defined permissions, limits, and permit authorities.
- **Construction Cost Code System:** A 6-digit code system (DDD L TT) for categorizing construction costs by department, cost type, and subcategory, supporting integration with external financial systems like Wave.
- **Job Management:** Admin dashboard functionality to create, track, and manage construction jobs with auto-generated job numbers and a defined workflow (Planning → Permits Pending → In Progress → Inspection → Completed).
- **Team Management:** Features for editing user details, deactivating accounts, and initiating password resets, managed through secure, admin-only operations.
- **Temporary Password System:** Automates secure password generation for new users, delivered via email with a `PasswordNeedsChange` flag to enforce a password update on first login.
- **MassGIS Property Data Integration:** Fetches Massachusetts parcel data (owner, zoning, assessed values, lot size) from the official ArcGIS FeatureServer to auto-populate permit applications. Job locations are parsed as "street address, city" format.
- **Permit Document Automation:** Uses `PermitFormTemplate`, `PropertyProfile`, and `FormFieldMapping` tables to auto-fill official MA building permit PDFs with GIS-sourced property data and company/job information.
- **Model Naming:** `PermitDocument` handles generated PDF permits; `UploadedPermitDoc` handles inspection-related document uploads.
- **Municipal Integration Infrastructure:** Foundation tables for future portal integrations:
  - `MunicipalPortal`: Registry of 19 MA towns with their permit platforms (OpenGov, PermitEyes, Accela, Custom)
  - `PermitAuditLog`: Compliance logging for all permit-related actions
  - `CompanyPortalCredential`: Secure storage for municipal portal API credentials per company
- **Permit Form Templates:** Uploaded templates stored as copies with unique filenames; originals never modified. Supports PDF, DOCX, XLSX formats.
- **Construction Control Documents:** Official MA 10th Edition forms (Initial/Final Construction Control, Checklist, Contractor Letter, Structural Review Guidance) required by 780 CMR Section 107.6 for buildings ≥ 35,000 cubic feet.
- **Document Requirements System:** `DocumentRequirements` table links templates to job scopes with phase tracking (EXISTING/PROPOSED) and format types (PHOTO/DRAWING/FORM/CERTIFICATE). Exterior existing conditions typically use photos; proposed work uses drawings. Documents are selected based on job scope and permit type.

## External Dependencies
- **Database:** PostgreSQL (Neon-backed via Replit)
- **Email Service:** SendGrid
- **SMS Service:** Twilio/Textedly
- **MassGIS API:** Massachusetts Geographic Information System via ArcGIS REST services (free public access)

## Future Integration Roadmap
- **Accela Civic Platform:** REST API for Framingham, Cape Cod towns (best API availability)
- **OpenGov/ViewPoint Cloud:** Enterprise API for Lexington, Hanover, Gardner, etc. (requires partnership)
- **PermitEyes:** Web automation for Mashpee, Falmouth, Chelmsford, etc. (no public API)
- **Additional Data Sources:** FEMA NFHL (flood zones), MA DPL (contractor licenses), DIA (workers comp)

## Documentation Files
- `JobTracker/docs/AWS_INTEGRATION_ROADMAP.md` - AWS integration strategy, migration plan, security requirements, cost estimates