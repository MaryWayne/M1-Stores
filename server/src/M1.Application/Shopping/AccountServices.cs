using M1.Application.Common;
using M1.Application.Interfaces;
using M1.Domain.Entities;

namespace M1.Application.Shopping;

public class AddressService(IRepository<Address> addresses, IUnitOfWork uow)
{
    public async Task<IReadOnlyList<AddressDto>> ListAsync(Guid userId, CancellationToken ct = default) =>
        (await addresses.ListAsync(a => a.UserId == userId, ct))
            .OrderByDescending(a => a.IsDefault).ThenBy(a => a.CreatedAt)
            .Select(ToDto).ToList();

    public async Task<AddressDto> SaveAsync(Guid userId, Guid? id, SaveAddressRequest request, CancellationToken ct = default)
    {
        Address address;
        if (id is { } existing)
        {
            address = await addresses.FirstOrDefaultAsync(a => a.Id == existing && a.UserId == userId, ct)
                ?? throw new NotFoundException("Address not found.");
        }
        else
        {
            address = new Address
            {
                UserId = userId, Label = "", FullName = "", Phone = "", Line1 = "", City = "", County = ""
            };
            addresses.Add(address);
        }

        address.Label = request.Label.Trim();
        address.FullName = request.FullName.Trim();
        address.Phone = request.Phone.Trim();
        address.Line1 = request.Line1.Trim();
        address.City = request.City.Trim();
        address.County = request.County.Trim();
        address.IsDefault = request.IsDefault;

        if (request.IsDefault)
            foreach (var other in await addresses.ListAsync(a => a.UserId == userId && a.Id != address.Id && a.IsDefault, ct))
                other.IsDefault = false;

        await uow.SaveChangesAsync(ct);
        return ToDto(address);
    }

    public async Task DeleteAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var address = await addresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, ct)
            ?? throw new NotFoundException("Address not found.");
        addresses.Remove(address);
        await uow.SaveChangesAsync(ct);
    }

    private static AddressDto ToDto(Address a) =>
        new(a.Id, a.Label, a.FullName, a.Phone, a.Line1, a.City, a.County, a.IsDefault);
}

public class NotificationService(IRepository<Notification> notifications, IUnitOfWork uow)
{
    public async Task<PagedResult<NotificationDto>> ListAsync(Guid userId, PagingParams paging, CancellationToken ct = default)
    {
        var all = await notifications.ListAsync(n => n.UserId == userId, ct);
        var ordered = all.OrderByDescending(n => n.CreatedAt).ToList();
        var page = ordered.Skip(paging.Skip).Take(paging.SafePageSize)
            .Select(n => new NotificationDto(n.Id, n.Type.ToString(), n.Title, n.Body, n.IsRead, n.CreatedAt))
            .ToList();
        return new PagedResult<NotificationDto>(page, paging.SafePage, paging.SafePageSize, ordered.Count);
    }

    public async Task<int> UnreadCountAsync(Guid userId, CancellationToken ct = default) =>
        await notifications.CountAsync(n => n.UserId == userId && !n.IsRead, ct);

    public async Task MarkReadAsync(Guid userId, IReadOnlyList<Guid>? ids, CancellationToken ct = default)
    {
        var targets = ids is { Count: > 0 }
            ? await notifications.ListAsync(n => n.UserId == userId && ids.Contains(n.Id), ct)
            : await notifications.ListAsync(n => n.UserId == userId && !n.IsRead, ct);
        foreach (var n in targets) n.IsRead = true;
        await uow.SaveChangesAsync(ct);
    }
}
