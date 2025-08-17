using BlueSchoolSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

public class ResetPasswordWithOTPModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ResetPasswordWithOTPModel(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [BindProperty]
    public string Email { get; set; }

    [BindProperty]
    [Required]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; }

    [BindProperty]
    [DataType(DataType.Password)]
    [Compare("NewPassword")]
    public string ConfirmPassword { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.FindByEmailAsync(Email);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Không tìm thấy người dùng.");
            return Page();
        }

        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, resetToken, NewPassword);

        if (result.Succeeded)
        {
            return RedirectToPage("./ResetPasswordConfirmation");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return Page();
    }
}
