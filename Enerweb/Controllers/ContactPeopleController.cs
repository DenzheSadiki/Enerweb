using Enerweb.Domain.Models;
using Enerweb.Interfaces.Services;

namespace Enerweb.Controllers;

public sealed class ContactPeopleController : CrudController<ContactPerson>
{
    public ContactPeopleController(IContactPersonService service) : base(service) { }
    protected override Guid GetId(ContactPerson entity) => entity.Id;
    protected override void SetId(ContactPerson entity, Guid id) => entity.Id = id;
}
