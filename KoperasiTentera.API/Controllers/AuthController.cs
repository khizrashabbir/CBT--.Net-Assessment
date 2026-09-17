using KoperasiTentera.Application.Common;
using KoperasiTentera.Application.Dtos;
using KoperasiTentera.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KoperasiTentera.API.Controllers;

/// <summary>
/// Entry-point lookups (IC search) and PIN-based login for already-onboarded customers.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Looks up a customer by IC number to decide which flow (register / migrate / login) to show.</summary>
    [HttpPost("ic-lookup")]
    [ProducesResponseType(typeof(ApiResponse<IcLookupResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> IcLookup([FromBody] IcLookupRequest request, CancellationToken cancellationToken)
    {
        IcLookupResponse response = await _authService.LookupIcAsync(request, cancellationToken);
        return Ok(ApiResponse<IcLookupResponse>.Ok(response));
    }

    /// <summary>Logs an Active customer in with their IC number and 6-digit PIN. No tokens are issued.</summary>
    [HttpPost("pin-login")]
    [ProducesResponseType(typeof(ApiResponse<PinLoginResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> PinLogin([FromBody] PinLoginRequest request, CancellationToken cancellationToken)
    {
        PinLoginResponse response = await _authService.PinLoginAsync(request, cancellationToken);
        return Ok(ApiResponse<PinLoginResponse>.Ok(response));
    }
}
