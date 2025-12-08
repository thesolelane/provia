using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using JobTracker.Data;
using JobTracker.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobTracker.Services
{
    public interface IPermitDocumentService
    {
        Task<PropertyProfile?> FetchAndSavePropertyDataAsync(int jobId, int companyId);
        Task<Dictionary<string, string>> GetPermitFormDataAsync(int jobPermitId, int companyId);
        Task<PermitDocument?> GeneratePermitDocumentAsync(int jobPermitId, int templateId, int companyId, int userId, Dictionary<string, string>? overrides = null);
        Task<List<PermitFormTemplate>> GetActiveTemplatesAsync(string? permitType = null);
    }

    public class PermitDocumentService : IPermitDocumentService
    {
        private readonly JobTrackerContext _context;
        private readonly IMassGISService _massGisService;
        private readonly ILogger<PermitDocumentService> _logger;

        public PermitDocumentService(
            JobTrackerContext context,
            IMassGISService massGisService,
            ILogger<PermitDocumentService> logger)
        {
            _context = context;
            _massGisService = massGisService;
            _logger = logger;
        }

        public async Task<PropertyProfile?> FetchAndSavePropertyDataAsync(int jobId, int companyId)
        {
            try
            {
                var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == jobId && j.CompanyId == companyId);
                if (job == null)
                {
                    _logger.LogWarning("Job not found: {JobId}", jobId);
                    return null;
                }

                var existingProfile = await _context.PropertyProfiles
                    .FirstOrDefaultAsync(p => p.JobId == jobId && p.CompanyId == companyId);

                var locationParts = (job.Location ?? "").Split(',');
                var address = locationParts.Length > 0 ? locationParts[0].Trim() : "";
                var city = locationParts.Length > 1 ? locationParts[1].Trim() : "";

                var gisData = await _massGisService.GetPropertyDataByAddressAsync(address, city);

                if (gisData == null)
                {
                    _logger.LogWarning("No GIS data found for job {JobId} at {Location}", jobId, job.Location);
                    return existingProfile;
                }

                if (existingProfile == null)
                {
                    existingProfile = new PropertyProfile
                    {
                        JobId = jobId,
                        CompanyId = companyId
                    };
                    _context.PropertyProfiles.Add(existingProfile);
                }

                existingProfile.PropertyAddress = gisData.SiteAddress ?? address;
                existingProfile.City = gisData.Town ?? city;
                existingProfile.ParcelId = gisData.LocId;
                existingProfile.MapLot = gisData.MapPar;
                existingProfile.OwnerName = gisData.Owner;
                existingProfile.OwnerAddress = gisData.OwnerAddress;
                existingProfile.LotAreaSqFt = gisData.LotSize;
                existingProfile.LandValue = gisData.LandValue;
                existingProfile.BuildingValue = gisData.BuildingValue;
                existingProfile.TotalAssessedValue = gisData.TotalValue;
                existingProfile.UseCode = gisData.UseCode;
                existingProfile.UseDescription = gisData.UseDescription;
                existingProfile.YearBuilt = gisData.YearBuilt;
                existingProfile.FiscalYear = gisData.FiscalYear;
                existingProfile.GisDataJson = JsonSerializer.Serialize(gisData);
                existingProfile.GisFetchedAt = DateTime.UtcNow;
                existingProfile.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Saved property profile for job {JobId}: {ParcelId}", jobId, gisData.LocId);
                return existingProfile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching/saving property data for job {JobId}", jobId);
                return null;
            }
        }

        public async Task<Dictionary<string, string>> GetPermitFormDataAsync(int jobPermitId, int companyId)
        {
            var data = new Dictionary<string, string>();

            try
            {
                var permit = await _context.JobPermits
                    .Include(p => p.Job)
                    .FirstOrDefaultAsync(p => p.Id == jobPermitId && p.CompanyId == companyId);

                if (permit == null) return data;

                var job = permit.Job;
                if (job == null) return data;

                var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == companyId);

                var propertyProfile = await _context.PropertyProfiles
                    .FirstOrDefaultAsync(p => p.JobId == job.Id && p.CompanyId == companyId);

                data["permit_type"] = permit.PermitType ?? "";
                data["permit_number"] = permit.PermitNumber ?? "";
                data["permit_status"] = permit.Status ?? "";

                var locationParts = (job.Location ?? "").Split(',');
                var address = locationParts.Length > 0 ? locationParts[0].Trim() : "";
                var city = locationParts.Length > 1 ? locationParts[1].Trim() : "";

                data["job_number"] = job.JobNumber ?? "";
                data["job_name"] = job.Name ?? "";
                data["job_description"] = job.Description ?? "";
                data["property_address"] = address;
                data["property_city"] = city;
                data["property_state"] = "MA";
                data["property_zip"] = "";

                if (company != null)
                {
                    data["contractor_name"] = company.CompanyName ?? "";
                    data["contractor_address"] = company.Address ?? "";
                    data["contractor_city"] = company.City ?? "";
                    data["contractor_state"] = company.State ?? "";
                    data["contractor_zip"] = company.ZipCode ?? "";
                    data["contractor_phone"] = company.ContactPhone ?? "";
                    data["contractor_email"] = company.ContactEmail ?? "";
                }

                if (propertyProfile != null)
                {
                    data["parcel_id"] = propertyProfile.ParcelId ?? "";
                    data["map_lot"] = propertyProfile.MapLot ?? "";
                    data["owner_name"] = propertyProfile.OwnerName ?? "";
                    data["owner_address"] = propertyProfile.OwnerAddress ?? "";
                    data["zoning_code"] = propertyProfile.ZoningCode ?? "";
                    data["zoning_description"] = propertyProfile.ZoningDescription ?? "";
                    data["lot_size"] = propertyProfile.LotAreaSqFt?.ToString("N0") ?? "";
                    data["building_sqft"] = propertyProfile.BuildingSqFt?.ToString("N0") ?? "";
                    data["land_value"] = propertyProfile.LandValue?.ToString("C0") ?? "";
                    data["building_value"] = propertyProfile.BuildingValue?.ToString("C0") ?? "";
                    data["total_assessed_value"] = propertyProfile.TotalAssessedValue?.ToString("C0") ?? "";
                    data["use_code"] = propertyProfile.UseCode ?? "";
                    data["use_description"] = propertyProfile.UseDescription ?? "";
                    data["year_built"] = propertyProfile.YearBuilt?.ToString() ?? "";
                    data["fiscal_year"] = propertyProfile.FiscalYear?.ToString() ?? "";
                }

                data["application_date"] = DateTime.Now.ToString("MM/dd/yyyy");
                data["current_date"] = DateTime.Now.ToString("MM/dd/yyyy");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permit form data for permit {PermitId}", jobPermitId);
            }

            return data;
        }

        public async Task<PermitDocument?> GeneratePermitDocumentAsync(
            int jobPermitId, 
            int templateId, 
            int companyId, 
            int userId,
            Dictionary<string, string>? overrides = null)
        {
            try
            {
                var template = await _context.PermitFormTemplates
                    .FirstOrDefaultAsync(t => t.Id == templateId && t.IsActive);

                if (template == null)
                {
                    _logger.LogWarning("Template not found: {TemplateId}", templateId);
                    return null;
                }

                var formData = await GetPermitFormDataAsync(jobPermitId, companyId);

                if (overrides != null)
                {
                    foreach (var kvp in overrides)
                    {
                        formData[kvp.Key] = kvp.Value;
                    }
                }

                var storagePath = $"permits/{companyId}/{jobPermitId}_{template.PermitType}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";

                var document = new PermitDocument
                {
                    JobPermitId = jobPermitId,
                    TemplateId = templateId,
                    CompanyId = companyId,
                    StoragePath = storagePath,
                    Status = "GENERATED",
                    GeneratedAt = DateTime.UtcNow,
                    GeneratedByUserId = userId,
                    FieldDataJson = JsonSerializer.Serialize(formData)
                };

                _context.PermitDocuments.Add(document);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created permit document record for permit {PermitId}, template {TemplateId}", 
                    jobPermitId, templateId);

                return document;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating permit document");
                return null;
            }
        }

        public async Task<List<PermitFormTemplate>> GetActiveTemplatesAsync(string? permitType = null)
        {
            var query = _context.PermitFormTemplates.Where(t => t.IsActive);

            if (!string.IsNullOrEmpty(permitType))
            {
                query = query.Where(t => t.PermitType == permitType);
            }

            return await query.OrderBy(t => t.TemplateName).ToListAsync();
        }
    }
}
