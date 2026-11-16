using System.Net.Mail;
using Enerweb.Domain.Models;
using Enerweb.Interfaces;
using Enerweb.Interfaces.Repositories;
using Enerweb.Interfaces.Services;

namespace Enerweb.Services;

public sealed class ContactPersonService : BaseService<ContactPerson>, IContactPersonService
{
    public ContactPersonService(IContactPersonRepository repository, IUnitOfWork unitOfWork) : base(repository, unitOfWork) { }

    protected override Guid GetId(ContactPerson entity) => entity.Id;

    protected override Task ValidateAsync(ContactPerson entity)
    {
        RequireText(entity.Name, nameof(entity.Name));
        RequireText(entity.ContactNumber, nameof(entity.ContactNumber));
        RequireText(entity.Email, nameof(entity.Email));
        if (!MailAddress.TryCreate(entity.Email, out var address) || address.Address != entity.Email)
            throw new ArgumentException("A valid email address is required.", nameof(entity.Email));
        return Task.CompletedTask;
    }

    public override Task CreateAsync(ContactPerson entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        entity.CreatedAt = DateTime.UtcNow;
        return base.CreateAsync(entity);
    }

    protected override void ApplyChanges(ContactPerson source, ContactPerson target)
    {
        target.Name = source.Name;
        target.ContactNumber = source.ContactNumber;
        target.Email = source.Email;
        target.UpdatedAt = DateTime.UtcNow;
    }
}
