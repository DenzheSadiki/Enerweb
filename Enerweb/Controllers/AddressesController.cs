using Enerweb.Domain.Models;
using Enerweb.Interfaces.Services;

namespace Enerweb.Controllers;

public sealed class AddressesController : CrudController<Address>
{
    public AddressesController(IAddressService service) : base(service) { }
    protected override Guid GetId(Address entity) => entity.id;
    protected override void SetId(Address entity, Guid id) => entity.id = id;
}
