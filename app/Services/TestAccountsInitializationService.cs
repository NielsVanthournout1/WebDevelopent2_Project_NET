using app.SID_in_beurzen;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace app.Services
{
    public class TestAccountsInitializationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TestAccountsInitializationService> _logger;

        public TestAccountsInitializationService(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<TestAccountsInitializationService> logger)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task InitializeTestAccounts()
        {
            // Create a new scope to retrieve scoped services
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SidInBeurzenContext>();

            try
            {
                // Create test accounts for all roles
                await CreateTestAccountsIfNotExist(dbContext);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while initializing test accounts");
            }
        }

        private async Task CreateTestAccountsIfNotExist(SidInBeurzenContext dbContext)
        {
            // Default password for all test accounts
            var defaultPassword = _configuration["TestAccounts:DefaultPassword"] ?? "Test123";
            
            // Define test accounts for each role
            var testAccounts = new List<(int RoleTypeId, string Email, string Password, string FirstName, string LastName)>
            {
                // Marketing Team Lead
                (
                    2, 
                    _configuration["TestAccounts:TeamLead:Email"] ?? "lead@test.com", 
                    defaultPassword, 
                    _configuration["TestAccounts:TeamLead:FirstName"] ?? "Test", 
                    _configuration["TestAccounts:TeamLead:LastName"] ?? "TeamLead"
                ),
                
                // Marketing Medewerker
                (
                    3, 
                    _configuration["TestAccounts:Marketing:Email"] ?? "marketing@test.com", 
                    defaultPassword,
                    _configuration["TestAccounts:Marketing:FirstName"] ?? "Test", 
                    _configuration["TestAccounts:Marketing:LastName"] ?? "Marketing"
                ),
                
                // Beursexposant
                (
                    4, 
                    _configuration["TestAccounts:Exposant:Email"] ?? "exposant@test.com", 
                    defaultPassword,
                    _configuration["TestAccounts:Exposant:FirstName"] ?? "Test", 
                    _configuration["TestAccounts:Exposant:LastName"] ?? "Exposant"
                )
            };

            foreach (var account in testAccounts)
            {
                // Check if this test account already exists
                var accountExists = await dbContext.Accounts
                    .AnyAsync(a => a.EmailAddress == account.Email && a.RoleTypeId == account.RoleTypeId);

                if (!accountExists)
                {
                    _logger.LogInformation("Creating test account with email: {Email}, role: {RoleId}", 
                        account.Email, account.RoleTypeId);

                    // Hash the password
                    string hashedPassword = HashPassword(account.Password);

                    // Create the test account
                    dbContext.Accounts.Add(new Account
                    {
                        FirstName = account.FirstName,
                        LastName = account.LastName,
                        EmailAddress = account.Email,
                        PasswordHash = hashedPassword,
                        RoleTypeId = account.RoleTypeId,
                        IsActive = true
                    });

                    await dbContext.SaveChangesAsync();
                    _logger.LogInformation("Test account created successfully: {Email}", account.Email);
                }
                else
                {
                    _logger.LogInformation("Test account already exists: {Email}", account.Email);
                }
            }
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}