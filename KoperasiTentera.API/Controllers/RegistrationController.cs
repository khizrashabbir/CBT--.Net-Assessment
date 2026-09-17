using KoperasiTentera.Application.Common;
using KoperasiTentera.Application.Dtos;
using KoperasiTentera.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KoperasiTentera.API.Controllers;

/// <summary>Flow A — New Customer Registration (create account, privacy policy, PIN, biometric).</summary>
[ApiController]
[Route("api/v1/registration")]
[Produces("application/json")]
public class RegistrationController : ControllerBase
{
    private readonly IRegistrationService _registrationService;

    public RegistrationController(IRegistrationService registrationService)
    {
        _registrationService = registrationService;
    }

    /// <summary>Step 1 of 4 — creates the customer and auto-issues the mobile OTP. 409 ACCOUNT_ALREADY_EXISTS if the IC is taken.</summary>
    [HttpPost("start")]
    [ProducesResponseType(typeof(ApiResponse<RegistrationStartResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Start([FromBody] RegistrationStartRequest request, CancellationToken cancellationToken)
    {
        RegistrationStartResponse response = await _registrationService.StartAsync(request, cancellationToken);
        return Ok(ApiResponse<RegistrationStartResponse>.Ok(response));
    }

    /// <summary>Step 2 of 4 — records T&amp;C / Privacy Policy consent. Requires EmailVerified status.</summary>
    [HttpPost("accept-policy")]
    [ProducesResponseType(typeof(ApiResponse<RegistrationStepResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AcceptPolicy([FromBody] AcceptPolicyRequest request, CancellationToken cancellationToken)
    {
        RegistrationStepResponse response = await _registrationService.AcceptPolicyAsync(request, cancellationToken);
        return Ok(ApiResponse<RegistrationStepResponse>.Ok(response));
    }

    /// <summary>Step 3 of 4 — creates and confirms the 6-digit PIN. 400 UNMATCHED_PIN when confirmation mismatches.</summary>
    [HttpPost("create-pin")]
    [ProducesResponseType(typeof(ApiResponse<RegistrationStepResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreatePin([FromBody] CreatePinRequest request, CancellationToken cancellationToken)
    {
        RegistrationStepResponse response = await _registrationService.CreatePinAsync(request, cancellationToken);
        return Ok(ApiResponse<RegistrationStepResponse>.Ok(response));
    }

    /// <summary>Step 4 of 4 — records the biometric preference (Enable Now / Maybe Later) and completes onboarding.</summary>
    [HttpPost("biometric")]
    [ProducesResponseType(typeof(ApiResponse<RegistrationStepResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Biometric([FromBody] BiometricRequest request, CancellationToken cancellationToken)
    {
        RegistrationStepResponse response = await _registrationService.SetBiometricAsync(request, cancellationToken);
        return Ok(ApiResponse<RegistrationStepResponse>.Ok(response));
    }
}
