using System;
using System.Threading.Tasks;
using app.SID_in_beurzen;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace app.Services
{
    public class DatabaseFixService
    {
        private readonly SidInBeurzenContext _context;
        private readonly ILogger<DatabaseFixService> _logger;

        public DatabaseFixService(SidInBeurzenContext context, ILogger<DatabaseFixService> logger)
        {
            _context = context;
            _logger = logger;
        }
        
        public async Task<bool> EnsureDatabaseConfigured()
        {
            try
            {
                // Test if we can connect to the database
                var canConnect = await _context.Database.CanConnectAsync();
                if (!canConnect)
                {
                    _logger.LogError("Cannot connect to database!");
                    return false;
                }
                
                _logger.LogInformation("Database connection successful");
                
                // Test if we can access the Candidates table
                var candidateCount = await _context.Candidates.CountAsync();
                _logger.LogInformation("Current candidate count: {Count}", candidateCount);
                
                // All tests passed
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database configuration test failed");
                return false;
            }
        }
    }
}