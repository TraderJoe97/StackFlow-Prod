using Microsoft.AspNetCore.Mvc;
using StackFlow.Data;
using StackFlow.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
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
            if (!email.EndsWith("@omnitak.com", StringComparison.OrdinalIgnoreCase))
            {
                ViewData["LoginError"] = "Invalid email or password.";
                return View();
            }

            // Find user by email
            // Include Role to get the role name for claims
            var user = await _context.User
                                     .Include(u => u.Role)
                                     .FirstOrDefaultAsync(u => u.Email == email);
            if (user.IsActive == false)
            {
                ViewData["LoginError"] = "Your account is deactivated. Please contact support.";
                return View();
            }
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

            if (!email.EndsWith("@omnitak.com", StringComparison.OrdinalIgnoreCase))
            {

                ViewData["RegistrationError"] = "Invalid email or password.";
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
                Role_Id = defaultRole.Id, // Assign the ID of the 'Developer' role
                Created_At = DateTime.UtcNow,
                IsActive = false
            };

            _context.User.Add(newUser);
            await _context.SaveChangesAsync();

            if(newUser.IsActive == false)
            {
                TempData["SuccessMessage"] = "Awaiting admins login's approval...";
                return View();
            }
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
                return RedirectToAction("Index", "Dashboard");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ManageAccount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            var user = await _context.User.FindAsync(int.Parse(userId));
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUsername(string Name)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            var user = await _context.User.FindAsync(int.Parse(userId));
            if (user == null)
            {
                return NotFound();
            }

            // Only update if the username is provided and different from the current one
            if (!string.IsNullOrWhiteSpace(Name) && user.Name != Name)
            {
                user.Name = Name;
                try
                {
                    await _context.SaveChangesAsync(); // Save changes to the database
                    TempData["SuccessMessage"] = "Your name has been updated successfully!";
                }
                catch (DbUpdateException ex)
                {
                    // Log the exception or handle it appropriately
                    TempData["ErrorMessage"] = "Error updating username.";
                }
                TempData["SuccessMessage"] = "Your name has been updated successfully!";
            }
            else if (string.IsNullOrWhiteSpace(Name))
            {
                TempData["ErrorMessage"] = "Username cannot be empty.";
            }

            return RedirectToAction(nameof(ManageAccount));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePassword(string currentPassword, string newPassword, string confirmNewPassword)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            var user = await _context.User.FindAsync(int.Parse(userId));
            if (user == null)
            {
                return NotFound();
            }

            // Only attempt password update if any password field is provided
            if (!string.IsNullOrWhiteSpace(currentPassword) || !string.IsNullOrWhiteSpace(newPassword) || !string.IsNullOrWhiteSpace(confirmNewPassword))
            {
                if (newPassword != confirmNewPassword)
                {
                    ViewData["PasswordErrorMessage"] = "New password and confirmation password do not match.";
                    return View("ManageAccount", user); // Return with current user data and error message
                }

                if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
                {
                    ViewData["PasswordErrorMessage"] = "Incorrect current password.";
                    return View(user);
                }

                if (string.IsNullOrWhiteSpace(newPassword))
                {
                    ViewData["PasswordErrorMessage"] = "New password cannot be empty.";
                    return View("ManageAccount", user);
                }

                // Update password
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Password updated successfully!";
            }
            return View("ManageAccount", user); // Return to the manage account page
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.User.FindAsync(int.Parse(userId));

            if (user != null)
            {
                user.IsActive = false;
                await _context.SaveChangesAsync();
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme); // Log user out after marking inactive
                TempData["AccountDeletionMessage"] = "Your account has been successfully deactivated.";
                return RedirectToAction("AccountDeletedConfirmation");
            }
            else
            {
                TempData["AccountDeletionError"] = "Could not find your account for deactivation.";
                return RedirectToAction("ManageAccount"); // Or another appropriate page
            }
        }

        [HttpGet]
        public IActionResult AccountDeletedConfirmation()
        {
            var message = TempData["AccountDeletionMessage"] as string;
            if (string.IsNullOrEmpty(message))
            {
                // Handle cases where they might land here without the TempData message
                // e.g., direct URL access
                return RedirectToAction("Login"); // Or another appropriate page
            }

            ViewBag.Message = message;
            return View("AccountDeletedConfirmation");
        }
    }
}