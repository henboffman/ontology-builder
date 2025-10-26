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

        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [BindProperty(SupportsGet = true)]
        public string? Mode { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public bool IsRegisterMode => Mode == "register";
        public string ToggleUrl => IsRegisterMode ? "/Account/Login" : "/Account/Login?mode=register";

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = "";

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; } = "";

            [DataType(DataType.Password)]
            [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
            public string ConfirmPassword { get; set; } = "";

            public string DisplayName { get; set; } = "";
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
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
            else
            {
                ErrorMessage = "Invalid email or password";
                return Page();
            }
        }
    }
}
