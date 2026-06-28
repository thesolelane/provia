namespace JobTracker.Models
{
    public class CreateJobSectionRequest
    {
        public int JobId { get; set; }
        public int SectionType { get; set; }
        public int Status { get; set; }
        public bool IsSubcontracted { get; set; }
        public string? Notes { get; set; }
        public string? Description { get; set; }
    }
}