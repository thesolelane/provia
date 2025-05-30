using JobTracker.Data;
using JobTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Services
{
    public class UserCodeService
    {
        private readonly JobTrackerContext _context;

        public UserCodeService(JobTrackerContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateUserCodeAsync(int companyId, int roleCode)
        {
            // Get company account number
            var company = await _context.Companies.FindAsync(companyId);
            if (company == null) throw new ArgumentException("Company not found");

            // Extract account identifier (36DMRD from account number)
            var accountNumber = company.AccountNumber ?? "";
            var accountParts = accountNumber.Split('-');
            var accountCode = accountParts.Length > 2 ? accountParts[2] : "DEFAULT";

            // Get next sequential number for this role within the company
            var existingCodes = await _context.Users
                .Where(u => u.CompanyId == companyId && u.Role == roleCode && u.UserCode != null)
                .Select(u => u.UserCode)
                .ToListAsync();

            var nextSequence = 1;
            while (existingCodes.Any(code => code.EndsWith($"-{nextSequence:D3}")))
            {
                nextSequence++;
            }

            return $"{accountCode}-{roleCode}-{nextSequence:D3}";
        }

        public async Task AssignUserCodesAsync()
        {
            var users = await _context.Users
                .Where(u => u.UserCode == null)
                .Include(u => u.Company)
                .ToListAsync();

            foreach (var user in users)
            {
                user.UserCode = await GenerateUserCodeAsync(user.CompanyId, user.Role);
            }

            await _context.SaveChangesAsync();
        }
    }
}