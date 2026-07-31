using Microsoft.AspNetCore.Mvc;
using WedBanSach.Services.Apriori;

namespace WedBanSach.Controllers.Api;

[Route("api/apriori")]
[ApiController]
public class AprioriApiController : ControllerBase
{
    private readonly IAprioriService _aprioriService;

    public AprioriApiController(IAprioriService aprioriService)
    {
        _aprioriService = aprioriService;
    }

    [HttpGet("recommend/{bookId}")]
    public async Task<IActionResult> GetRecommendations(int bookId, [FromQuery] int top = 5)
    {
        var recommendations = await _aprioriService.GetRecommendationsForBookAsync(bookId, top);
        return Ok(recommendations);
    }

    [HttpPost("recommend-cart")]
    public async Task<IActionResult> GetRecommendationsForCart([FromBody] List<int> cartBookIds, [FromQuery] int top = 5)
    {
        if (cartBookIds == null || !cartBookIds.Any())
        {
            return BadRequest("Cart is empty.");
        }

        var recommendations = await _aprioriService.GetRecommendationsForCartAsync(cartBookIds, top);
        return Ok(recommendations);
    }
}
