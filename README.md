# PROVIA

**Enterprise Construction Management Platform**

![PROVIA Logo](./JobTracker/wwwroot/images/provia-logo.svg)

A secure, multi-tenant construction management system designed for companies like Preferred Builders USA, LLC. PROVIA enables complete job tracking, workforce management, scheduling, and real-time location monitoring across multiple projects.

---

## 🎯 Overview

PROVIA is built to handle the complexity of modern construction operations:

- **Multi-tenant Architecture**: Multiple construction companies operate independently within a single database using CompanyId-based data isolation
- **Enterprise Security**: BCrypt password hashing, JWT authentication (1-hour expiration), rate limiting, and role-based access control
- **Real-time Collaboration**: Job tracking, time logging, schedule management, and location monitoring
- **Scalable Design**: Support for unlimited Supervisors and Field Operators per company, with role limits for Admins (2) and Foremen (5)

---

## 🛠️ Technology Stack

| Layer | Technology |
|-------|-----------|
| **Backend** | .NET 6.0 + C# + Entity Framework Core |
| **Database** | PostgreSQL (Neon-backed) |
| **Frontend** | React + Node.js |
| **Authentication** | JWT (Bearer tokens) + BCrypt hashing |
| **Email** | SendGrid |
| **SMS** | Twilio / Textedly |
| **Deployment** | Replit / Cloud |

---

## 🚀 Quick Start

### Prerequisites
- .NET 6.0 SDK
- PostgreSQL database
- Environment variables configured

### Setup

1. **Clone and navigate to project**
   ```bash
   cd JobTracker
   ```

2. **Configure environment variables**
   ```bash
   # Set in Replit secrets or .env
   JWT_SECRET_KEY=your_secret_key_here
   ALLOWED_ORIGINS=https://your-domain.com
   DATABASE_URL=postgresql://user:password@host:port/dbname
   SENDGRID_API_KEY=your_sendgrid_key
   TWILIO_ACCOUNT_SID=your_twilio_sid
   TWILIO_AUTH_TOKEN=your_twilio_token
   ```

3. **Build and run**
   ```bash
   dotnet build
   dotnet run --urls=http://0.0.0.0:5000
   ```

4. **Access the app**
   - Open http://localhost:5000 (or your Replit domain)
   - Login with seeded demo credentials or register a new company

---

## 🏗️ Architecture

### Multi-Tenancy Model

PROVIA uses a **shared database with CompanyId filtering** for complete data isolation:

```
┌─────────────────────────────────────┐
│     Single PostgreSQL Database      │
├─────────────────────────────────────┤
│ Companies Table                     │
│ ├─ Company 1 (Preferred Builders)   │
│ ├─ Company 2 (Partner Company)      │
│ └─ Company 3 (Another Contractor)   │
├─────────────────────────────────────┤
│ Users, Jobs, TimeEntries (all filtered by CompanyId)
│ - Automatic filtering in middleware │
│ - Enforced in all queries           │
│ - Validated at API endpoints        │
└─────────────────────────────────────┘
```

### Data Flow

```
User Login → JWT Token (with CompanyId) → TenantContext (extracts CompanyId)
   ↓
All database queries automatically filtered by CompanyId
   ↓
User sees only their company's data
```

### Key Services

- **TenantContext** (`Services/TenantContext.cs`): Manages current company context from JWT claims
- **TenantContextMiddleware** (`Middleware/TenantContextMiddleware.cs`): Populates tenant context on each request
- **RateLimitingMiddleware** (`Middleware/RateLimitingMiddleware.cs`): Protects against brute force (5 attempts → 15-min lockout)

---

## 🔐 Security Features

### Authentication & Authorization
- **JWT Tokens**: 1-hour expiration, signed with secret key
- **BCrypt Hashing**: All passwords hashed before storage (no plaintext)
- **Rate Limiting**: 5 failed login attempts → 15-minute lockout
- **CORS Protection**: Only allowed origins can access API

### Data Protection
- **Automatic Tenant Filtering**: Middleware enforces CompanyId filtering on all queries
- **Role-Based Access Control**: Users can only perform actions their role allows
- **Secure Endpoints**: All sensitive operations require valid JWT token

### Credential Management
- All secrets stored in environment variables (never committed to code)
- No hardcoded API keys or passwords
- Automatic secret rotation support

---

## 👥 User Roles & Permissions

Each company manages its own users with the following role structure:

| Role | Role Code | Max per Company | Permissions |
|------|-----------|-----------------|------------|
| **Admin** | 1510 | 2 | Full system access, user management, reports |
| **Foreman** | 1520 | 5 | Schedule management, team supervision, job updates |
| **Supervisor** | 1530 | Unlimited | Job oversight, time tracking approval, reporting |
| **Field Operator** | 2001 | Unlimited | Clock in/out, location tracking, job updates |

### Company-Centric Login
- Single unified login for all users (no admin/user tabs)
- All users login with email + password
- Role and CompanyId extracted from JWT claims on every request
- Frontend routing determined by user's role

---

## 🔄 Onboarding Process

### New Company Registration

1. **Company Registration**
   ```
   POST /api/Company/register
   {
     "companyName": "Acme Construction",
     "contactEmail": "admin@acmeconstruction.com",
     "contactPhone": "(555) 123-4567",
     "adminFirstName": "John",
     "adminLastName": "Doe",
     "adminPassword": "SecurePassword123",
     "requestedSubscription": "Standard"
   }
   ```

2. **System Response**
   - Company created with unique account number
   - Master Admin user created and email verified
   - Subscription limits applied
   - Company ready to use

3. **Admin Creates Team**
   - Admin logs in and creates additional users
   - Assigns roles (Foreman, Supervisor, Field Operator)
   - Users receive invitations or temporary codes
   - Team members join with own credentials

### Initial Setup (Development)
- Database seeder creates "Preferred Builders USA, LLC" with demo users on first startup
- Demo credentials available for immediate testing

---

## 📊 Key Features

### Job Management
- Create and track construction projects
- Organize jobs into sections/phases
- Real-time status updates
- Image attachments and documentation

### Workforce Management
- Time tracking with clock in/out
- Location tracking via GPS geo-fencing
- Schedule management and shift planning
- Employee verification and approval workflows

### Communication
- Email notifications (SendGrid)
- SMS alerts (Twilio/Textedly)
- In-app messaging
- Issue tracking and escalation

### Reporting & Analytics
- Job progress reports
- Time tracking analytics
- Attendance records
- Performance metrics

---

## 🗄️ Database

### Schema
- **Companies**: Multi-tenant containers
- **Users**: Employee records with CompanyId
- **Jobs**: Construction projects
- **JobSections**: Project phases
- **TimeEntries**: Work hours logging
- **LocationTracking**: GPS coordinates
- **Issues**: Problem tracking
- **MaterialRuns**: Material management
- And many more supporting tables...

### CompanyId Filtering
- Every major table includes `CompanyId` column
- Queries automatically scoped to current company
- Middleware enforces this on every request

---

## 🛠️ Development

### Project Structure
```
JobTracker/
├── Controllers/          # API endpoints
├── Models/              # Data models
├── Services/            # Business logic
├── Middleware/          # Request processing
├── Data/                # Database context
├── wwwroot/             # Frontend HTML/CSS/JS
└── ClientApp/           # React components
```

### Key Controllers
- `WorkingAuthController.cs` - Secure JWT authentication
- `CompanyController.cs` - Company registration and management
- `UsersController.cs` - User management
- `JobsController.cs` - Job tracking
- `TimeTrackingController.cs` - Time and attendance
- `LocationTrackingController.cs` - GPS tracking

### Running Locally
```bash
cd JobTracker
dotnet build
dotnet run --urls=http://0.0.0.0:5000
```

### Database Migrations
```bash
# Create migration
dotnet ef migrations add MigrationName

# Apply migration
dotnet ef database update
```

---

## 📝 Environment Variables

Required for production:

```env
# Core
JWT_SECRET_KEY=<your-jwt-secret>
ALLOWED_ORIGINS=https://app.provia.com,https://admin.provia.com
DATABASE_URL=postgresql://user:pass@host:5432/provia_db

# Notifications (optional)
SENDGRID_API_KEY=<your-sendgrid-key>
TWILIO_ACCOUNT_SID=<your-twilio-sid>
TWILIO_AUTH_TOKEN=<your-twilio-token>

# Optional: Company Branding
COMPANY_NAME=PROVIA
SUPPORT_EMAIL=support@provia.com
```

---

## 🚀 Deployment

### Replit Deployment
1. Configure environment variables in Replit secrets
2. Set workflow to: `cd JobTracker && dotnet run --urls=http://0.0.0.0:5000`
3. Click "Publish" for live URL

### Production Checklist
- [ ] JWT_SECRET_KEY set to strong random value
- [ ] ALLOWED_ORIGINS configured for your domain
- [ ] DATABASE_URL points to production PostgreSQL
- [ ] SendGrid and Twilio keys configured
- [ ] SSL/TLS enabled
- [ ] Database backups configured
- [ ] Rate limiting tested
- [ ] CORS policy verified

---

## 🐛 Troubleshooting

### Login Issues
- Verify BCrypt password hashing is working
- Check JWT_SECRET_KEY is set
- Ensure user's IsActive flag is true
- Verify user has assigned company

### Database Connection
- Confirm DATABASE_URL format: `postgresql://user:pass@host:port/dbname`
- Check database server is running
- Verify firewall allows connections

### Tenant Isolation Issues
- Ensure all queries use TenantContext.CompanyId
- Check middleware is registered in Program.cs
- Verify JWT token contains CompanyId claim

---

## 📚 API Documentation

### Authentication
```
POST /api/working-auth/login
Content-Type: application/json

{
  "identifier": "user@company.com",
  "password": "password123"
}

Response:
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "user": {
    "id": 1,
    "email": "user@company.com",
    "role": 1530,
    "companyId": 1
  }
}
```

### Company Registration
```
POST /api/Company/register
Content-Type: application/json

{
  "companyName": "Company Name",
  "contactEmail": "admin@company.com",
  "contactPhone": "(555) 123-4567",
  "adminFirstName": "John",
  "adminLastName": "Doe",
  "adminPassword": "SecurePassword123",
  "requestedSubscription": "Standard"
}
```

---

## 📄 License

PROVIA is proprietary software developed for Preferred Builders USA, LLC.

---

## 🤝 Support

For issues, bugs, or feature requests, contact the development team or open an issue in the project repository.

---

**Made with ❤️ for construction professionals**
