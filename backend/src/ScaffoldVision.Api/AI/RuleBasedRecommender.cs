using ScaffoldVision.Api.Models;
using ScaffoldVision.Api.Services;

namespace ScaffoldVision.Api.AI;

/// <summary>
/// Computes a recommended scaffold configuration for a given building.
/// </summary>
public interface IScaffoldRecommender
{
    RecommendationResponse Recommend(
        BuildingDimensions building,
        ScaffoldingPreferences? preferences = null);
}

public record ScaffoldingPreferences
{
    /// <summary>Preferred horizontal bay length in metres. Default 2.5m.</summary>
    public double BayWidthMeters { get; init; } = 2.5;

    /// <summary>Vertical lift height in metres. Default 2.0m.</summary>
    public double LiftHeightMeters { get; init; } = 2.0;

    /// <summary>Place a diagonal brace every Nth bay. Default 5.</summary>
    public int BraceEveryNBays { get; init; } = 5;

    /// <summary>Wrap the scaffold around all four sides if true; otherwise front-only.</summary>
    public bool WrapAround { get; init; } = false;
}

public record RecommendationResponse
{
    public required RecommendationSummary Summary { get; init; }
    public required IReadOnlyList<RecommendedComponent> Components { get; init; }
    public required IReadOnlyList<string> Notes { get; init; }
    public decimal EstimatedTotalEur { get; init; }
    public double EstimatedWeightKg { get; init; }
}

public record RecommendationSummary
{
    public int Bays { get; init; }
    public int Lifts { get; init; }
    public double LinearFacadeMeters { get; init; }
    public double TotalAreaSquareMeters { get; init; }
}

public record RecommendedComponent
{
    public required string Sku { get; init; }
    public required string Name { get; init; }
    public int Quantity { get; init; }
    public decimal LineTotalEur { get; init; }
}

/// <summary>
/// A deterministic rule-based recommender. Computes how many components are needed
/// to wrap a scaffold around a building of given dimensions, using industry-typical
/// defaults for bay width and lift height.
/// </summary>
public class RuleBasedRecommender : IScaffoldRecommender
{
    private readonly IComponentCatalog _catalog;
    private readonly ILogger<RuleBasedRecommender> _log;

    public RuleBasedRecommender(IComponentCatalog catalog, ILogger<RuleBasedRecommender> log)
    {
        _catalog = catalog;
        _log = log;
    }

    public RecommendationResponse Recommend(
        BuildingDimensions building,
        ScaffoldingPreferences? preferences = null)
    {
        ArgumentNullException.ThrowIfNull(building);
        var prefs = preferences ?? new ScaffoldingPreferences();
        ValidateInputs(building, prefs);

        var catalog = _catalog.GetAll();
        var notes = new List<string>();

        // Geometry: compute the linear facade length the scaffold must cover.
        // For a wraparound scaffold the perimeter is 2*(width+depth), otherwise
        // just the front face.
        var facadeLength = prefs.WrapAround
            ? 2.0 * (building.WidthMeters + building.DepthMeters)
            : building.WidthMeters;

        // Round bay count up so the scaffold always fully covers the facade.
        var bays = (int)Math.Ceiling(facadeLength / prefs.BayWidthMeters);
        var lifts = (int)Math.Ceiling(building.HeightMeters / prefs.LiftHeightMeters);

        if (bays * prefs.BayWidthMeters > facadeLength + 0.5)
        {
            notes.Add($"Last bay overhangs by {bays * prefs.BayWidthMeters - facadeLength:F2}m. " +
                      "Consider a shorter ledger length to fit exactly.");
        }

        // Component counts: for an N-bay, M-lift scaffold,
        //   standards = (N+1) * (M+1)            -- vertical poles at every joint
        //   ledgers   = N * (M+1) * 2            -- inner and outer horizontals at every lift
        //   platforms = N * M                    -- one platform per bay per lift
        //   braces    = ceil(N / brace_every) * M
        var standards = (bays + 1) * (lifts + 1);
        var ledgers = bays * (lifts + 1) * 2;
        var platforms = bays * lifts;
        var braces = (int)Math.Ceiling((double)bays / prefs.BraceEveryNBays) * lifts;

        // Couplers: roughly 4 per ledger-to-standard joint. This is an estimate;
        // real planning would account for specific connection types per layout.
        var connectors = ledgers * 4;

        // Pick the catalog entries that best fit the chosen lift height and bay width.
        var standardSku = PickBestStandard(catalog, prefs.LiftHeightMeters, notes);
        var ledgerSku = PickBestLedger(catalog, prefs.BayWidthMeters, notes);
        var platformSku = catalog.FirstOrDefault(c => c.Category == ComponentCategory.Platform);
        var braceSku = catalog.FirstOrDefault(c => c.Category == ComponentCategory.Brace);
        var connectorSku = catalog.FirstOrDefault(c => c.Category == ComponentCategory.Connector);

        var lineItems = new List<RecommendedComponent>();
        AddLine(lineItems, standardSku, standards);
        AddLine(lineItems, ledgerSku, ledgers);
        AddLine(lineItems, platformSku, platforms);
        AddLine(lineItems, braceSku, braces);
        AddLine(lineItems, connectorSku, connectors);

        var totalCost = lineItems.Sum(l => l.LineTotalEur);
        var totalWeight = ComputeWeight(catalog, lineItems);

        if (building.HeightMeters > 8.0)
        {
            notes.Add("Heights above 8m typically require additional anchoring to the building. " +
                      "Consult applicable safety standards (e.g. DIN EN 12810 / 12811).");
        }

        if (prefs.BraceEveryNBays > 5)
        {
            notes.Add("Bracing intervals greater than 5 bays may not satisfy stability requirements " +
                      "under typical loading. Reduce the interval if in doubt.");
        }

        _log.LogInformation(
            "Generated recommendation: {Bays} bays x {Lifts} lifts, {Components} component lines, total {Cost:C}",
            bays, lifts, lineItems.Count, totalCost);

        return new RecommendationResponse
        {
            Summary = new RecommendationSummary
            {
                Bays = bays,
                Lifts = lifts,
                LinearFacadeMeters = facadeLength,
                TotalAreaSquareMeters = facadeLength * building.HeightMeters
            },
            Components = lineItems,
            Notes = notes,
            EstimatedTotalEur = totalCost,
            EstimatedWeightKg = totalWeight
        };
    }

    private static void ValidateInputs(BuildingDimensions building, ScaffoldingPreferences prefs)
    {
        if (building.WidthMeters <= 0 || building.HeightMeters <= 0 || building.DepthMeters <= 0)
            throw new ArgumentException("Building dimensions must be positive.");
        if (prefs.BayWidthMeters <= 0 || prefs.LiftHeightMeters <= 0)
            throw new ArgumentException("Preferences must specify positive bay width and lift height.");
        if (prefs.BraceEveryNBays < 1)
            throw new ArgumentException("BraceEveryNBays must be at least 1.");
    }

    private static ScaffoldComponent? PickBestStandard(
        IReadOnlyList<ScaffoldComponent> catalog,
        double targetLength,
        List<string> notes)
    {
        var standards = catalog.Where(c => c.Category == ComponentCategory.Standard).ToList();
        if (standards.Count == 0) return null;

        var best = standards.OrderBy(c => Math.Abs(c.LengthMeters - targetLength)).First();
        if (Math.Abs(best.LengthMeters - targetLength) > 0.3)
        {
            notes.Add($"No standard pole exactly matches the {targetLength:F1}m lift height. " +
                      $"Selected the closest match: {best.Name}.");
        }
        return best;
    }

    private static ScaffoldComponent? PickBestLedger(
        IReadOnlyList<ScaffoldComponent> catalog,
        double targetLength,
        List<string> notes)
    {
        var ledgers = catalog.Where(c => c.Category == ComponentCategory.Ledger).ToList();
        if (ledgers.Count == 0) return null;

        var best = ledgers.OrderBy(c => Math.Abs(c.LengthMeters - targetLength)).First();
        if (Math.Abs(best.LengthMeters - targetLength) > 0.3)
        {
            notes.Add($"No ledger exactly matches the {targetLength:F2}m bay width. " +
                      $"Selected the closest match: {best.Name}.");
        }
        return best;
    }

    private static void AddLine(List<RecommendedComponent> lines, ScaffoldComponent? component, int qty)
    {
        if (component is null || qty <= 0) return;
        lines.Add(new RecommendedComponent
        {
            Sku = component.Sku,
            Name = component.Name,
            Quantity = qty,
            LineTotalEur = component.UnitPriceEur * qty
        });
    }

    private static double ComputeWeight(
        IReadOnlyList<ScaffoldComponent> catalog,
        List<RecommendedComponent> lines)
    {
        var skuToWeight = catalog.ToDictionary(c => c.Sku, c => c.WeightKg);
        return lines.Sum(l => skuToWeight.GetValueOrDefault(l.Sku, 0.0) * l.Quantity);
    }
}
