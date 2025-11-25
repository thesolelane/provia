using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobTracker.Models
{
    /// <summary>
    /// Client contact information with role-based access control
    /// Stores contact details WITHOUT billing information
    /// </summary>
    public class ClientInfo
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CompanyId { get; set; }
        public Company? Company { get; set; }

        // Contact Information (NO BILLING DATA)
        [Required]
        [StringLength(100)]
        public string ContactName { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Address { get; set; }

        [StringLength(50)]
        public string? City { get; set; }

        [StringLength(50)]
        public string? State { get; set; }

        [StringLength(20)]
        public string? ZipCode { get; set; }

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        // Emergency Contact Fields
        [StringLength(100)]
        public string? EmergencyContactName { get; set; }

        [StringLength(20)]
        public string? EmergencyContactPhone { get; set; }

        [StringLength(100)]
        public string? EmergencyContactEmail { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // For tracking which company user created/updated
        public int? CreatedByUserId { get; set; }
        public int? UpdatedByUserId { get; set; }
    }

    /// <summary>
    /// DTO for role-based client info visibility
    /// Different roles see different levels of data
    /// </summary>
    public class ClientInfoDto
    {
        public int Id { get; set; }
        public string ContactName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Notes { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? EmergencyContactEmail { get; set; }

        public static ClientInfoDto FromModel(ClientInfo client, int userRole)
        {
            return userRole switch
            {
                // Admin (1510): Full contact information
                1510 => new ClientInfoDto
                {
                    Id = client.Id,
                    ContactName = client.ContactName,
                    Address = client.Address,
                    City = client.City,
                    State = client.State,
                    ZipCode = client.ZipCode,
                    PhoneNumber = client.PhoneNumber,
                    Email = client.Email,
                    Notes = client.Notes,
                    EmergencyContactName = client.EmergencyContactName,
                    EmergencyContactPhone = client.EmergencyContactPhone,
                    EmergencyContactEmail = client.EmergencyContactEmail
                },
                // Foreman (1520): Full contact information
                1520 => new ClientInfoDto
                {
                    Id = client.Id,
                    ContactName = client.ContactName,
                    Address = client.Address,
                    City = client.City,
                    State = client.State,
                    ZipCode = client.ZipCode,
                    PhoneNumber = client.PhoneNumber,
                    Email = client.Email,
                    Notes = client.Notes,
                    EmergencyContactName = client.EmergencyContactName,
                    EmergencyContactPhone = client.EmergencyContactPhone,
                    EmergencyContactEmail = client.EmergencyContactEmail
                },
                // Supervisor (1530): Email or phone only (emergency)
                1530 => new ClientInfoDto
                {
                    Id = client.Id,
                    ContactName = client.ContactName,
                    PhoneNumber = client.EmergencyContactPhone,
                    Email = client.EmergencyContactEmail
                },
                // Field Operator (2001): Email or phone only (emergency)
                2001 => new ClientInfoDto
                {
                    Id = client.Id,
                    ContactName = client.ContactName,
                    PhoneNumber = client.EmergencyContactPhone,
                    Email = client.EmergencyContactEmail
                },
                // Sub-Contractor (2010): Emergency contact only (via admin/foreman)
                2010 => new ClientInfoDto
                {
                    Id = client.Id,
                    EmergencyContactPhone = client.EmergencyContactPhone,
                    EmergencyContactEmail = client.EmergencyContactEmail
                },
                // Default: Minimal info
                _ => new ClientInfoDto
                {
                    Id = client.Id,
                    ContactName = client.ContactName
                }
            };
        }
    }
}
