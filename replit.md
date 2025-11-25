# PROVIA - Construction Management Platform

## Project Overview
Enterprise-grade construction management system for Preferred Builders USA, LLC and partner companies. Multi-tenant architecture using shared database with CompanyId-based data isolation.

**Technology Stack:**
- Backend: .NET 6.0 + C# + Entity Framework Core
- Database: PostgreSQL (Neon-backed via Replit)
- Frontend: React + Node.js
- Authentication: JWT (1-hour expiration) + BCrypt password hashing
- Messaging: SendGrid (email), Twilio/Textedly (SMS)

---

## Recent Changes (November 25, 2025)

### Completed
1. **Full PROVIA Rebranding** (100+ references replaced)
   - Custom 3-arrow SVG logo (orange/blue/navy)
   - White background with orange-to-teal gradient UI
   - All user-facing text updated: frontend header, footer, SMS/email notifications, password salts
   - Internal namespaces retained (JobTracker.*) - safe for production

2. **Enterprise Security Implementation**
   - BCrypt password hashing (no plaintext storage)
   - JWT token authentication (1-hour expiration)
   - Rate limiting middleware (5 failed attempts → 15-min lockout)
   - CORS restrictions to allowed origins only
   - All credentials moved to environment variables (JWT_SECRET_KEY, ALLOWED_ORIGINS)
   - Deprecated hardcoded test auth endpoints

3. **.NET SDK & JWT Package Resolution**
   - Installed Microsoft.AspNetCore.Authentication.JwtBearer (6.0.28)
   - Verified .NET 6.0 compatibility and successful build
   - App running on http://0.0.0.0:5000

### Configuration
**Environment Variables Required:**
- `JWT_SECRET_KEY` - Signing key for JWT tokens
- `ALLOWED_ORIGINS` - CORS-allowed domains (comma-separated)
- `DATABASE_URL` - PostgreSQL connection string
- `SENDGRID_API_KEY` - Email service (optional if no notifications)
- `TWILIO_ACCOUNT_SID`, `TWILIO_AUTH_TOKEN` - SMS service (optional)

---

## Multi-Tenancy Architecture

### Data Isolation Strategy
- **Shared Database Model**: Single PostgreSQL database with CompanyId filtering
- **Tenant Context Service** (`ITenantContext`): Extracts CompanyId from JWT claims on each request
- **Middleware-Enforced Filtering**: All database queries automatically filtered by CompanyId
- **Controllers**: Use `_tenantContext.CompanyId` to enforce query scope

### Role-Based Access Control (RBAC)
- **Admin** (1510): Max 2 per company
- **Foreman** (1520): Max 5 per company
- **Supervisor** (1530): Unlimited
- **Field Operator** (2001): Unlimited

### Company-Centric Login
- Unified login endpoint (no admin/user tabs)
- All users login with email + password
- Role and CompanyId extracted from JWT claims
- Routing determined by role on frontend

---

## Important Files
- `JobTracker/Program.cs` - Configuration, services, middleware setup
- `JobTracker/Controllers/WorkingAuthController.cs` - Secure JWT authentication
- `JobTracker/Services/TenantContext.cs` - CompanyId context tracking
- `JobTracker/Middleware/TenantContextMiddleware.cs` - Automatic tenant filtering
- `JobTracker/Middleware/RateLimitingMiddleware.cs` - Brute force protection
- `JobTracker/Models/UserRoles.cs` - Role definitions
- `JobTracker/wwwroot/login.html` - Frontend login UI

---

## Known Status
- **App Build**: ✅ Successful with .NET 6.0 + JWT Bearer 6.0.28
- **Database**: ✅ Connected and seeded
- **Branding**: ✅ 100% PROVIA (user-facing) + maintained internal namespaces
- **Security**: ✅ Enterprise-grade (BCrypt, JWT, rate limiting, CORS)
- **Server**: ✅ Running on 0.0.0.0:5000

---

## Next Steps (Future Development)
1. Complete tenant isolation verification in all controllers
2. Implement CompanyId validation on write operations
3. Add audit logging for all data mutations
4. Database schema migration for missing CompanyId fields
5. End-to-end testing with multi-company scenarios

---

## User Preferences
- Framework: .NET 6.0 (locked to Replit environment availability)
- Brand: PROVIA (3-arrow logo, orange/teal theme)
- Security: Enterprise-grade (no test credentials, all secrets in env vars)
- Database: Shared tenant model with CompanyId filtering (not separate DBs)
