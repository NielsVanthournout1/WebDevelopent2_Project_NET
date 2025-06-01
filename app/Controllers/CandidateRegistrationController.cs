using Microsoft.AspNetCore.Mvc;
using app.SID_in_beurzen;
using app.Models;
using app.Services; // Add this missing using directive
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using MySql.Data.MySqlClient; // Add this for MySqlConnection

namespace app.Controllers
{
    // Update the route to match the QR code link format
    [Route("registration")]
    public class CandidateRegistrationController : Controller
    {
        private readonly SidInBeurzenContext _context;
        private readonly ILogger<CandidateRegistrationController> _logger;
        private readonly DirectDatabaseAccessService _directDbService;

        public CandidateRegistrationController(
            SidInBeurzenContext context, 
            ILogger<CandidateRegistrationController> logger,
            DirectDatabaseAccessService directDbService)
        {
            _context = context;
            _logger = logger;
            _directDbService = directDbService;
        }

        // This endpoint handles the QR code scan
        [HttpGet("{candidateToken}")]
        public async Task<IActionResult> RegisterInterest(string candidateToken)
        {
            _logger.LogInformation("Received QR code scan with token: {Token}", candidateToken);

            var candidate = await _context.Candidates
                .FirstOrDefaultAsync(c => c.QrcodeLink == candidateToken);

            if (candidate == null)
            {
                _logger.LogWarning("Invalid candidate token: {CandidateToken}", candidateToken);
                return NotFound("Kandidaat niet gevonden");
            }

            _logger.LogInformation("Found candidate with ID: {CandidateId}", candidate.CandidateId);

            var viewModel = new CandidateRegistrationViewModel
            {
                CandidateId = candidate.CandidateId,
                GivenName = candidate.GivenName,
                Surname = candidate.Surname,
                BirthDate = candidate.BirthDate,
                Institution = candidate.Institution,
                FieldOfStudy = candidate.FieldOfStudy,
                AvailablePrograms = await _context.Programs.ToListAsync() as IEnumerable<app.SID_in_beurzen.Program>,
                SelectedProgramIds = new List<int>()
            };

            // Get ALL trade shows, not just future ones
            viewModel.TradeShows = await _context.TradeShows.ToListAsync();

            if (!viewModel.TradeShows.Any())
            {
                _logger.LogWarning("No trade shows found in database. Creating default trade show.");
                
                // Create a default trade show if none exist
                var defaultTradeShow = new TradeShow
                {
                    Title = "Default Tradeshow",
                    EventDate = DateTime.Today.AddDays(30),
                    Venue = "Default Venue"
                };
                
                _context.TradeShows.Add(defaultTradeShow);
                await _context.SaveChangesAsync();
                
                viewModel.TradeShows = new List<TradeShow> { defaultTradeShow };
            }

            // Set a default selected trade show (the first one)
            if (viewModel.TradeShows.Any())
            {
                viewModel.SelectedTradeShowId = viewModel.TradeShows.First().TradeShowId;
            }

            return View(viewModel);
        }

        [HttpPost("{candidateToken}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterInterest(string candidateToken, CandidateRegistrationViewModel model)
        {
            _logger.LogInformation("Processing registration submission for token: {Token}", candidateToken);

            // Dump the entire model to the log for debugging
            _logger.LogInformation("Model dump: CandidateId={CandidateId}, TradeShowId={TradeShowId}, " +
                "Notes='{Notes}', Programs={ProgramCount}", 
                model.CandidateId, model.SelectedTradeShowId, 
                model.Notes ?? "(null)", 
                model.SelectedProgramIds?.Count ?? 0);
            
            // Validate trade show selection
            var tradeShow = await _context.TradeShows.FindAsync(model.SelectedTradeShowId);
            if (tradeShow == null)
            {
                _logger.LogError("Selected trade show ID {TradeShowId} not found", model.SelectedTradeShowId);
                ModelState.AddModelError("SelectedTradeShowId", "De geselecteerde beurs bestaat niet.");
                
                // Re-populate lists for view
                model.AvailablePrograms = await _context.Programs.ToListAsync() as IEnumerable<app.SID_in_beurzen.Program>;
                model.TradeShows = await _context.TradeShows.ToListAsync();
                return View(model);
            }

            // Validate candidate exists
            var candidate = await _context.Candidates.FindAsync(model.CandidateId);
            if (candidate == null)
            {
                _logger.LogError("Candidate ID {CandidateId} not found", model.CandidateId);
                ModelState.AddModelError("", "De geselecteerde studiekiezer bestaat niet.");
                
                // Re-populate lists for view
                model.AvailablePrograms = await _context.Programs.ToListAsync() as IEnumerable<app.SID_in_beurzen.Program>;
                model.TradeShows = await _context.TradeShows.ToListAsync();
                return View(model);
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model validation failed");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _logger.LogWarning("Validation error: {ErrorMessage}", error.ErrorMessage);
                }
                model.AvailablePrograms = await _context.Programs.ToListAsync() as IEnumerable<app.SID_in_beurzen.Program>;
                model.TradeShows = await _context.TradeShows.ToListAsync();
                return View(model);
            }

            // Try using direct SQL first as it's more reliable
            try 
            {
                _logger.LogInformation("Attempting direct SQL insertion for registration");
                
                var directResult = await _directDbService.InsertRegistrationDirectly(
                    model.CandidateId, 
                    model.SelectedTradeShowId, 
                    model.Notes, 
                    model.SelectedProgramIds?.ToList());
                    
                if (directResult.success)
                {
                    _logger.LogInformation("Direct SQL registration succeeded with ID {ID}", directResult.registrationId);
                    return RedirectToAction("RegistrationConfirmed", new { candidateToken });
                }
                else
                {
                    _logger.LogWarning("Direct SQL registration failed, falling back to EF Core");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in direct SQL registration");
                // Continue to EF Core approach
            }

            // Try Entity Framework Core approach as fallback
            try
            {
                _logger.LogInformation("Attempting EF Core approach for registration");
                
                // Reset tracking to avoid conflicts
                _context.ChangeTracker.Clear();
                
                // Create a new registration
                var registration = new Registration
                {
                    CandidateId = candidate.CandidateId,
                    TradeShowId = model.SelectedTradeShowId,
                    Notes = string.IsNullOrWhiteSpace(model.Notes) ? "" : model.Notes, // Ensure Notes is never NULL
                    RegisteredAt = DateTime.Now
                };

                _context.Registrations.Add(registration);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Created registration with ID: {RegistrationId}", registration.RegistrationId);

                // Add selected programs to the registration
                if (model.SelectedProgramIds != null && model.SelectedProgramIds.Any())
                {
                    // Log the selected program IDs
                    _logger.LogInformation("Selected program IDs: {ProgramIds}", 
                        string.Join(", ", model.SelectedProgramIds));
                        
                    // Fetch the programs that were selected
                    var selectedPrograms = await _context.Programs
                        .Where(p => model.SelectedProgramIds.Contains(p.ProgramId))
                        .ToListAsync();
                    
                    _logger.LogInformation("Found {Count} matching programs", selectedPrograms.Count);
                    
                    // Add them to the registration's Programs collection
                    foreach (var program in selectedPrograms)
                    {
                        registration.Programs.Add(program);
                        _logger.LogInformation("Added program {ProgramId}: {ProgramName}", 
                            program.ProgramId, program.ProgramName);
                    }
                    
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Added {Count} programs to registration", selectedPrograms.Count);
                }

                return RedirectToAction("RegistrationConfirmed", new { candidateToken });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating registration with EF Core");
                
                // Try to provide more detailed error information
                if (ex.InnerException != null)
                {
                    _logger.LogError("Inner exception: {Message}", ex.InnerException.Message);
                }
                
                ModelState.AddModelError("", "Er is een fout opgetreden bij het registreren: " + ex.Message);
                
                // Re-populate lists for view
                model.AvailablePrograms = await _context.Programs.ToListAsync() as IEnumerable<app.SID_in_beurzen.Program>;
                model.TradeShows = await _context.TradeShows.ToListAsync();
                return View(model);
            }
        }

        [HttpGet("{candidateToken}/confirmed")]
        public IActionResult RegistrationConfirmed(string candidateToken)
        {
            _logger.LogInformation("Showing registration confirmation for token: {Token}", candidateToken);
            
            // Get the candidate to display info
            var candidate = _context.Candidates
                .FirstOrDefault(c => c.QrcodeLink == candidateToken);
            
            if (candidate != null)
            {
                ViewBag.CandidateId = candidate.CandidateId;
                ViewBag.CandidateName = $"{candidate.GivenName} {candidate.Surname}";
                _logger.LogInformation("Found candidate for confirmation page: {CandidateId}", candidate.CandidateId);
            }
            else
            {
                _logger.LogWarning("Candidate not found for token on confirmation page: {Token}", candidateToken);
            }
            
            return View();
        }

        [HttpGet("scan")]
        public IActionResult ScanQR()
        {
            // Check if user is an exhibitor (role 4)
            var roleString = HttpContext.Session.GetString("GebruikerRol");
            if (roleString != "4") // Beursexposant role
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            
            return View();
        }
    }
}