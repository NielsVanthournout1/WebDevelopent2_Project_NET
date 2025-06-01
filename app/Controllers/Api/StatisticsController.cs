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

        [HttpGet("most-popular-program")]
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

        [HttpGet("least-popular-program")]
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

        [HttpGet("tradeshow/{id}/total-candidates")]
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
    }
}