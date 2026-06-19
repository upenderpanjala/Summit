using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Summit.VMS.Models.Entities;
using Summit.VMS.Services.Interfaces;
using Summit.VMS.ViewModels;

namespace Summit.VMS.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly IVictimService _victims;
    private readonly ICaseService _cases;

    public HomeController(IVictimService victims, ICaseService cases)
    {
        _victims = victims;
        _cases = cases;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.VictimCount = (await _victims.GetAllAsync()).Count;
        ViewBag.CaseCount = (await _cases.GetAllAsync()).Count;
        return View();
    }

    [AllowAnonymous]
    public IActionResult Error() => View();
}

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IAuditService _audit;

    public AccountController(
        SignInManager<ApplicationUser> signIn,
        UserManager<ApplicationUser> users,
        IAuditService audit)
    {
        _signIn = signIn;
        _users = users;
        _audit = audit;
    }

    [HttpGet, AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
        => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _users.FindByEmailAsync(model.Email);
        if (user is { IsActive: false })
        {
            ModelState.AddModelError(string.Empty, "This account is disabled.");
            return View(model);
        }

        var result = await _signIn.PasswordSignInAsync(
            model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            await _audit.LogAsync("Login", "Account", user?.Id, model.Email);
            return RedirectToLocal(model.ReturnUrl);
        }

        if (result.IsLockedOut)
            ModelState.AddModelError(string.Empty, "Account locked. Try again later.");
        else
            ModelState.AddModelError(string.Empty, "Invalid email or password.");

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    private IActionResult RedirectToLocal(string? returnUrl)
        => Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl!)
            : RedirectToAction(nameof(HomeController.Index), "Home");
}
