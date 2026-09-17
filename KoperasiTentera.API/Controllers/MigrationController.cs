using KoperasiTentera.Application.Common;
using KoperasiTentera.Application.Dtos;
using KoperasiTentera.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KoperasiTentera.API.Controllers;

/// <summary>Flow B — Migrate Existing User (legacy customer onboarding onto PIN + biometric login).</summary>
[ApiController]
[Route("api/v1/migration")]
[Produces("application/json")]
public class MigrationController : ControllerBase
{
    private readonly IMigrationService _migrationService;

    public MigrationController(IMigrationService migrationService)
    {
        _migrationService = migrationService;
    }

    /// <summary>Finds the legacy customer by IC, returns masked contact details, and auto-issues the mobile OTP.</summary>
    [HttpPost("start")]
    [ProducesResponseType(typeof(ApiResponse<MigrationStartResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Start([FromBody] MigrationStartRequest request, CancellationToken cancellationToken)
    {
        MigrationStartResponse response = await _migrationService.StartAsync(request, cancellationToken);
        return Ok(ApiResponse<MigrationStartResponse>.Ok(response));
    }

    /// <summary>Updates the customer's email (the "Change Email Address" link) and re-issues the email OTP.</summary>
    [HttpPost("change-email")]
    [ProducesResponseType(typeof(ApiResponse<ChangeEmailResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailRequest request, CancellationToken cancellationToken)
    {
        ChangeEmailResponse response = await _migrationService.ChangeEmailAsync(request, cancellationToken);
        return Ok(ApiResponse<ChangeEmailResponse>.Ok(response));
    }
}
