using System.Collections.Concurrent;
using ScaffoldVision.Api.Models;

namespace ScaffoldVision.Api.Services;

/// <summary>
/// Persistence boundary for saved scaffold configurations. The in-memory
/// implementation keeps the MVP self-contained; a Postgres-backed implementation
/// is the production path described in docs/ARCHITECTURE.md.
/// </summary>
public interface IConfigurationStore
{
    void Upsert(ScaffoldConfiguration config);
    ScaffoldConfiguration? Get(Guid id);
    IReadOnlyList<ScaffoldConfiguration> List();
    bool Delete(Guid id);
}

public class InMemoryConfigurationStore : IConfigurationStore
{
    private readonly ConcurrentDictionary<Guid, ScaffoldConfiguration> _configs = new();

    public void Upsert(ScaffoldConfiguration config)
        => _configs[config.Id] = config;

    public ScaffoldConfiguration? Get(Guid id)
        => _configs.TryGetValue(id, out var c) ? c : null;

    public IReadOnlyList<ScaffoldConfiguration> List()
        => _configs.Values.OrderByDescending(c => c.CreatedAt).ToList();

    public bool Delete(Guid id) => _configs.TryRemove(id, out _);
}

public interface IConfigurationService
{
    ScaffoldConfiguration Save(ScaffoldConfiguration config);
    ScaffoldConfiguration? Get(Guid id);
    IReadOnlyList<ScaffoldConfiguration> List();
    bool Delete(Guid id);
}

public class ConfigurationService : IConfigurationService
{
    private readonly IConfigurationStore _store;
    private readonly ILogger<ConfigurationService> _log;

    public ConfigurationService(IConfigurationStore store, ILogger<ConfigurationService> log)
    {
        _store = store;
        _log = log;
    }

    public ScaffoldConfiguration Save(ScaffoldConfiguration config)
    {
        var toSave = config with
        {
            Id = config.Id == Guid.Empty ? Guid.NewGuid() : config.Id,
            CreatedAt = config.CreatedAt == default ? DateTimeOffset.UtcNow : config.CreatedAt
        };

        _store.Upsert(toSave);
        _log.LogInformation("Saved configuration {ConfigId} with {Count} components",
            toSave.Id, toSave.Components.Count);
        return toSave;
    }

    public ScaffoldConfiguration? Get(Guid id) => _store.Get(id);
    public IReadOnlyList<ScaffoldConfiguration> List() => _store.List();
    public bool Delete(Guid id) => _store.Delete(id);
}
