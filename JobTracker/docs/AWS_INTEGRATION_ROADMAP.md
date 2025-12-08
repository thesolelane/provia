# PROVIA AWS Integration & Migration Roadmap

## Document Information
- **Created:** December 8, 2024
- **Purpose:** Strategic planning for AWS integration and future migration
- **Status:** Planning Phase

---

## Current Architecture (Replit-Hosted)

| Component | Current Solution |
|-----------|-----------------|
| Application Runtime | Replit (.NET 6.0) |
| Database | Neon PostgreSQL (via Replit) |
| File Storage | Local filesystem |
| Secrets Management | Replit Secrets |
| Email | SendGrid |
| SMS | Twilio |

---

## Phase 1: Hybrid Architecture (Immediate Option)

Integrate AWS services while remaining hosted on Replit.

### Recommended AWS Services

| Service | Purpose | Priority |
|---------|---------|----------|
| **Amazon S3** | Permit document storage, generated PDFs | High |
| **AWS Secrets Manager** | Municipal portal credentials (encrypted) | High |
| **Amazon SES** | Email service (optional, replace SendGrid) | Medium |
| **CloudWatch Logs** | Centralized audit logging for compliance | Medium |
| **AWS KMS** | Encryption keys for sensitive data | Medium |

### Security Requirements for Hybrid

1. **IAM Configuration**
   - Create dedicated IAM user for PROVIA
   - Apply least-privilege policies (only required S3 buckets, Secrets Manager access)
   - Enable MFA on AWS root account
   - Rotate access keys every 90 days

2. **Credential Storage**
   - Store AWS_ACCESS_KEY_ID in Replit Secrets
   - Store AWS_SECRET_ACCESS_KEY in Replit Secrets
   - Never commit credentials to code

3. **S3 Security**
   - Enable server-side encryption (SSE-S3 or SSE-KMS)
   - Use presigned URLs for document access (expire in 15 minutes)
   - Enable versioning for document audit trail
   - Configure bucket policies to restrict access

4. **Network Security**
   - Consider IP allowlists on AWS API Gateway
   - Enable CloudTrail for API access auditing
   - Use VPC endpoints if sensitive data flows increase

### Implementation Example (S3 Document Storage)

```csharp
// Add to appsettings.json
{
  "AWS": {
    "Region": "us-east-1",
    "S3BucketName": "provia-permits-prod"
  }
}

// Service implementation
public class S3DocumentService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    public async Task<string> UploadPermitDocumentAsync(
        int companyId, int permitId, Stream fileStream, string fileName)
    {
        var key = $"companies/{companyId}/permits/{permitId}/{fileName}";
        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = fileStream,
            ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
        };
        await _s3Client.PutObjectAsync(request);
        return key;
    }

    public string GetPresignedUrl(string key, int expirationMinutes = 15)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = key,
            Expires = DateTime.UtcNow.AddMinutes(expirationMinutes)
        };
        return _s3Client.GetPreSignedURL(request);
    }
}
```

---

## Phase 2: Migration Preparation

### Containerization

Create Dockerfile for the application:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["JobTracker/JobTracker.csproj", "JobTracker/"]
RUN dotnet restore "JobTracker/JobTracker.csproj"
COPY . .
WORKDIR "/src/JobTracker"
RUN dotnet build "JobTracker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "JobTracker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "JobTracker.dll"]
```

### Infrastructure as Code (Terraform)

```hcl
# Basic AWS infrastructure for PROVIA
resource "aws_ecs_cluster" "provia" {
  name = "provia-cluster"
}

resource "aws_db_instance" "provia_postgres" {
  identifier           = "provia-db"
  engine               = "postgres"
  engine_version       = "14"
  instance_class       = "db.t3.medium"
  allocated_storage    = 100
  storage_encrypted    = true
  multi_az             = true
  
  vpc_security_group_ids = [aws_security_group.db.id]
  db_subnet_group_name   = aws_db_subnet_group.provia.name
}

resource "aws_s3_bucket" "permits" {
  bucket = "provia-permits-prod"
  
  versioning {
    enabled = true
  }
  
  server_side_encryption_configuration {
    rule {
      apply_server_side_encryption_by_default {
        sse_algorithm = "aws:kms"
      }
    }
  }
}
```

---

## Phase 3: Full AWS Migration

### Target Architecture

| Component | AWS Service | Configuration |
|-----------|-------------|---------------|
| Application | ECS Fargate or Elastic Beanstalk | Auto-scaling, ALB |
| Database | Amazon RDS PostgreSQL | Multi-AZ, automated backups |
| Static Files | S3 + CloudFront | CDN distribution |
| Secrets | AWS Secrets Manager | Automatic rotation |
| Logs | CloudWatch Logs | Centralized, searchable |
| Monitoring | CloudWatch + X-Ray | APM, tracing |
| Security | WAF, GuardDuty, Security Hub | Threat detection |

### Database Migration Strategy

1. **Preparation**
   - Enable logical replication on Neon PostgreSQL
   - Create RDS PostgreSQL instance in target VPC
   - Set up AWS DMS (Database Migration Service) if needed

2. **Migration Steps**
   ```bash
   # Export from Neon
   pg_dump -h neon-host -U user -d database > provia_backup.sql
   
   # Import to RDS
   psql -h rds-endpoint -U admin -d provia < provia_backup.sql
   ```

3. **Cutover**
   - Schedule maintenance window (recommended: 2-4 hours)
   - Put application in maintenance mode
   - Final sync of data
   - Update connection strings
   - Verify application functionality
   - Enable production traffic

### Security Compliance Checklist

For municipal government integrations:

- [ ] VPC with private subnets for database
- [ ] Security groups with minimal ports open
- [ ] WAF rules for API protection
- [ ] CloudTrail enabled for all API calls
- [ ] GuardDuty for threat detection
- [ ] Config rules for compliance monitoring
- [ ] KMS keys for encryption at rest
- [ ] ACM certificates for encryption in transit
- [ ] IAM Identity Center for admin access
- [ ] MFA required for all IAM users

### Compliance Frameworks

| Framework | AWS Support | Relevance |
|-----------|-------------|-----------|
| SOC 2 Type II | Certified | Required for enterprise contracts |
| FedRAMP | Authorized (GovCloud) | If serving federal projects |
| MA 201 CMR 17.00 | Achievable | MA personal data protection |
| CJIS | Achievable with controls | If accessing criminal records |

---

## Cost Estimates

### Hybrid (Replit + AWS)
| Service | Monthly Cost |
|---------|-------------|
| S3 (100GB) | ~$2-5 |
| Secrets Manager (50 secrets) | ~$20 |
| CloudWatch Logs | ~$5-10 |
| **Total** | **~$30-40/month** |

### Full AWS Migration
| Service | Monthly Cost |
|---------|-------------|
| ECS Fargate (2 tasks) | ~$50-100 |
| RDS PostgreSQL (db.t3.medium) | ~$60-80 |
| ALB | ~$20 |
| S3 + CloudFront | ~$10-20 |
| Other services | ~$20-30 |
| **Total** | **~$160-250/month** |

---

## Decision Criteria

### Stay on Replit When:
- Rapid development is priority
- Team size is small
- No compliance requirements yet
- Budget is limited

### Migrate to AWS When:
- Enterprise municipal contracts require SOC 2
- Need CJIS compliance for criminal record access
- Data residency requirements
- Scale beyond Replit limits
- Need advanced security features

---

## Next Steps

1. **Immediate:** Continue development on Replit
2. **When Ready:** Pilot S3 integration for document storage
3. **Before Enterprise Sales:** Prepare migration runbook
4. **For Major Contracts:** Execute full AWS migration

---

## Related Files
- `JobTracker/Models/MunicipalIntegration.cs` - Portal credential storage model
- `JobTracker/Models/PermitDocument.cs` - Document storage models
- `replit.md` - Project documentation
