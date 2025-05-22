using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobTrackerApp.Models
{
    public class Employee
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(50)]
        public string Role { get; set; } = string.Empty;

        [StringLength(100)]
        public string ActiveDirectoryId { get; set; } = string.Empty;

        [StringLength(30)]
        public string EmployeeNumber { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime HireDate { get; set; }

        public DateTime? TerminationDate { get; set; }

        [StringLength(100)]
        public string EmergencyContactName { get; set; } = string.Empty;

        [StringLength(20)]
        public string EmergencyContactPhone { get; set; } = string.Empty;

        public string? Notes { get; set; }

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;

        // Navigation properties
        public ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();
        public ICollection<JobAssignment> JobAssignments { get; set; } = new List<JobAssignment>();
        public ICollection<JobSection> ResponsibleForSections { get; set; } = new List<JobSection>();
    }
}
