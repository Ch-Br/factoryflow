using FactoryFlow.Modules.Notifications.Domain.Entities;
using FactoryFlow.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;

namespace FactoryFlow.Modules.Notifications.Application.Queries.GetMyNotifications;

public sealed class GetMyNotificationsQueryHandler
{
    private readonly DbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetMyNotificationsQueryHandler(DbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<NotificationListResultDto> HandleAsync(CancellationToken ct = default)
    {
        if (!_currentUser.IsAuthenticated)
            return new NotificationListResultDto([], 0);

        var items = await _db.Set<InternalNotification>()
            .Where(n => n.RecipientUserId == _currentUser.UserId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Select(n => new NotificationDto(
                n.Id,
                n.NotificationType,
                n.TicketId,
                n.TicketNumber,
                n.Title,
                n.EscalationLevel,
                n.CreatedAtUtc,
                n.ReadAtUtc))
            .ToListAsync(ct);

        return new NotificationListResultDto(items, items.Count);
    }
}
