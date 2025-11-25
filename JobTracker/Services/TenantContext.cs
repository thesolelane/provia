namespace JobTracker.Services
{
    public class TenantContext : ITenantContext
    {
        private int _currentCompanyId;

        public int GetCurrentCompanyId()
        {
            if (_currentCompanyId == 0)
            {
                throw new InvalidOperationException("Tenant context not set. User may not be authenticated or company not found.");
            }
            return _currentCompanyId;
        }

        public void SetCurrentCompanyId(int companyId)
        {
            _currentCompanyId = companyId;
        }
    }
}
