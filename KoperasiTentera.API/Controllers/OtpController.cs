using KoperasiTentera.Application.Common;
using KoperasiTentera.Application.Dtos;
using KoperasiTentera.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KoperasiTentera.API.Controllers;

/// <summary>
/// Shared OTP send/verify endpoints used by both the New Customer Registration
/// and Migrate Existing User flows.
/// </summary>
[ApiController]
[Route("api/v1/otp")]
[Produces("application/json")]
public class OtpController : ControllerBase
{
    private readonly IOtpService _otpService;

    public OtpController(IOtpService otpService)
    {
        _otpService = otpService;
    }

    /// <summary>Sends (or resends) a 4-digit OTP to the customer's mobile or email. Enforces a 120s resend cooldown.</summary>
    [HttpPost("send")]
    [ProducesResponseType(typeof(ApiResponse<OtpSendResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Send([FromBody] OtpSendRequest request, CancellationToken cancellationToken)
    {
        OtpSendResponse response = await _otpService.SendAsync(request, cancellationToken);
        return Ok(ApiResponse<OtpSendResponse>.Ok(response));
    }

    /// <summary>Verifies the 4-digit OTP; advances the customer's status when correct.</summary>
    [HttpPost("verify")]
    [ProducesResponseType(typeof(ApiResponse<OtpVerifyResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Verify([FromBody] OtpVerifyRequest request, CancellationToken cancellationToken)
    {
        OtpVerifyResponse response = await _otpService.VerifyAsync(request, cancellationToken);
        return Ok(ApiResponse<OtpVerifyResponse>.Ok(response));
    }
}
