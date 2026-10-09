namespace JobTracker.Services
{
    public sealed class TenantContextException : InvalidOperationException
    {
        public TenantContextException()
            : base("Tenant context is required for this operation.")
        {
        }
    }

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
            if (_currentCompanyId <= 0)
            {
                throw new TenantContextException();
            }
            return _currentCompanyId;
        }

        public void SetCurrentCompanyId(int companyId)
        {
            _currentCompanyId = companyId;
        }
    }
}
