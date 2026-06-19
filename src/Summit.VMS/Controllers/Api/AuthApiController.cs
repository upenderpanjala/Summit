using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Summit.VMS.DTOs;
using Summit.VMS.Models.Entities;
using Summit.VMS.Services.Interfaces;

namespace Summit.VMS.Controllers.Api;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthApiController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly ITokenService _tokens;
    private readonly IAuditService _audit;

    public AuthApiController(
        UserManager<ApplicationUser> users,
        SignInManager<ApplicationUser> signIn,
        ITokenService tokens,
        IAuditService audit)
    {
        _users = users;
        _signIn = signIn;
        _tokens = tokens;
        _audit = audit;
    }

    /// <summary>Exchange credentials for a JWT bearer token.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _users.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
            return Unauthorized(new { message = "Invalid credentials." });

        var ok = await _signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!ok.Succeeded)
            return Unauthorized(new { message = "Invalid credentials." });

        var roles = await _users.GetRolesAsync(user);
        await _audit.LogAsync("ApiLogin", "Account", user.Id, user.Email);
        return Ok(_tokens.CreateToken(user, roles));
    }

    /// <summary>Returns the identity of the caller (verifies the bearer token).</summary>
    [HttpGet("me")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public IActionResult Me() => Ok(new
    {
        name = User.Identity?.Name,
        roles = User.Claims.Where(c => c.Type.EndsWith("/role")).Select(c => c.Value)
    });
}
