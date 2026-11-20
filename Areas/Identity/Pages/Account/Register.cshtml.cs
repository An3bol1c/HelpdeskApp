using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HelpdeskApp.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class RegisterModel : PageModel
{
    private const string DefaultRole = "User";
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<RegisterModel> _logger;

    public RegisterModel(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<RegisterModel> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (!await _roleManager.RoleExistsAsync(DefaultRole))
        {
            ModelState.AddModelError(string.Empty, "The default User role is not configured.");
            return Page();
        }

        var user = new IdentityUser
        {
            UserName = Input.Email,
            Email = Input.Email
        };

        var createResult = await _userManager.CreateAsync(user, Input.Password);
        if (!createResult.Succeeded)
        {
            AddErrors(createResult);
            return Page();
        }

        var roleResult = await _userManager.AddToRoleAsync(user, DefaultRole);
        if (!roleResult.Succeeded)
        {
            AddErrors(roleResult);

            var deleteResult = await _userManager.DeleteAsync(user);
            if (!deleteResult.Succeeded)
            {
                _logger.LogError(
                    "Could not remove newly registered user {UserId} after assigning the default role failed.",
                    user.Id);
                AddErrors(deleteResult);
                ModelState.AddModelError(
                    string.Empty,
                    "The account was created but could not be assigned the default role. Contact an administrator.");
            }

            return Page();
        }

        await _signInManager.SignInAsync(user, isPersistent: false);
        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Content("~/"));
    }

    private void AddErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at most {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare(nameof(Password), ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = "";
    }
}
