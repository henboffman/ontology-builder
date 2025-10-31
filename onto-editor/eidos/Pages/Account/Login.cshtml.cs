using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Eidos.Models;
using System.ComponentModel.DataAnnotations;

namespace Eidos.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _configuration = configuration;
            _environment = environment;
        }

        [BindProperty(SupportsGet = true)]
        public string? Mode { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public bool IsRegisterMode => Mode == "register";
        public string ToggleUrl => IsRegisterMode ? "/Account/Login" : "/Account/Login?mode=register";

        // Check if OAuth providers are configured
        public bool IsGoogleConfigured =>
            !string.IsNullOrEmpty(_configuration["Authentication:Google:ClientId"]) &&
            !string.IsNullOrEmpty(_configuration["Authentication:Google:ClientSecret"]);

        public bool IsMicrosoftConfigured =>
            !string.IsNullOrEmpty(_configuration["Authentication:Microsoft:ClientId"]) &&
            !string.IsNullOrEmpty(_configuration["Authentication:Microsoft:ClientSecret"]);

        public bool IsGitHubConfigured =>
            !string.IsNullOrEmpty(_configuration["Authentication:GitHub:ClientId"]) &&
            !string.IsNullOrEmpty(_configuration["Authentication:GitHub:ClientSecret"]);

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = "";

            // Password not required for local development (supports passwordless login)
            [DataType(DataType.Password)]
            public string Password { get; set; } = "";

            [DataType(DataType.Password)]
            // Note: Compare validation removed - validated manually in HandleRegister to avoid validation errors in login mode
            public string ConfirmPassword { get; set; } = "";

            public string DisplayName { get; set; } = "";
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // In production (non-development), password is required for login mode
            if (!_environment.IsDevelopment() && !IsRegisterMode && string.IsNullOrEmpty(Input.Password))
            {
                ModelState.AddModelError(nameof(Input.Password), "Password is required.");
            }

            if (!ModelState.IsValid)
            {
                // Set error message from ModelState validation errors
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .Where(m => !string.IsNullOrEmpty(m));

                if (errors.Any())
                {
                    ErrorMessage = string.Join("; ", errors);
                }
                else
                {
                    ErrorMessage = "Please fill in all required fields.";
                }

                return Page();
            }

            if (IsRegisterMode)
            {
                return await HandleRegister();
            }
            else
            {
                return await HandleLogin();
            }
        }

        private async Task<IActionResult> HandleRegister()
        {
            if (string.IsNullOrWhiteSpace(Input.DisplayName))
            {
                ErrorMessage = "Display name is required";
                return Page();
            }

            // Manual password confirmation validation (not in attributes to avoid login mode validation errors)
            if (Input.Password != Input.ConfirmPassword)
            {
                ErrorMessage = "Passwords do not match";
                return Page();
            }

            var user = new ApplicationUser
            {
                UserName = Input.Email,
                Email = Input.Email,
                DisplayName = Input.DisplayName,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                SuccessMessage = "Registration successful! You can now login.";
                // Switch to login mode and preserve email
                Mode = null;
                Input = new InputModel { Email = user.Email };
                return Page();
            }
            else
            {
                ErrorMessage = string.Join(", ", result.Errors.Select(e => e.Description));
                return Page();
            }
        }

        private async Task<IActionResult> HandleLogin()
        {
            // First check if the user exists
            var user = await _userManager.FindByEmailAsync(Input.Email);

            if (user == null)
            {
                ErrorMessage = "No account found with this email address.";
                return Page();
            }

            // Allow passwordless login ONLY in development for accounts with no password hash set
            if (_environment.IsDevelopment() && string.IsNullOrEmpty(user.PasswordHash))
            {
                await _signInManager.SignInAsync(user, isPersistent: true);
                return Redirect("/");
            }

            var result = await _signInManager.PasswordSignInAsync(
                Input.Email,
                Input.Password,
                isPersistent: true,
                lockoutOnFailure: true
            );

            if (result.Succeeded)
            {
                return Redirect("/");
            }
            else if (result.IsLockedOut)
            {
                ErrorMessage = "Account locked due to multiple failed login attempts. Please try again in 15 minutes.";
                return Page();
            }
            else if (result.RequiresTwoFactor)
            {
                ErrorMessage = "Two-factor authentication is required but not implemented.";
                return Page();
            }
            else if (result.IsNotAllowed)
            {
                ErrorMessage = "Login not allowed. Please confirm your email address first.";
                return Page();
            }
            else
            {
                // More specific error message
                ErrorMessage = $"Invalid password for {Input.Email}. Please check your password and try again.";
                return Page();
            }
        }
    }
}
