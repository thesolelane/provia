namespace JobTracker.Services
{
    public class TenantContext : ITenantContext
    {
        private int _currentCompanyId;
        private int _userId;

        public int CompanyId => GetCurrentCompanyId();
        public int UserId 
        { 
            get => _userId;
            set => _userId = value;
        }

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
