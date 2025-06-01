using Microsoft.AspNetCore.Mvc;
using app.SID_in_beurzen;
using Microsoft.EntityFrameworkCore;
using app.Models;

namespace app.Controllers
{
    public class HomeController : Controller
    {
        private readonly SidInBeurzenContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(SidInBeurzenContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index()
        {
            var viewModel = new HomeViewModel();

            try
            {
                // Test database connection
                var canConnect = _context.Database.CanConnect();
                viewModel.DatabaseConnected = canConnect;

                if (canConnect)
                {
                    viewModel.DatabaseMessage = "Database connection successful";

                    // Count some records to verify data access
                    viewModel.AccountCount = _context.Accounts.Count();
                    viewModel.RoleCount = _context.RoleTypes.Count();
                }
                else
                {
                    viewModel.DatabaseMessage = "Failed to connect to database";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking database connection");
                viewModel.DatabaseConnected = false;
                viewModel.DatabaseMessage = $"Database error: {ex.Message}";
            }

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [Route("error")]
        public IActionResult Error()
        {
            return View();
        }

        public IActionResult Api()
        {
            var model = new ApiInfoViewModel
            {
                ApiEndpoints = new List<ApiEndpointInfo>
                {
                    new ApiEndpointInfo
                    {
                        Name = "Most chosen program (across all trade shows)",
                        Url = Url.Action("GetMostPopularProgram", "Statistics", new { area = "Api" }, Request.Scheme),
                        HttpMethod = "GET",
                        Description = "Returns the program that has been selected the most times across all trade shows"
                    },
                    new ApiEndpointInfo
                    {
                        Name = "Least chosen program (across all trade shows)",
                        Url = Url.Action("GetLeastPopularProgram", "Statistics", new { area = "Api" }, Request.Scheme),
                        HttpMethod = "GET",
                        Description = "Returns the program that has been selected the least times across all trade shows"
                    },
                    new ApiEndpointInfo
                    {
                        Name = "Total candidates for a trade show",
                        Url = "/api/statistics/tradeshow/{id}/total-candidates",
                        HttpMethod = "GET",
                        Description = "Returns the total number of unique candidates registered for a specific trade show"
                    },
                    new ApiEndpointInfo
                    {
                        Name = "Most chosen program for a trade show",
                        Url = "/api/statistics/tradeshow/{id}/most-popular-program",
                        HttpMethod = "GET",
                        Description = "Returns the most popular program for a specific trade show"
                    },
                    new ApiEndpointInfo
                    {
                        Name = "Least chosen program for a trade show",
                        Url = "/api/statistics/tradeshow/{id}/least-popular-program",
                        HttpMethod = "GET",
                        Description = "Returns the least popular program for a specific trade show"
                    },
                    new ApiEndpointInfo
                    {
                        Name = "All statistics for a trade show",
                        Url = "/api/statistics/tradeshow/{id}/all",
                        HttpMethod = "GET",
                        Description = "Returns all statistics (most and least popular programs, total candidates) for a specific trade show"
                    }
                },
                ApiDocumentation = $"{Request.Scheme}://{Request.Host}/api/docs"
            };
            
            return View(model);
        }
    }
}