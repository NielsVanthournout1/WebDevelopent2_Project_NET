using app.SID_in_beurzen;
using app.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Setup logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Get connection string from configuration
var connectionString = builder.Configuration.GetConnectionString("SidInBeurzenContext");

// Register the DbContext
builder.Services.AddDbContext<SidInBeurzenContext>(options =>
{
    options.UseMySQL(connectionString ??
        "server=localhost;port=3307;database=SID_in_beurzen;user=root;password=root;");
    
    // Add logging to the database context
    options.EnableSensitiveDataLogging(); // Log parameter values
    options.LogTo(message => Console.WriteLine($"EF Core: {message}"), 
        LogLevel.Information);
});

// Register services
builder.Services.AddScoped<AdminInitializationService>();
builder.Services.AddScoped<TestAccountsInitializationService>();
builder.Services.AddScoped<DataSeedService>(); // Add the data seeding service
builder.Services.AddScoped<DatabaseFixService>();
builder.Services.AddScoped<DirectDatabaseAccessService>();
builder.Services.AddScoped<app.Services.DatabaseFixService>();
builder.Services.AddScoped<app.Services.DirectDatabaseAccessService>();

// Basic services
builder.Services.AddControllersWithViews();
builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();

// Add Swagger for API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SID in Beurzen - Statistics API",
        Version = "v1",
        Description = "Public API providing statistics about programs and trade shows",
        Contact = new OpenApiContact
        {
            Name = "SID in Beurzen",
            Email = "info@sidinbeurzen.be"
        }
    });
    
    // Use XML comments for documentation (optional - enable if you want to add XML comments)
    // var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    // var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    // c.IncludeXmlComments(xmlPath);
});

// Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
    });

var app = builder.Build();

// Configure the application
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    
    // Enable Swagger UI in development
    app.UseSwagger();
    app.UseSwaggerUI(c => 
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SID in Beurzen API v1");
        c.RoutePrefix = "api/docs"; // Access at /api/docs
    });
}
else
{
    app.UseExceptionHandler("/Home/Error");
    
    // Still enable Swagger in production, but at a different URL
    app.UseSwagger();
    app.UseSwaggerUI(c => 
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SID in Beurzen API v1");
        c.RoutePrefix = "api/docs"; // Access at /api/docs
    });
}

// Basic middleware
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();

if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
        context.Response.Headers["Pragma"] = "no-cache";
        context.Response.Headers["Expires"] = "0";
        await next();
    });
}

// Add this before app.UseRouting()
if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Request: {Method} {Path}", context.Request.Method, context.Request.Path);
        await next();
        logger.LogInformation("Response: {StatusCode} for {Path}", context.Response.StatusCode, context.Request.Path);
    });
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Map routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Add this custom route for our registration page with tokens
app.MapControllerRoute(
    name: "candidateRegistration",
    pattern: "registration/{candidateToken}",
    defaults: new { controller = "CandidateRegistration", action = "RegisterInterest" });

app.MapControllerRoute(
    name: "candidateRegistrationConfirmed",
    pattern: "registration/{candidateToken}/confirmed",
    defaults: new { controller = "CandidateRegistration", action = "RegistrationConfirmed" });

app.MapControllerRoute(
    name: "candidateRegistrationScan",
    pattern: "CandidateRegistration/ScanQR",
    defaults: new { controller = "CandidateRegistration", action = "ScanQR" });

// Initialize admin account at startup
using (var scope = app.Services.CreateScope())
{
    var adminInitService = scope.ServiceProvider.GetRequiredService<AdminInitializationService>();
    await adminInitService.InitializeAdminAccount();
    
    // Initialize test accounts (only in development environment)
    if (app.Environment.IsDevelopment())
    {
        var testAccountsService = scope.ServiceProvider.GetRequiredService<TestAccountsInitializationService>();
        await testAccountsService.InitializeTestAccounts();
        
        // Seed sample data
        var dataSeedService = scope.ServiceProvider.GetRequiredService<DataSeedService>();
        await dataSeedService.SeedSampleData();
    }
}

// Add this before app.Run() to test the database connection
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/testdb")
    {
        try
        {
            var dbContext = context.RequestServices.GetRequiredService<SidInBeurzenContext>();
            var canConnect = await dbContext.Database.CanConnectAsync();
            var roleCount = await dbContext.RoleTypes.CountAsync();
            
            await context.Response.WriteAsync($"Database connection test: {(canConnect ? "SUCCESS" : "FAILED")}\n");
            await context.Response.WriteAsync($"Number of roles in database: {roleCount}\n");
            
            return;
        }
        catch (Exception ex)
        {
            await context.Response.WriteAsync($"Database connection error: {ex.Message}\n");
            return;
        }
    }
    
    await next();
});

app.Run();