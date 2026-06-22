using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Summit.VMS.Services.Safety;

namespace Summit.VMS.Controllers.Api;

/// <summary>
/// Reference data for the registration dropdowns (states + districts).
/// Covers Telangana and Andhra Pradesh.
/// </summary>
[ApiController]
[Route("api/app/jurisdictions")]
[AllowAnonymous]
[Produces("application/json")]
public class JurisdictionApiController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(Jurisdictions.AsDto());
}
