using Microsoft.AspNetCore.Mvc;
using ScaffoldVision.Api.AI;
using ScaffoldVision.Api.Models;

namespace ScaffoldVision.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecommendationsController : ControllerBase
{
    private readonly IScaffoldRecommender _recommender;

    public RecommendationsController(IScaffoldRecommender recommender)
        => _recommender = recommender;

    /// <summary>
    /// Compute a recommended scaffold configuration for a given building and preferences.
    /// </summary>
    [HttpPost]
    public ActionResult<RecommendationResponse> Recommend([FromBody] RecommendationRequest request)
    {
        try
        {
            var result = _recommender.Recommend(request.Building, request.Preferences);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public record RecommendationRequest
{
    public BuildingDimensions Building { get; init; } = new();
    public ScaffoldingPreferences? Preferences { get; init; }
}
