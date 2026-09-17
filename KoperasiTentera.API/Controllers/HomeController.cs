using KoperasiTentera.Application.Common;
using KoperasiTentera.Application.Dtos;
using KoperasiTentera.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KoperasiTentera.API.Controllers;

/// <summary>Returns the post-login dashboard content: greeting, biometric flag, and active banners.</summary>
[ApiController]
[Route("api/v1/home")]
[Produces("application/json")]
public class HomeController : ControllerBase
{
    private readonly IHomeService _homeService;

    public HomeController(IHomeService homeService)
    {
        _homeService = homeService;
    }

    [HttpGet("{customerId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<HomeResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(Guid customerId, CancellationToken cancellationToken)
    {
        HomeResponse response = await _homeService.GetHomeAsync(customerId, cancellationToken);
        return Ok(ApiResponse<HomeResponse>.Ok(response));
    }
}
