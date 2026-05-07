namespace ScaffoldVision.Api.Models;

/// <summary>
/// A scaffold component type from the catalog (e.g. a 2.5m vertical standard).
/// </summary>
public record ScaffoldComponent
{
    public Guid Id { get; init; }
    public required string Sku { get; init; }
    public required string Name { get; init; }
    public required ComponentCategory Category { get; init; }
    public double LengthMeters { get; init; }
    public double WeightKg { get; init; }
    public decimal UnitPriceEur { get; init; }
}

public enum ComponentCategory
{
    Standard,    // vertical pole
    Ledger,      // horizontal beam
    Platform,    // walking surface
    Brace,       // diagonal brace
    Connector    // coupler / fitting
}

/// <summary>
/// A placed component within a saved configuration. Position is in metres relative
/// to the building's bottom-left corner; rotation is the Y-axis angle in radians.
/// </summary>
public record PlacedComponent
{
    public Guid ComponentId { get; init; }
    public double X { get; init; }
    public double Y { get; init; }
    public double Z { get; init; }
    public double RotationY { get; init; }
}

/// <summary>
/// A persisted scaffold configuration.
/// </summary>
public record ScaffoldConfiguration
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public BuildingDimensions Building { get; init; } = new();
    public List<PlacedComponent> Components { get; init; } = new();
}

public record BuildingDimensions
{
    public double WidthMeters { get; init; } = 10.0;
    public double HeightMeters { get; init; } = 6.0;
    public double DepthMeters { get; init; } = 8.0;
}
