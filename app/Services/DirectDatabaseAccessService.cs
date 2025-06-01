using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Linq;

namespace app.Services
{
    public class DirectDatabaseAccessService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DirectDatabaseAccessService> _logger;
        
        public DirectDatabaseAccessService(IConfiguration configuration, ILogger<DirectDatabaseAccessService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        // The updated InsertCandidateDirectly method
        public async Task<(bool success, int candidateId)> InsertCandidateDirectly(
            string givenName, string surname, DateTime? birthDate,
            string institution, string fieldOfStudy, string qrcodeLink)
        {
            var connectionString = _configuration.GetConnectionString("SidInBeurzenContext");
            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogError("Connection string is null or empty");
                // Use hardcoded connection string as fallback
                connectionString = "server=localhost;port=3307;database=SID_in_beurzen;user=root;password=root;";
                _logger.LogWarning("Using fallback connection string: {connString}", connectionString);
            }

            using (var connection = new MySqlConnection(connectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    _logger.LogInformation("Direct database connection opened successfully");

                    // Create command for inserting candidate - make sure column names match exactly
                    using (var command = new MySqlCommand())
                    {
                        command.Connection = connection;
                        command.CommandText = @"
                    INSERT INTO sid_in_beurzen.Candidate 
                        (GivenName, Surname, BirthDate, Institution, FieldOfStudy, QRCodeLink) 
                    VALUES 
                        (@GivenName, @Surname, @BirthDate, @Institution, @FieldOfStudy, @QRCodeLink);
                    SELECT LAST_INSERT_ID();";

                        // Make sure we don't pass null values
                        command.Parameters.AddWithValue("@GivenName", givenName ?? "");
                        command.Parameters.AddWithValue("@Surname", surname ?? "");

                        // Format date correctly for MySQL
                        if (birthDate.HasValue)
                            command.Parameters.AddWithValue("@BirthDate", birthDate.Value.ToString("yyyy-MM-dd"));
                        else
                            command.Parameters.AddWithValue("@BirthDate", DBNull.Value);

                        command.Parameters.AddWithValue("@Institution", institution ?? "");
                        command.Parameters.AddWithValue("@FieldOfStudy", fieldOfStudy ?? "");
                        command.Parameters.AddWithValue("@QRCodeLink", qrcodeLink ?? "");

                        // Execute and get the inserted ID
                        var result = await command.ExecuteScalarAsync();
                        if (result != null)
                        {
                            int candidateId = Convert.ToInt32(result);
                            _logger.LogInformation("Direct SQL insertion successful, ID: {ID}", candidateId);
                            return (true, candidateId);
                        }
                    }

                    _logger.LogError("Direct SQL insertion returned null ID");
                    return (false, 0);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in direct SQL insertion: {Message}", ex.Message);

                    // Try again with the simpler approach
                    try
                    {
                        _logger.LogInformation("Attempting simple SQL insert as fallback");

                        using var cmd = new MySqlCommand(
                            "INSERT INTO sid_in_beurzen.Candidate (GivenName, Surname) VALUES (@name, @surname); SELECT LAST_INSERT_ID();",
                            connection);

                        cmd.Parameters.AddWithValue("@name", givenName);
                        cmd.Parameters.AddWithValue("@surname", surname);

                        var simpleResult = await cmd.ExecuteScalarAsync();
                        if (simpleResult != null)
                        {
                            int candidateId = Convert.ToInt32(simpleResult);
                            _logger.LogInformation("Simple SQL insertion successful, ID: {ID}", candidateId);
                            return (true, candidateId);
                        }
                    }
                    catch (Exception innerEx)
                    {
                        _logger.LogError(innerEx, "Simple SQL insertion also failed");
                    }

                    return (false, 0);
                }
            }
        }

        public async Task<(bool success, int registrationId)> InsertRegistrationDirectly(
            int candidateId, int tradeShowId, string notes, List<int> programIds)
        {
            _logger.LogInformation("Attempting direct SQL insertion for registration");

            var connectionString = _configuration.GetConnectionString("SidInBeurzenContext");
            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogError("Connection string is null or empty");
                // Use hardcoded connection string as fallback
                connectionString = "server=localhost;port=3307;database=SID_in_beurzen;user=root;password=root;";
                _logger.LogWarning("Using fallback connection string: {connString}", connectionString);
            }

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            _logger.LogInformation("MySQL connection opened");
            
            using var transaction = await connection.BeginTransactionAsync();
            _logger.LogInformation("Transaction started");
            
            try
            {
                // Insert into Registration table
                string insertRegistrationQuery = @"
                    INSERT INTO sid_in_beurzen.Registration (CandidateId, TradeShowId, Notes, RegisteredAt)
                    VALUES (@CandidateId, @TradeShowId, @Notes, @RegisteredAt);
                    SELECT LAST_INSERT_ID();";
                
                _logger.LogInformation("Executing registration insert query");
                
                // Use parameterized query to prevent SQL injection
                using var command = new MySqlCommand(insertRegistrationQuery, connection, transaction);
                command.Parameters.AddWithValue("@CandidateId", candidateId);
                command.Parameters.AddWithValue("@TradeShowId", tradeShowId);
                command.Parameters.AddWithValue("@Notes", notes ?? "");  // Empty string instead of null
                command.Parameters.AddWithValue("@RegisteredAt", DateTime.Now);
                
                // Execute and get the new registration ID
                var result = await command.ExecuteScalarAsync();
                if (result == null || result == DBNull.Value)
                {
                    _logger.LogError("Registration insert returned null ID");
                    await transaction.RollbackAsync();
                    return (false, 0);
                }
                
                var registrationId = Convert.ToInt32(result);
                _logger.LogInformation("Registration created with ID: {RegistrationId}", registrationId);
                
                // Insert program selections
                if (programIds != null && programIds.Any())
                {
                    _logger.LogInformation("Adding {Count} programs to registration {RegistrationId}", 
                        programIds.Count, registrationId);
                        
                    // Create many-to-many relationships in RegistrationProgram table
                    foreach (var programId in programIds)
                    {
                        string insertProgramQuery = @"
                            INSERT INTO sid_in_beurzen.RegistrationProgram (RegistrationId, ProgramId)
                            VALUES (@RegistrationId, @ProgramId);";
                        
                        using var programCommand = new MySqlCommand(insertProgramQuery, connection, transaction);
                        programCommand.Parameters.AddWithValue("@RegistrationId", registrationId);
                        programCommand.Parameters.AddWithValue("@ProgramId", programId);
                        
                        await programCommand.ExecuteNonQueryAsync();
                        _logger.LogInformation("Added program {ProgramId} to registration", programId);
                    }
                }
                
                await transaction.CommitAsync();
                _logger.LogInformation("Transaction committed successfully");
                return (true, registrationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in direct SQL registration insertion");
                
                try
                {
                    await transaction.RollbackAsync();
                    _logger.LogInformation("Transaction rolled back");
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(rollbackEx, "Error rolling back transaction");
                }
                
                return (false, 0);
            }
        }
    }
}