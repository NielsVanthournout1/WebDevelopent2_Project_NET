using Microsoft.AspNetCore.Mvc;
using app.SID_in_beurzen;
using app.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Linq;

namespace app.Controllers
{
    [Route("dashboard")]
    public class DashboardController : Controller
    {
        private readonly SidInBeurzenContext _context;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(SidInBeurzenContext context, ILogger<DashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            // Main dashboard page
            return View();
        }

        [HttpGet("program/{id}")]
        public async Task<IActionResult> ProgramDetails(int id)
        {
            var program = await _context.Programs
                .FirstOrDefaultAsync(p => p.ProgramId == id);
            
            if (program == null)
            {
                return NotFound();
            }

            // Get all candidates interested in this program
            var candidates = await _context.Registrations
                .Include(r => r.Candidate)
                .Include(r => r.TradeShow)
                .Include(r => r.Programs)
                .Where(r => r.Programs.Any(p => p.ProgramId == id))
                .Select(r => new ProgramCandidateViewModel
                {
                    RegistrationId = r.RegistrationId,
                    CandidateId = r.Candidate.CandidateId,
                    CandidateName = $"{r.Candidate.GivenName} {r.Candidate.Surname}",
                    Institution = r.Candidate.Institution,
                    FieldOfStudy = r.Candidate.FieldOfStudy,
                    TradeShowName = r.TradeShow.Title,
                    TradeShowDate = r.TradeShow.EventDate,
                    RegisteredAt = r.RegisteredAt,
                    Notes = r.Notes
                })
                .ToListAsync();

            var viewModel = new ProgramDetailsViewModel
            {
                ProgramId = program.ProgramId,
                ProgramName = program.ProgramName,
                Candidates = candidates
            };

            return View(viewModel);
        }

        [HttpGet("statistics")]
        public async Task<IActionResult> ProgramStatistics()
        {
            // Get program registration counts
            var programStats = await _context.Programs
                .Select(p => new ProgramStatisticsViewModel
                {
                    ProgramId = p.ProgramId,
                    ProgramName = p.ProgramName,
                    TotalRegistrations = p.Registrations.Count,
                    TradeShowBreakdown = p.Registrations
                        .GroupBy(r => r.TradeShow)
                        .Select(g => new TradeShowStatViewModel
                        {
                            TradeShowId = g.Key.TradeShowId,
                            TradeShowName = g.Key.Title,
                            Count = g.Count()
                        })
                        .ToList()
                })
                .ToListAsync();

            return View(programStats);
        }

        [HttpGet("candidate/{id}/edit")]
        public async Task<IActionResult> EditCandidate(int id)
        {
            var candidate = await _context.Candidates
                .FirstOrDefaultAsync(c => c.CandidateId == id);

            if (candidate == null)
            {
                return NotFound();
            }

            var viewModel = new CandidateEditViewModel
            {
                CandidateId = candidate.CandidateId,
                GivenName = candidate.GivenName,
                Surname = candidate.Surname,
                BirthDate = candidate.BirthDate,
                Institution = candidate.Institution,
                FieldOfStudy = candidate.FieldOfStudy
            };

            return View(viewModel);
        }

        [HttpPost("candidate/{id}/edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCandidate(int id, CandidateEditViewModel model)
        {
            if (id != model.CandidateId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var candidate = await _context.Candidates.FindAsync(id);
                    if (candidate == null)
                    {
                        return NotFound();
                    }

                    candidate.GivenName = model.GivenName;
                    candidate.Surname = model.Surname;
                    candidate.BirthDate = model.BirthDate;
                    candidate.Institution = model.Institution;
                    candidate.FieldOfStudy = model.FieldOfStudy;

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Candidate {CandidateId} updated", id);
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Candidates.AnyAsync(c => c.CandidateId == id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(model);
        }

        [HttpPost("candidates/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCandidates(int[] candidateIds)
        {
            if (candidateIds == null || !candidateIds.Any())
            {
                return BadRequest();
            }

            var candidates = await _context.Candidates
                .Where(c => candidateIds.Contains(c.CandidateId))
                .ToListAsync();

            if (candidates.Any())
            {
                _context.Candidates.RemoveRange(candidates);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted {Count} candidates", candidates.Count);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}