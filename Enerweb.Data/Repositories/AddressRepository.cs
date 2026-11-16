using Enerweb.Domain.Models;
using Enerweb.Interfaces.Repositories;

namespace Enerweb.Data.Repositories;

public sealed class AddressRepository : BaseRepository<Address>, IAddressRepository
{
    public AddressRepository(EnerwebDbContext context) : base(context) { }
}
