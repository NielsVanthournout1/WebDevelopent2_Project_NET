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
    public class AdminInitializationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AdminInitializationService> _logger;

        public AdminInitializationService(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<AdminInitializationService> logger)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task InitializeAdminAccount()
        {
            // Create a new scope to retrieve scoped services
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SidInBeurzenContext>();

            try
            {
                // First ensure all required roles exist
                await InitializeRoles(dbContext);

                // Then create admin account if it doesn't exist
                await CreateAdminAccountIfNotExists(dbContext);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while initializing the system");
            }
        }

        private async Task InitializeRoles(SidInBeurzenContext dbContext)
        {
            _logger.LogInformation("Checking and creating required roles");

            // Define the required roles with their IDs
            var requiredRoles = new List<RoleType>
            {
                new RoleType { RoleTypeId = 1, RoleName = "Administrator" },
                new RoleType { RoleTypeId = 2, RoleName = "Marketing Team Lead" },
                new RoleType { RoleTypeId = 3, RoleName = "Marketing Medewerker" },
                new RoleType { RoleTypeId = 4, RoleName = "Beursexposant" }
            };

            foreach (var role in requiredRoles)
            {
                var roleExists = await dbContext.RoleTypes
                    .AnyAsync(r => r.RoleTypeId == role.RoleTypeId);

                if (!roleExists)
                {
                    _logger.LogInformation("Creating role: {RoleName}", role.RoleName);
                    dbContext.RoleTypes.Add(role);
                }
            }

            // Save any new roles
            if (dbContext.ChangeTracker.HasChanges())
            {
                await dbContext.SaveChangesAsync();
                _logger.LogInformation("Roles created successfully");
            }
            else
            {
                _logger.LogInformation("All required roles already exist");
            }
        }

        private async Task CreateAdminAccountIfNotExists(SidInBeurzenContext dbContext)
        {
            // Check if any admin account exists
            var adminExists = await dbContext.Accounts
                .AnyAsync(a => a.RoleTypeId == 1 && a.IsActive == true);

            if (!adminExists)
            {
                var adminEmail = _configuration["AdminAccount:Email"] ?? "admin@admin.com";
                var adminPassword = _configuration["AdminAccount:Password"] ?? "Admin";
                var adminFirstName = _configuration["AdminAccount:FirstName"] ?? "Admin";
                var adminLastName = _configuration["AdminAccount:LastName"] ?? "User";

                _logger.LogInformation("Creating admin account with email: {Email}", adminEmail);

                // Hash the password
                string hashedPassword = HashPassword(adminPassword);

                // Create the admin account
                dbContext.Accounts.Add(new Account
                {
                    FirstName = adminFirstName,
                    LastName = adminLastName,
                    EmailAddress = adminEmail,
                    PasswordHash = hashedPassword,
                    RoleTypeId = 1,
                    IsActive = true
                });

                await dbContext.SaveChangesAsync();
                _logger.LogInformation("Admin account created successfully");
            }
            else
            {
                _logger.LogInformation("Admin account already exists");
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