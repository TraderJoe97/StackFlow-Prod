using Microsoft.EntityFrameworkCore;
using StackFlow.Data;
using System;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication.Cookies; // Added for cookie authentication

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure DbContext with SQL Server, retry logic, and command timeout
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
            sqlOptions.CommandTimeout(60);
        });
});
// --- START LOGGING CONFIGURATION ---

// Clear default logging providers (optional, but gives you full control)
builder.Logging.ClearProviders();

// Add Console logging provider: Logs to the console window (e.g., in VS Output window, or command line)
builder.Logging.AddConsole();

// Add Debug logging provider: Logs to the debug output window (e.g., VS Output window - Debug)
builder.Logging.AddDebug();

// You can also specify minimum log levels for different providers or categories
builder.Logging.AddConsole(options =>
{
    options.IncludeScopes = true; // Include scope information (e.g., HTTP request path)
    options.TimestampFormat = "HH:mm:ss "; // Custom timestamp format
});

// Configure logging filters to control what gets logged
builder.Logging.AddFilter("Microsoft", LogLevel.Warning) // Log Warnings and above for Microsoft namespaces
               .AddFilter("System", LogLevel.Warning)     // Log Warnings and above for System namespaces
               .AddFilter("StackFlow", LogLevel.Information); // Log Information and above for your application's namespaces (e.g., Controllers, Services)

// --- END LOGGING CONFIGURATION ---


// Configure Authentication services (specifically Cookie Authentication)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; // Path to your login action
        options.LogoutPath = "/Account/Logout"; // Path to your logout action
        options.AccessDeniedPath = "/Account/AccessDenied"; // Path for access denied
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // Cookie expiration time
        options.SlidingExpiration = true; // Renew cookie if half of its lifetime has passed
    });

// Add Authorization services
builder.Services.AddAuthorization();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// IMPORTANT: UseAuthentication must be placed before UseAuthorization
// This ensures that the user's identity is established before authorization checks are performed.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

//Remote repo
//https://github.com/TraderJoe97/StackFlow-Prod.git

//Local repo
//StackFlow-Prod