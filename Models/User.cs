using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace JobTracker.Models
{
    public class User
    {
        [Key]
        public string UserId { get; set; } // Active Directory ID

        [Required]
        [MaxLength(100)]
        public string UserName { get; set; }

        [Required]
        [MaxLength(100)]
        public string Email { get; set; }

        [MaxLength(100)]
        public string FirstName { get; set; }

        [MaxLength(100)]
        public string LastName { get; set; }

        [MaxLength(20)]
        public string Phone { get; set; }

        [Required]
        public UserRole Role { get; set; }

        public bool IsActive { get; set; } = true;

        [MaxLength(100)]
        public string JobTitle { get; set; }

        [MaxLength(100)]
        public string Department { get; set; }

        [MaxLength(200)]
        public string Skills { get; set; }

        [MaxLength(200)]
        public string Certifications { get; set; }

        // Navigation properties
        public virtual ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();

        // Auditing
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }
    }

    public enum UserRole
    {
        Administrator,
        ProjectManager,
        Supervisor,
        Worker,
        Accountant,
        Viewer
    }
}
