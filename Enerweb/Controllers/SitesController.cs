using Enerweb.Domain.Models;
using Enerweb.Interfaces.Services;

namespace Enerweb.Controllers;

public sealed class SitesController : CrudController<Site>
{
    public SitesController(ISiteService service) : base(service) { }
    protected override Guid GetId(Site entity) => entity.Id;
    protected override void SetId(Site entity, Guid id) => entity.Id = id;
}
