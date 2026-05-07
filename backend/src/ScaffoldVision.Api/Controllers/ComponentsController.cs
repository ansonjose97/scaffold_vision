using Microsoft.AspNetCore.Mvc;
using ScaffoldVision.Api.Models;
using ScaffoldVision.Api.Services;

namespace ScaffoldVision.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ComponentsController : ControllerBase
{
    private readonly IComponentCatalog _catalog;

    public ComponentsController(IComponentCatalog catalog) => _catalog = catalog;

    /// <summary>List all available components, optionally filtered by category.</summary>
    [HttpGet]
    public ActionResult<IReadOnlyList<ScaffoldComponent>> GetAll(
        [FromQuery] ComponentCategory? category)
    {
        var items = category.HasValue
            ? _catalog.GetByCategory(category.Value)
            : _catalog.GetAll();
        return Ok(items);
    }

    /// <summary>Fetch a single component by id.</summary>
    [HttpGet("{id:guid}")]
    public ActionResult<ScaffoldComponent> GetById(Guid id)
    {
        var component = _catalog.GetById(id);
        return component is null ? NotFound() : Ok(component);
    }
}
