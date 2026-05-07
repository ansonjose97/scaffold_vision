using Microsoft.AspNetCore.Mvc;
using ScaffoldVision.Api.Models;
using ScaffoldVision.Api.Services;

namespace ScaffoldVision.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfigurationsController : ControllerBase
{
    private readonly IConfigurationService _service;

    public ConfigurationsController(IConfigurationService service) => _service = service;

    /// <summary>List all saved configurations.</summary>
    [HttpGet]
    public ActionResult<IReadOnlyList<ScaffoldConfiguration>> List()
        => Ok(_service.List());

    /// <summary>Fetch a configuration by id.</summary>
    [HttpGet("{id:guid}")]
    public ActionResult<ScaffoldConfiguration> Get(Guid id)
    {
        var config = _service.Get(id);
        return config is null ? NotFound() : Ok(config);
    }

    /// <summary>Save (create or update) a configuration.</summary>
    [HttpPost]
    public ActionResult<ScaffoldConfiguration> Save([FromBody] ScaffoldConfiguration config)
    {
        var saved = _service.Save(config);
        return CreatedAtAction(nameof(Get), new { id = saved.Id }, saved);
    }

    /// <summary>Delete a configuration.</summary>
    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        var deleted = _service.Delete(id);
        return deleted ? NoContent() : NotFound();
    }
}
