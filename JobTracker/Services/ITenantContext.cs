namespace JobTracker.Services
{
    public interface ITenantContext
    {
        int GetCurrentCompanyId();
        void SetCurrentCompanyId(int companyId);
    }
}
