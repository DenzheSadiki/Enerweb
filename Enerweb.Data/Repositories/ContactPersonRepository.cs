using Enerweb.Domain.Models;
using Enerweb.Interfaces.Repositories;

namespace Enerweb.Data.Repositories;

public sealed class ContactPersonRepository : BaseRepository<ContactPerson>, IContactPersonRepository
{
    public ContactPersonRepository(EnerwebDbContext context) : base(context) { }
}
