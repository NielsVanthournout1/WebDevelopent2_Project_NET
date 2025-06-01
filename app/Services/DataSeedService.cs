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

                // Add these calls with the dbContext parameter
                await SeedSampleRegistrations(dbContext);
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

                var programs = new List<app.SID_in_beurzen.Program>
                {
                    new app.SID_in_beurzen.Program { ProgramName = "Informatica" },
                    new app.SID_in_beurzen.Program { ProgramName = "Elektromechanica" },
                    new app.SID_in_beurzen.Program { ProgramName = "Verpleegkunde" },
                    new app.SID_in_beurzen.Program { ProgramName = "Bedrijfsmanagement" },
                    new app.SID_in_beurzen.Program { ProgramName = "Architectuur" },
                    new app.SID_in_beurzen.Program { ProgramName = "Communicatie" },
                    new app.SID_in_beurzen.Program { ProgramName = "Rechten" },
                    new app.SID_in_beurzen.Program { ProgramName = "Geneeskunde" }
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

        // Add sample registrations to generate statistics
        private async Task SeedSampleRegistrations(SidInBeurzenContext dbContext)
        {
            if (!await dbContext.Registrations.AnyAsync())
            {
                _logger.LogInformation("Seeding sample registrations for statistics");

                // Get existing candidates, programs, and trade shows
                var candidates = await dbContext.Candidates.ToListAsync();
                var programs = await dbContext.Programs.ToListAsync();
                var tradeShows = await dbContext.TradeShows.ToListAsync();

                if (candidates.Count == 0 || programs.Count == 0 || tradeShows.Count == 0)
                {
                    _logger.LogWarning("Cannot seed registrations: missing candidates, programs, or trade shows");
                    return;
                }

                // Create a list to hold all registration objects
                var registrations = new List<Registration>();
                var random = new Random();

                // For each candidate, create registrations for each trade show with different program interests
                foreach (var candidate in candidates)
                {
                    foreach (var tradeShow in tradeShows)
                    {
                        // 70% chance to register for a trade show
                        if (random.NextDouble() < 0.7)
                        {
                            var registration = new Registration
                            {
                                CandidateId = candidate.CandidateId,
                                TradeShowId = tradeShow.TradeShowId,
                                RegisteredAt = DateTime.Now.AddDays(-random.Next(1, 30)),
                                Notes = $"Sample registration for {candidate.GivenName} at {tradeShow.Title}"
                            };

                            // Add 1-5 random programs
                            var shuffledPrograms = programs.OrderBy(x => random.Next()).Take(random.Next(1, 6)).ToList();
                            foreach (var program in shuffledPrograms)
                            {
                                registration.Programs.Add(program);
                            }

                            registrations.Add(registration);
                        }
                    }
                }

                // Add all registrations to the database
                await dbContext.Registrations.AddRangeAsync(registrations);
                await dbContext.SaveChangesAsync();
                _logger.LogInformation("Successfully seeded {Count} registrations", registrations.Count);
            }
            else
            {
                _logger.LogInformation("Registrations already exist, skipping seed");
            }
        }
    }
}