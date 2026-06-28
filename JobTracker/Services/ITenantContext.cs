namespace JobTracker.Services
{
    public interface ITenantContext
    {
        int CompanyId { get; }
        int UserId { get; set; }
        int GetCurrentCompanyId();
        void SetCurrentCompanyId(int companyId);
    }
}
