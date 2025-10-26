using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Eidos.Models;
using System.Security.Claims;

namespace Eidos.Pages.Account
{
    public class ExternalLoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public ExternalLoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        public IActionResult OnGet(string provider)
        {
            // Request a redirect to the external login provider
            var redirectUrl = Url.Page("/Account/ExternalLogin", pageHandler: "Callback");
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }

        public async Task<IActionResult> OnGetCallbackAsync(string? remoteError = null)
        {
            if (remoteError != null)
            {
                return RedirectToPage("/Account/Login", new { error = $"Error from external provider: {remoteError}" });
            }

            // Get the information about the user from the external login provider
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return RedirectToPage("/Account/Login", new { error = "Error loading external login information." });
            }

            // Try to sign in the user with this external login provider
            var result = await _signInManager.ExternalLoginSignInAsync(
                info.LoginProvider,
                info.ProviderKey,
                isPersistent: true,
                bypassTwoFactor: true
            );

            if (result.Succeeded)
            {
                return Redirect("/");
            }
            else
            {
                // External login doesn't exist yet - check if user exists by email
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                var name = info.Principal.FindFirstValue(ClaimTypes.Name);

                if (email == null)
                {
                    return RedirectToPage("/Account/Login", new { error = "Email not received from provider. Please ensure your account has a verified email." });
                }

                // Check if a user with this email already exists
                var existingUser = await _userManager.FindByEmailAsync(email);

                if (existingUser != null)
                {
                    // User exists - link this external login to the existing account
                    var addLoginResult = await _userManager.AddLoginAsync(existingUser, info);
                    if (addLoginResult.Succeeded)
                    {
                        await _signInManager.SignInAsync(existingUser, isPersistent: true);
                        return Redirect("/");
                    }
                    else
                    {
                        var errors = string.Join(", ", addLoginResult.Errors.Select(e => e.Description));
                        return RedirectToPage("/Account/Login", new { error = $"Error linking account: {errors}" });
                    }
                }
                else
                {
                    // User doesn't exist - create a new account
                    var user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        DisplayName = name ?? email,
                        CreatedAt = DateTime.UtcNow,
                        EmailConfirmed = true // External provider emails are already verified
                    };

                    var createResult = await _userManager.CreateAsync(user);
                    if (createResult.Succeeded)
                    {
                        createResult = await _userManager.AddLoginAsync(user, info);
                        if (createResult.Succeeded)
                        {
                            await _signInManager.SignInAsync(user, isPersistent: true);
                            return Redirect("/");
                        }
                    }

                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    return RedirectToPage("/Account/Login", new { error = $"Error creating account: {errors}" });
                }
            }
        }
    }
}
