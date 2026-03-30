using FactoryFlow.Modules.Identity.Domain.Entities;
using FactoryFlow.Modules.Tickets.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FactoryFlow.Modules.Tickets.Application.Queries.GetTicketDetail;

public sealed class GetTicketDetailQueryHandler
{
    private readonly DbContext _db;

    public GetTicketDetailQueryHandler(DbContext db)
    {
        _db = db;
    }

    public async Task<TicketDetailDto?> HandleAsync(Guid ticketId, CancellationToken ct = default)
    {
        var detail = await (
            from t in _db.Set<Ticket>()
            join d in _db.Set<Department>() on t.DepartmentId equals d.Id into departments
            from d in departments.DefaultIfEmpty()
            join s in _db.Set<Site>() on t.SiteId equals s.Id into sites
            from s in sites.DefaultIfEmpty()
            join u in _db.Set<ApplicationUser>() on t.CreatedByUserId equals u.Id into users
            from u in users.DefaultIfEmpty()
            where t.Id == ticketId
            select new
            {
                t.Id,
                t.TicketNumber,
                t.Title,
                t.Description,
                TicketTypeName = t.TicketType!.Name,
                PriorityName = t.Priority!.Name,
                t.StatusId,
                StatusName = t.Status!.Name,
                DepartmentName = d != null ? d.Name : null,
                SiteName = s != null ? s.Name : null,
                t.MachineOrWorkstation,
                t.CreatedAtUtc,
                CreatedByDisplayName = u != null ? (u.FirstName + " " + u.LastName).Trim() : t.CreatedByUserId
            }
        ).FirstOrDefaultAsync(ct);

        if (detail is null)
            return null;

        var comments = await (
            from c in _db.Set<TicketComment>()
            join u in _db.Set<ApplicationUser>() on c.CreatedByUserId equals u.Id into users
            from u in users.DefaultIfEmpty()
            where c.TicketId == ticketId
            orderby c.CreatedAtUtc descending
            select new TicketCommentDto(
                c.Id,
                c.Text,
                c.CreatedAtUtc,
                u != null ? (u.FirstName + " " + u.LastName).Trim() : c.CreatedByUserId)
        ).ToListAsync(ct);

        return new TicketDetailDto(
            detail.Id,
            detail.TicketNumber,
            detail.Title,
            detail.Description,
            detail.TicketTypeName,
            detail.PriorityName,
            detail.StatusId,
            detail.StatusName,
            detail.DepartmentName,
            detail.SiteName,
            detail.MachineOrWorkstation,
            detail.CreatedAtUtc,
            detail.CreatedByDisplayName,
            comments);
    }
}
