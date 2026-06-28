using System.ComponentModel.DataAnnotations;

namespace JobTrackerApp.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [StringLength(100)]
        public string ActiveDirectoryId { get; set; } = string.Empty;

        public int? EmployeeId { get; set; }

        [Required]
        [StringLength(50)]
        public string Role { get; set; } = "User"; // Admin, ProjectManager, Employee, User

        public bool IsActive { get; set; } = true;

        public DateTime LastLogin { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
    }
}
