using FactoryFlow.Modules.Notifications.Domain.Entities;
using FactoryFlow.SharedKernel.Application;
using FactoryFlow.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;

namespace FactoryFlow.Modules.Notifications.Application.Commands.MarkNotificationRead;

public sealed class MarkNotificationReadCommandHandler
{
    private readonly DbContext _db;
    private readonly ICurrentUserService _currentUser;

    public MarkNotificationReadCommandHandler(DbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result> HandleAsync(Guid notificationId, CancellationToken ct = default)
    {
        if (!_currentUser.IsAuthenticated)
            return Result.Failure("Benutzer ist nicht authentifiziert.");

        var notification = await _db.Set<InternalNotification>()
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.RecipientUserId == _currentUser.UserId, ct);

        if (notification is null)
            return Result.Failure("Benachrichtigung nicht gefunden.");

        notification.MarkAsRead(DateTime.UtcNow);
        await _db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
