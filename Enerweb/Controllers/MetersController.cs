using Enerweb.Domain.Models;
using Enerweb.Interfaces.Services;

namespace Enerweb.Controllers;

public sealed class MetersController : CrudController<Meters>
{
    public MetersController(IMeterService service) : base(service) { }
    protected override Guid GetId(Meters entity) => entity.Id;
    protected override void SetId(Meters entity, Guid id) => entity.Id = id;
}
