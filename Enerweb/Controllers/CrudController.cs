using Enerweb.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Enerweb.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class CrudController<TEntity> : ControllerBase where TEntity : class
{
    private readonly IBaseService<TEntity> service;
    protected CrudController(IBaseService<TEntity> service) => this.service = service;
    protected abstract Guid GetId(TEntity entity);
    protected abstract void SetId(TEntity entity, Guid id);

    [HttpGet]
    public async Task<ActionResult<List<TEntity>>> GetAll() => Ok(await service.GetAllAsync());

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TEntity>> GetById(Guid id)
    {
        var entity = await service.GetByIdAsync(id);
        return entity == null ? NotFound() : Ok(entity);
    }

    [HttpPost]
    public async Task<ActionResult<TEntity>> Create([FromBody] TEntity entity)
    {
        await service.CreateAsync(entity);
        return CreatedAtAction(nameof(GetById), new { id = GetId(entity) }, entity);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] TEntity entity)
    {
        if (GetId(entity) != Guid.Empty && GetId(entity) != id)
            return BadRequest(new ProblemDetails { Title = "Route and body IDs must match." });
        SetId(entity, id);
        await service.UpdateAsync(entity);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await service.GetByIdAsync(id);
        if (entity == null) return NotFound();
        await service.DeleteAsync(entity);
        return NoContent();
    }
}
