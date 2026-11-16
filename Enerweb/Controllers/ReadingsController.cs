using Enerweb.Domain.Models.Dtos;
using Enerweb.Domain.Models;
using Enerweb.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Enerweb.Controllers;

public sealed class ReadingsController : CrudController<Reading>
{
    private readonly IReadingService service;
    public ReadingsController(IReadingService service) : base(service) => this.service = service;

    [HttpGet("meter/{meterId:guid}")]
    public async Task<ActionResult<List<Reading>>> GetMeterReadingsByMeterId(
        Guid meterId, [FromQuery] DateTime? fromUtc = null, [FromQuery] DateTime? toUtc = null)
        => Ok(await service.GetByMeterAsync(meterId, fromUtc, toUtc));

    [HttpPost("summary")]
    public async Task<ActionResult<ReadingSummaryDto>> GetSummary([FromBody] ReadingSummaryRequest request)
        => Ok(await service.GetSummaryAsync(request));
    protected override Guid GetId(Reading entity) => entity.Id;
    protected override void SetId(Reading entity, Guid id) => entity.Id = id;
}
