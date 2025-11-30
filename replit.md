# PROVIA - Construction Management Platform

## Overview
PROVIA is an enterprise-grade, multi-tenant construction management SaaS platform developed by Cooperanth Consulting LLC. It serves construction companies like Preferred Builders USA, LLC and their partners. The platform supports a diverse user base including field workers, supervisors, foremen, administrators, and sub-contractors across multiple client companies. The core purpose is to streamline construction project workflows, from job creation and scope definition to permit tracking, inspections, and sub-contractor management. Key capabilities include Massachusetts code engine integration for automated compliance, a comprehensive role-based access control system, multi-company sub-contractor management, and an offline-first mobile synchronization architecture for field operations.

## User Preferences
- Framework: .NET 6.0 (locked to Replit environment availability)
- Brand: PROVIA (3-arrow logo, orange/teal theme, white background)
- Security: Enterprise-grade (no test credentials, all secrets in env vars)
- Database: Shared tenant model with CompanyId filtering + sub-contractor multi-company
- Deployment: Three separate domains (.com, .app, .sol)
- Mobile: Offline-first architecture with batch sync

---

## Recent Changes (November 30, 2025)

### Team Management - Edit & Deactivate Features ✅
- **Edit Team Member**: Click Edit button to modify user details (first name, last name, email, phone, role)
- **Deactivate/Suspend Account**: Click Deactivate button to suspend user access with optional reason
- **Password Reset Integration**: Option to send password reset email when editing user details
- **API Endpoints**: 
  - GET `/api/users/{id}` - Fetch user details
  - PUT `/api/users/{id}` - Update user information  
  - POST `/api/users/{id}/deactivate` - Deactivate/suspend user account
  - POST `/api/WorkingAuth/send-password-reset/{userId}` - Send password reset email
- **Modal Forms**: Dedicated Edit User modal with role selection and optional password reset
- **Security**: Admin-only operations with proper authorization checks

## Recent Changes (November 28, 2025)

### Temporary Password System for User Creation ✅
- **Auto-Generated Passwords**: When admins create users without providing a password, system generates a secure 12-character random password
- **Email Delivery**: Temporary password sent via email (if available) with instructions to change on first login
- **PasswordNeedsChange Flag**: User.PasswordNeedsChange tracks if password must be changed during email verification
- **Change Password Endpoint**: POST /api/WorkingAuth/change-password requires current password + new password
- **Login Response**: Login endpoint now returns `passwordNeedsChange` flag for client-side password change prompt
- **Future Enhancement**: Email verification flow will force password change before account activation

### Massachusetts Code Engine (Merged Architecture) ✅
- **CodeBook Table**: Stores MA jurisdiction codes (780 CMR Building, 248 CMR Plumbing/Gas, 527 CMR 12 Electrical, 527 CMR 1 Fire, 521 CMR Accessibility)
- **CodeRule Table**: Trigger conditions linking scope selections to required permits/inspections with code section references
- **ScopeCategory + ScopeItem**: Job Options Wizard categories (Exterior, Structural, Plumbing, Electrical, HVAC, Sheet Metal, Finishes, Accessibility)
- **JobScope**: Links job to selected scope items with status tracking and trade assignment
- **TradeAssignment**: Assigns users to jobs with permit holder flag for licensed trades
- **Auto-Generation**: Selecting scopes auto-triggers required permits, inspection stages, and code references
- **Department Views**: Filter by BUILDING, ELECTRICAL, PLUMBING, MECHANICAL, FIRE, ACCESSIBILITY

### Updated Role Structure (Consolidated) ✅
```
Role Code | Name          | Limit | Permit Authority | Key Permissions
----------|---------------|-------|------------------|------------------
1510      | Master Admin  | 2     | No               | GC/Account Owner (individual or company), license holder, full control
1520      | Supervisor    | 4     | No               | Create jobs, approve scopes, assign trades
1530      | Foreman       | 3     | No               | On-site lead, daily tasks, mark ready for inspection
2001      | Field Operator| ∞     | No (GC permit)   | Task updates, checklists, photos
2010      | Subcontractor | ∞     | Yes              | Bid, hold permits, flag inspections
```

**Master Admin (1510)**: The General Contractor who is the account owner. Can be an individual name or company name. This is the entity that holds the PROVIA license.

**RoleConfig.cs**: Static class with role codes, limits, and permissions
**RolePermissions**: Per-role permission arrays for authorization checks

### Trade Types ✅
- **Licensed Trades** (require permit authority): Plumber, Electrician, HVAC, Sheet Metal, Gas Fitter, Fire Protection
- **General Trades** (work under GC permit): Roofing, Siding, Flooring, Tile, Painting, Drywall, Framing, Masonry, Demo, Windows, Finish Carpentry, Cabinets

---

## System Architecture

**Technology Stack:**
- Backend: .NET 6.0 + C# + Entity Framework Core
- Database: PostgreSQL (Neon-backed via Replit)
- Frontend: React + Node.js + HTML/CSS/JS
- Authentication: JWT (1-hour expiration) + BCrypt password hashing
- Messaging: SendGrid (email), Twilio/Textedly (SMS)
- Offline-First Mobile: Local sync queue for field workers

**UI/UX Decisions:**
- **Branding:** Custom 3-arrow SVG logo (orange/blue/navy), white background with orange (#FF9500)/teal (#2F5A7E) gradient UI
- **Role-Based Dashboards:** Five distinct interfaces with unique themes (Admin purple, Foreman navy, Sub-Contractor orange)
- **Unified Portal:** Single login for all users, auto-routes to role-based dashboard

**Technical Implementations:**
- **Multi-Tenancy:** Shared PostgreSQL database with CompanyId-based isolation via TenantContext service
- **Massachusetts Code Engine:** CodeBook + CodeRule tables auto-generate permits/inspections from scope selections
- **Multi-Company Subs:** SubContractorCompany junction table allows subs to work for multiple companies
- **Offline-First Mobile:** SyncQueueItem stores pending operations, MobileSyncService handles batch processing
- **Job Bidding System:** Subs bid → Foreman accepts with X% advance → Supervisor approves completion
- **Multi-Stage Inspections:** ROUGH → SECOND → FINISH → FINAL_SIGNOFF with permit document tracking

---

## API Endpoints

### Code Engine (Job Options Wizard)
- **GET /api/codeengine/scope-categories** → Get all scope categories with items for wizard
- **GET /api/codeengine/code-rules/{scopeItemCode}** → Get code rules triggered by scope
- **POST /api/codeengine/job/{jobId}/scopes** → Save selected scopes (auto-generates permits/inspections)
- **GET /api/codeengine/job/{jobId}/scopes** → Get scopes for a job
- **GET /api/codeengine/job/{jobId}/required-permits** → Get required permits based on scopes
- **GET /api/codeengine/job/{jobId}/department/{department}** → Department-filtered view
- **POST /api/codeengine/job/{jobId}/assign-trade** → Assign trade to job
- **GET /api/codeengine/job/{jobId}/assignments** → Get trade assignments for job
- **POST /api/codeengine/seed-codes** → Seed MA codes (admin only, one-time)
- **GET /api/codeengine/role-info** → Get role codes, limits, and trade types

### Job Bidding
- **POST /api/jobbid/place-bid** → Sub places bid on job
- **POST /api/jobbid/accept-bid** → Foreman accepts with payment terms
- **POST /api/jobbid/reject-bid/{bidId}** → Foreman rejects bid
- **POST /api/jobbid/approve-work** → Supervisor approves completion
- **GET /api/jobbid/job/{jobId}** → View all bids for a job
- **GET /api/jobbid/my-bids** → Sub-contractor views their bids

### Inspections & Permits
- **POST /api/inspection/create** → Create inspection for a stage
- **POST /api/inspection/submit/{inspectionId}** → Inspector submits results
- **POST /api/inspection/upload-permit** → Sub uploads signed permit
- **POST /api/inspection/approve-permit/{permitId}** → Admin/foreman approves permit
- **GET /api/inspection/bid/{jobBidId}** → View all inspections for a bid

### Mobile Sync
- **POST /api/MobileSync/push** → Upload batched changes from mobile
- **POST /api/MobileSync/pull** → Download server updates
- **POST /api/MobileSync/register-device** → Register device

---

## Important Files

**Code Engine:**
- `JobTracker/Models/CodeEngine.cs` - CodeBook, CodeRule, ScopeCategory, ScopeItem, JobScope, TradeAssignment models
- `JobTracker/Models/RoleConfig.cs` - RoleCodes, RolePermissions, DepartmentCodes, TradeTypes
- `JobTracker/Services/CodeEngineService.cs` - Auto-generation of permits/inspections from scopes
- `JobTracker/Controllers/CodeEngineController.cs` - Job Options Wizard and department view APIs

**Core System:**
- `JobTracker/Program.cs` - Configuration, services, middleware setup
- `JobTracker/Data/JobTrackerContext.cs` - Entity Framework DbContext with all tables
- `JobTracker/Services/TenantContext.cs` - Multi-tenant context tracking

**Bidding & Inspections:**
- `JobTracker/Models/JobBid.cs` - Bidding workflow model
- `JobTracker/Models/InspectionStage.cs` - Multi-stage inspection model
- `JobTracker/Services/JobBidService.cs` - Bidding business logic
- `JobTracker/Services/InspectionService.cs` - Inspection workflow logic

---

## External Dependencies
- **Database:** PostgreSQL (Neon-backed via Replit)
- **Email Service:** SendGrid
- **SMS Service:** Twilio/Textedly

---

## Known Status
- **App Build**: ✅ Successful with .NET 6.0 + Entity Framework Core
- **Database**: ✅ Connected with Code Engine + bidding + inspection tables
- **Code Engine**: ✅ MA jurisdiction codes integrated with auto-permit generation
- **Role System**: ✅ Consolidated with proper limits and permit authority flags
- **Server**: ✅ Running on 0.0.0.0:5000
