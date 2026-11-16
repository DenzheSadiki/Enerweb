using Enerweb.Domain.Models;
using Enerweb.Interfaces;
using Enerweb.Interfaces.Repositories;
using Enerweb.Interfaces.Services;

namespace Enerweb.Services;

public sealed class AddressService : BaseService<Address>, IAddressService
{
    public AddressService(IAddressRepository repository, IUnitOfWork unitOfWork) : base(repository, unitOfWork) { }

    protected override Guid GetId(Address entity) => entity.id;

    protected override Task ValidateAsync(Address entity)
    {
        RequireText(entity.AddressLine1, nameof(entity.AddressLine1));
        RequireText(entity.City, nameof(entity.City));
        RequireText(entity.Province, nameof(entity.Province));
        RequireText(entity.PostalCode, nameof(entity.PostalCode));
        RequireText(entity.Country, nameof(entity.Country));
        entity.AddressLine2 ??= "";
        return Task.CompletedTask;
    }

    public override Task CreateAsync(Address entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        entity.CreatedAt = DateTime.UtcNow;
        return base.CreateAsync(entity);
    }

    protected override void ApplyChanges(Address source, Address target)
    {
        target.AddressLine1 = source.AddressLine1;
        target.AddressLine2 = source.AddressLine2;
        target.City = source.City;
        target.Province = source.Province;
        target.PostalCode = source.PostalCode;
        target.Country = source.Country;
        target.UpdatedAt = DateTime.UtcNow;
    }
}
