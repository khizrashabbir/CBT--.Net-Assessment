using KoperasiTentera.Application.Common;
using KoperasiTentera.Application.Dtos;
using KoperasiTentera.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KoperasiTentera.API.Controllers;

/// <summary>Serves the active Privacy Policy content shown on the policy consent screen.</summary>
[ApiController]
[Route("api/v1/privacy-policy")]
[Produces("application/json")]
public class PrivacyPolicyController : ControllerBase
{
    private readonly IRegistrationService _registrationService;

    public PrivacyPolicyController(IRegistrationService registrationService)
    {
        _registrationService = registrationService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PrivacyPolicyResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        PrivacyPolicyResponse response = await _registrationService.GetActivePrivacyPolicyAsync(cancellationToken);
        return Ok(ApiResponse<PrivacyPolicyResponse>.Ok(response));
    }
}
