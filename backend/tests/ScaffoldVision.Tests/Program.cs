using Microsoft.Extensions.Logging.Abstractions;
using ScaffoldVision.Api.AI;
using ScaffoldVision.Api.Models;
using ScaffoldVision.Api.Services;
using ScaffoldVision.Tests;

// A minimal test runner. We use a console app with hand-rolled assertions instead
// of pulling in xUnit so the test project has zero external NuGet dependencies —
// `dotnet run` from this directory executes the suite. This keeps the project
// fully buildable offline and reduces friction for someone cloning the repo.

var runner = new TestRunner();

runner.Run("Recommend_SmallBuilding_ProducesPositiveCounts", () =>
{
    var recommender = NewRecommender();
    var building = new BuildingDimensions { WidthMeters = 10, HeightMeters = 6, DepthMeters = 8 };

    var result = recommender.Recommend(building);

    Assert.True(result.Summary.Bays > 0, "expected at least one bay");
    Assert.True(result.Summary.Lifts > 0, "expected at least one lift");
    Assert.True(result.Components.Count > 0, "expected line items");
    Assert.True(result.EstimatedTotalEur > 0, "expected positive total cost");
});

runner.Run("Recommend_WrapAround_ProducesMoreComponentsThanFrontOnly", () =>
{
    var recommender = NewRecommender();
    var building = new BuildingDimensions { WidthMeters = 10, HeightMeters = 6, DepthMeters = 8 };

    var frontOnly = recommender.Recommend(building, new() { WrapAround = false });
    var wrapAround = recommender.Recommend(building, new() { WrapAround = true });

    Assert.True(wrapAround.Summary.LinearFacadeMeters > frontOnly.Summary.LinearFacadeMeters,
        "wrap-around facade length should exceed front-only");
    Assert.True(wrapAround.EstimatedTotalEur > frontOnly.EstimatedTotalEur,
        "wrap-around cost should exceed front-only");
});

runner.Run("Recommend_BayCountCoversFacade", () =>
{
    var recommender = NewRecommender();
    var building = new BuildingDimensions { WidthMeters = 11.5, HeightMeters = 6, DepthMeters = 8 };
    var prefs = new ScaffoldingPreferences { BayWidthMeters = 2.5, WrapAround = false };

    var result = recommender.Recommend(building, prefs);

    // 11.5m / 2.5m = 4.6, rounds up to 5 bays.
    Assert.Equal(5, result.Summary.Bays);
});

runner.Run("Recommend_TallBuilding_AddsAnchoringNote", () =>
{
    var recommender = NewRecommender();
    var building = new BuildingDimensions { WidthMeters = 10, HeightMeters = 12, DepthMeters = 8 };

    var result = recommender.Recommend(building);

    var hasAnchoringNote = result.Notes.Any(n =>
        n.Contains("anchoring", StringComparison.OrdinalIgnoreCase));
    Assert.True(hasAnchoringNote, "expected note about anchoring for tall building");
});

runner.Run("Recommend_ZeroDimension_Throws", () =>
{
    var recommender = NewRecommender();
    var building = new BuildingDimensions { WidthMeters = 0, HeightMeters = 6, DepthMeters = 8 };

    Assert.Throws<ArgumentException>(() => recommender.Recommend(building));
});

runner.Run("Recommend_StandardCount_MatchesFormula_1x1", () =>
{
    AssertStandardCount(expectedBays: 1, expectedLifts: 1);
});

runner.Run("Recommend_StandardCount_MatchesFormula_2x1", () =>
{
    AssertStandardCount(expectedBays: 2, expectedLifts: 1);
});

runner.Run("Recommend_StandardCount_MatchesFormula_3x2", () =>
{
    AssertStandardCount(expectedBays: 3, expectedLifts: 2);
});

return runner.Summarize();


// --- Helpers ---

static IScaffoldRecommender NewRecommender()
    => new RuleBasedRecommender(
        new InMemoryComponentCatalog(),
        NullLogger<RuleBasedRecommender>.Instance);

static void AssertStandardCount(int expectedBays, int expectedLifts)
{
    var recommender = NewRecommender();
    var building = new BuildingDimensions
    {
        WidthMeters = expectedBays * 2.5,
        HeightMeters = expectedLifts * 2.0,
        DepthMeters = 5
    };

    var result = recommender.Recommend(building);

    var standardLine = result.Components.First(c => c.Sku.StartsWith("STD"));
    var expected = (expectedBays + 1) * (expectedLifts + 1);
    Assert.Equal(expected, standardLine.Quantity);
}
