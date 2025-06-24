using Microsoft.AspNetCore.Mvc;
using StackFlow.Data;
using StackFlow.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using BCrypt.Net; // For password hashing

namespace StackFlow.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewData["LoginError"] = "Email and password are required.";
                return View();
            }

            // Find user by email
            // Include Role to get the role name for claims
            var user = await _context.User
                                     .Include(u => u.Role)
                                     .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                ViewData["LoginError"] = "Invalid email or password.";
                return View();
            }

            // Create claims for the user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name), 
                new Claim(ClaimTypes.Email, user.Email)
            };

            // Add role claim if user has a role
            if (user.Role != null)
            {
                claims.Add(new Claim(ClaimTypes.Role, user.Role.Name)); // Using Name property from Role model
            }

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true, // For "Remember me" functionality
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30) // Set cookie expiration
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            return RedirectToLocal(returnUrl);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string username, string email, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewData["RegistrationError"] = "Username, email, and password are required.";
                return View();
            }

            // Check if email already exists
            if (await _context.User.AnyAsync(u => u.Email == email))
            {
                ViewData["RegistrationError"] = "Email is already registered.";
                return View();
            }

            // Hash password
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            // Assign default role (e.g., 'Developer')
            // Querying by Role.Name property as per your model
            var defaultRole = await _context.Role.FirstOrDefaultAsync(r => r.Name == "Developer");
            if (defaultRole == null)
            {
                // Fallback if 'Developer' role isn't seeded, or create it if not found
                // For a robust system, ensure roles are seeded during app startup/migration
                ViewData["RegistrationError"] = "Default role 'Developer' not found. Please contact support.";
                return View();
            }

            var newUser = new User
            {
                Name = username, // Changed from Username to Name to match User.cs model
                Email = email,
                PasswordHash = passwordHash,
                RoleId = defaultRole.Id, // Assign the ID of the 'Developer' role
                CreatedAt = DateTime.UtcNow
            };

            _context.User.Add(newUser);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Registration successful! You can now log in.";
            return RedirectToAction("Login");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }
    }
}