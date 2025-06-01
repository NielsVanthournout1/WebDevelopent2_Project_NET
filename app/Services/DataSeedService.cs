using app.SID_in_beurzen;
using Microsoft.EntityFrameworkCore;

namespace app.Services
{
    public class DataSeedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DataSeedService> _logger;

        public DataSeedService(
            IServiceProvider serviceProvider,
            ILogger<DataSeedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task SeedSampleData()
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SidInBeurzenContext>();

            try
            {
                // First check if there are any trade shows and seed them if needed
                await SeedTradeShows(dbContext);
                
                // Then seed the rest of the data
                await SeedPrograms(dbContext);
                await SeedCandidates(dbContext);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding sample data");
            }
        }

        private async Task SeedPrograms(SidInBeurzenContext dbContext)
        {
            if (!await dbContext.Programs.AnyAsync())
            {
                _logger.LogInformation("Seeding programs (opleidingen)");

                var programs = new List<SID_in_beurzen.Program>
                {
                    new SID_in_beurzen.Program { ProgramName = "Informatica" },
                    new SID_in_beurzen.Program { ProgramName = "Elektromechanica" },
                    new SID_in_beurzen.Program { ProgramName = "Verpleegkunde" },
                    new SID_in_beurzen.Program { ProgramName = "Bedrijfsmanagement" },
                    new SID_in_beurzen.Program { ProgramName = "Architectuur" },
                    new SID_in_beurzen.Program { ProgramName = "Communicatie" },
                    new SID_in_beurzen.Program { ProgramName = "Rechten" },
                    new SID_in_beurzen.Program { ProgramName = "Geneeskunde" }
                };

                await dbContext.Programs.AddRangeAsync(programs);
                await dbContext.SaveChangesAsync();
                _logger.LogInformation("Successfully seeded {Count} programs", programs.Count);
            }
            else
            {
                _logger.LogInformation("Programs already exist, skipping seed");
            }
        }

        private async Task SeedTradeShows(SidInBeurzenContext dbContext)
        {
            if (!await dbContext.TradeShows.AnyAsync())
            {
                _logger.LogInformation("Seeding trade shows (beurzen)");

                var now = DateTime.Now;
                var tradeShows = new List<TradeShow>
                {
                    new TradeShow 
                    { 
                        Title = "Sid-in Antwerpen", 
                        EventDate = new DateTime(now.Year + 1, 2, 15), 
                        Venue = "Antwerp Expo" 
                    },
                    new TradeShow 
                    { 
                        Title = "Sid-in Gent", 
                        EventDate = new DateTime(now.Year + 1, 3, 10), 
                        Venue = "Flanders Expo" 
                    },
                    new TradeShow 
                    { 
                        Title = "Sid-in Brussel", 
                        EventDate = new DateTime(now.Year + 1, 4, 5), 
                        Venue = "Brussels Expo" 
                    }
                };

                await dbContext.TradeShows.AddRangeAsync(tradeShows);
                await dbContext.SaveChangesAsync();
                _logger.LogInformation("Successfully seeded {Count} trade shows", tradeShows.Count);
            }
            else
            {
                _logger.LogInformation("Trade shows already exist, skipping seed");
            }
        }

        private async Task SeedCandidates(SidInBeurzenContext dbContext)
        {
            if (!await dbContext.Candidates.AnyAsync())
            {
                _logger.LogInformation("Seeding example candidates");

                var candidates = new List<Candidate>
                {
                    new Candidate
                    {
                        GivenName = "Voorbeeld",
                        Surname = "Student",
                        BirthDate = new DateTime(2006, 5, 20),
                        Institution = "Sint-Jozefscollege Aalst",
                        FieldOfStudy = "Wetenschappen-Wiskunde",
                        QrcodeLink = "example123"
                    },
                    new Candidate
                    {
                        GivenName = "Jan",
                        Surname = "Janssens",
                        BirthDate = new DateTime(2005, 8, 12),
                        Institution = "Atheneum Gentbrugge",
                        FieldOfStudy = "Economie-Moderne Talen",
                        QrcodeLink = "jan123456"
                    },
                    new Candidate
                    {
                        GivenName = "Emma",
                        Surname = "Peeters",
                        BirthDate = new DateTime(2006, 3, 17),
                        Institution = "Sint-Lodewijkscollege Brugge",
                        FieldOfStudy = "Latijn-Wetenschappen",
                        QrcodeLink = "emma12345"
                    }
                };

                await dbContext.Candidates.AddRangeAsync(candidates);
                await dbContext.SaveChangesAsync();
                _logger.LogInformation("Successfully seeded {Count} candidates", candidates.Count);
            }
            else
            {
                _logger.LogInformation("Candidates already exist, skipping seed");
            }
        }
    }
}