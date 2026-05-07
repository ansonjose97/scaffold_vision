using ScaffoldVision.Api.Models;

namespace ScaffoldVision.Api.Services;

public interface IComponentCatalog
{
    IReadOnlyList<ScaffoldComponent> GetAll();
    ScaffoldComponent? GetById(Guid id);
    IReadOnlyList<ScaffoldComponent> GetByCategory(ComponentCategory category);
}

/// <summary>
/// In-memory catalog seeded with a small representative set of scaffold components.
/// In production this would be backed by a relational store (see docs/ARCHITECTURE.md).
/// </summary>
public class InMemoryComponentCatalog : IComponentCatalog
{
    private readonly List<ScaffoldComponent> _items;

    public InMemoryComponentCatalog()
    {
        _items = new List<ScaffoldComponent>
        {
            new()
            {
                Id = Guid.NewGuid(), Sku = "STD-200", Name = "Standard 2.0m",
                Category = ComponentCategory.Standard, LengthMeters = 2.0,
                WeightKg = 6.4, UnitPriceEur = 28.50m
            },
            new()
            {
                Id = Guid.NewGuid(), Sku = "STD-300", Name = "Standard 3.0m",
                Category = ComponentCategory.Standard, LengthMeters = 3.0,
                WeightKg = 9.6, UnitPriceEur = 39.00m
            },
            new()
            {
                Id = Guid.NewGuid(), Sku = "LDG-250", Name = "Ledger 2.5m",
                Category = ComponentCategory.Ledger, LengthMeters = 2.5,
                WeightKg = 5.1, UnitPriceEur = 22.00m
            },
            new()
            {
                Id = Guid.NewGuid(), Sku = "LDG-307", Name = "Ledger 3.07m",
                Category = ComponentCategory.Ledger, LengthMeters = 3.07,
                WeightKg = 6.3, UnitPriceEur = 26.40m
            },
            new()
            {
                Id = Guid.NewGuid(), Sku = "PLT-250", Name = "Steel Platform 2.5m",
                Category = ComponentCategory.Platform, LengthMeters = 2.5,
                WeightKg = 16.8, UnitPriceEur = 65.00m
            },
            new()
            {
                Id = Guid.NewGuid(), Sku = "BRC-280", Name = "Diagonal Brace 2.8m",
                Category = ComponentCategory.Brace, LengthMeters = 2.8,
                WeightKg = 7.4, UnitPriceEur = 31.00m
            },
            new()
            {
                Id = Guid.NewGuid(), Sku = "CPL-001", Name = "Right-Angle Coupler",
                Category = ComponentCategory.Connector, LengthMeters = 0.0,
                WeightKg = 0.9, UnitPriceEur = 4.20m
            }
        };
    }

    public IReadOnlyList<ScaffoldComponent> GetAll()
        => _items.OrderBy(c => c.Sku).ToList();

    public ScaffoldComponent? GetById(Guid id)
        => _items.FirstOrDefault(c => c.Id == id);

    public IReadOnlyList<ScaffoldComponent> GetByCategory(ComponentCategory category)
        => _items.Where(c => c.Category == category)
                 .OrderBy(c => c.LengthMeters)
                 .ToList();
}
