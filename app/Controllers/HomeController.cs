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
    }
}