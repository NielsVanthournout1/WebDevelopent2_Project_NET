using Microsoft.AspNetCore.Mvc;
using app.SID_in_beurzen;
using app.Models.Api;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;

namespace app.Controllers.Api
{
    [Route("api/statistics")]
    [ApiController]
    public class StatisticsController : ControllerBase
    {
        private readonly SidInBeurzenContext _context;
        private readonly ILogger<StatisticsController> _logger;

        public StatisticsController(SidInBeurzenContext context, ILogger<StatisticsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get the most popular (most chosen) program across all trade shows
        /// </summary>
        [HttpGet("most-popular-program")]
        [ProducesResponseType(200, Type = typeof(ProgramStatisticsDto))]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ProgramStatisticsDto>> GetMostPopularProgram()
        {
            var program = await _context.Programs
                .Select(p => new
                {
                    Program = p,
                    Count = p.Registrations.Count
                })
                .OrderByDescending(x => x.Count)
                .FirstOrDefaultAsync();

            if (program == null)
            {
                return NotFound("No programs found");
            }

            return new ProgramStatisticsDto
            {
                ProgramId = program.Program.ProgramId,
                ProgramName = program.Program.ProgramName,
                RegistrationCount = program.Count
            };
        }

        /// <summary>
        /// Get the least popular (least chosen) program across all trade shows
        /// </summary>
        [HttpGet("least-popular-program")]
        [ProducesResponseType(200, Type = typeof(ProgramStatisticsDto))]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ProgramStatisticsDto>> GetLeastPopularProgram()
        {
            var program = await _context.Programs
                .Select(p => new
                {
                    Program = p,
                    Count = p.Registrations.Count
                })
                .OrderBy(x => x.Count)
                .FirstOrDefaultAsync();

            if (program == null)
            {
                return NotFound("No programs found");
            }

            return new ProgramStatisticsDto
            {
                ProgramId = program.Program.ProgramId,
                ProgramName = program.Program.ProgramName,
                RegistrationCount = program.Count
            };
        }

        /// <summary>
        /// Get the total number of unique candidates registered for a specific trade show
        /// </summary>
        /// <param name="id">The trade show ID</param>
        [HttpGet("tradeshow/{id}/total-candidates")]
        [ProducesResponseType(200, Type = typeof(TradeShowStatisticsDto))]
        [ProducesResponseType(404)]
        public async Task<ActionResult<TradeShowStatisticsDto>> GetTradeShowCandidateCount(int id)
        {
            var tradeShow = await _context.TradeShows
                .Where(ts => ts.TradeShowId == id)
                .Select(ts => new
                {
                    TradeShow = ts,
                    Count = ts.Registrations
                        .Select(r => r.CandidateId)
                        .Distinct()
                        .Count()
                })
                .FirstOrDefaultAsync();

            if (tradeShow == null)
            {
                return NotFound("Trade show not found");
            }

            return new TradeShowStatisticsDto
            {
                TradeShowId = tradeShow.TradeShow.TradeShowId,
                TradeShowName = tradeShow.TradeShow.Title,
                CandidateCount = tradeShow.Count,
                EventDate = tradeShow.TradeShow.EventDate
            };
        }

        /// <summary>
        /// Get most popular program for a specific trade show
        /// </summary>
        /// <param name="id">The trade show ID</param>
        [HttpGet("tradeshow/{id}/most-popular-program")]
        [ProducesResponseType(200, Type = typeof(ProgramStatisticsDto))]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ProgramStatisticsDto>> GetMostPopularProgramForTradeShow(int id)
        {
            // First check if the trade show exists
            var tradeShowExists = await _context.TradeShows.AnyAsync(ts => ts.TradeShowId == id);
            if (!tradeShowExists)
            {
                return NotFound("Trade show not found");
            }

            // Get registrations for this trade show
            var programCounts = await _context.Programs
                .Select(p => new
                {
                    Program = p,
                    Count = p.Registrations.Count(r => r.TradeShowId == id)
                })
                .OrderByDescending(x => x.Count)
                .FirstOrDefaultAsync();

            if (programCounts == null || programCounts.Count == 0)
            {
                return NotFound("No program registrations found for this trade show");
            }

            return new ProgramStatisticsDto
            {
                ProgramId = programCounts.Program.ProgramId,
                ProgramName = programCounts.Program.ProgramName,
                RegistrationCount = programCounts.Count
            };
        }

        /// <summary>
        /// Get least popular program for a specific trade show
        /// </summary>
        /// <param name="id">The trade show ID</param>
        [HttpGet("tradeshow/{id}/least-popular-program")]
        [ProducesResponseType(200, Type = typeof(ProgramStatisticsDto))]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ProgramStatisticsDto>> GetLeastPopularProgramForTradeShow(int id)
        {
            // First check if the trade show exists
            var tradeShowExists = await _context.TradeShows.AnyAsync(ts => ts.TradeShowId == id);
            if (!tradeShowExists)
            {
                return NotFound("Trade show not found");
            }

            // Get registrations for this trade show with at least 1 registration
            var programCounts = await _context.Programs
                .Select(p => new
                {
                    Program = p,
                    Count = p.Registrations.Count(r => r.TradeShowId == id)
                })
                .Where(x => x.Count > 0)
                .OrderBy(x => x.Count)
                .FirstOrDefaultAsync();

            if (programCounts == null)
            {
                return NotFound("No program registrations found for this trade show");
            }

            return new ProgramStatisticsDto
            {
                ProgramId = programCounts.Program.ProgramId,
                ProgramName = programCounts.Program.ProgramName,
                RegistrationCount = programCounts.Count
            };
        }

        /// <summary>
        /// Get all statistics for a specific trade show in a single request
        /// </summary>
        /// <param name="id">The trade show ID</param>
        [HttpGet("tradeshow/{id}/all")]
        [ProducesResponseType(200, Type = typeof(TradeShowAllStatisticsDto))]
        [ProducesResponseType(404)]
        public async Task<ActionResult<TradeShowAllStatisticsDto>> GetAllStatisticsForTradeShow(int id)
        {
            // Check if trade show exists
            var tradeShow = await _context.TradeShows
                .FirstOrDefaultAsync(ts => ts.TradeShowId == id);

            if (tradeShow == null)
            {
                return NotFound("Trade show not found");
            }

            // Get the total number of unique candidates
            var candidateCount = await _context.Registrations
                .Where(r => r.TradeShowId == id)
                .Select(r => r.CandidateId)
                .Distinct()
                .CountAsync();

            // Get program statistics
            var programCounts = await _context.Programs
                .Select(p => new
                {
                    Program = p,
                    Count = p.Registrations.Count(r => r.TradeShowId == id)
                })
                .Where(x => x.Count > 0)
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            if (!programCounts.Any())
            {
                return new TradeShowAllStatisticsDto
                {
                    TradeShowId = tradeShow.TradeShowId,
                    TradeShowName = tradeShow.Title,
                    EventDate = tradeShow.EventDate,
                    TotalCandidateCount = candidateCount,
                    MostPopularProgram = null,
                    LeastPopularProgram = null
                };
            }

            var mostPopular = programCounts.First();
            var leastPopular = programCounts.Last();

            return new TradeShowAllStatisticsDto
            {
                TradeShowId = tradeShow.TradeShowId,
                TradeShowName = tradeShow.Title,
                EventDate = tradeShow.EventDate,
                TotalCandidateCount = candidateCount,
                MostPopularProgram = new ProgramStatisticsDto
                {
                    ProgramId = mostPopular.Program.ProgramId,
                    ProgramName = mostPopular.Program.ProgramName,
                    RegistrationCount = mostPopular.Count
                },
                LeastPopularProgram = new ProgramStatisticsDto
                {
                    ProgramId = leastPopular.Program.ProgramId,
                    ProgramName = leastPopular.Program.ProgramName,
                    RegistrationCount = leastPopular.Count
                }
            };
        }

        /// <summary>
        /// Get all registrations for a specific candidate
        /// </summary>
        /// <param name="id">The candidate ID</param>
        [HttpGet("candidate/{id}/registrations")]
        [ProducesResponseType(200, Type = typeof(List<CandidateRegistrationHistoryDto>))]
        [ProducesResponseType(404)]
        public async Task<ActionResult<List<CandidateRegistrationHistoryDto>>> GetCandidateRegistrations(int id)
        {
            // Check if candidate exists
            var candidateExists = await _context.Candidates.AnyAsync(c => c.CandidateId == id);
            if (!candidateExists)
            {
                return NotFound("Candidate not found");
            }

            // Get all registrations for this candidate
            var registrations = await _context.Registrations
                .Include(r => r.TradeShow)
                .Include(r => r.Programs)
                .Where(r => r.CandidateId == id)
                .Select(r => new CandidateRegistrationHistoryDto
                {
                    RegistrationId = r.RegistrationId,
                    TradeShowId = r.TradeShowId.Value,
                    TradeShowName = r.TradeShow.Title,
                    RegisteredAt = r.RegisteredAt,
                    Notes = r.Notes,
                    Programs = r.Programs.Select(p => p.ProgramName).ToList()
                })
                .ToListAsync();

            return registrations;
        }

        /// <summary>
        /// Get all programs
        /// </summary>
        [HttpGet("programs")]
        [ProducesResponseType(200, Type = typeof(List<ProgramBasicDto>))]
        public async Task<ActionResult<List<ProgramBasicDto>>> GetAllPrograms()
        {
            var programs = await _context.Programs
                .Select(p => new ProgramBasicDto
                {
                    ProgramId = p.ProgramId,
                    ProgramName = p.ProgramName
                })
                .ToListAsync();

            return programs;
        }
    }
}