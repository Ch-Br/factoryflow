using FactoryFlow.Modules.Tickets.Domain.Enums;

namespace FactoryFlow.Modules.Tickets.Domain.Services;

public static class DueStateCalculator
{
    public static readonly TimeSpan DueSoonThreshold = TimeSpan.FromHours(24);

    public static DueState Calculate(DateTime? dueAtUtc, bool isClosed, DateTime utcNow)
    {
        if (dueAtUtc is null)
            return DueState.NoDueDate;

        if (isClosed)
            return DueState.OnTrack;

        if (dueAtUtc.Value <= utcNow)
            return DueState.Overdue;

        if (dueAtUtc.Value <= utcNow + DueSoonThreshold)
            return DueState.DueSoon;

        return DueState.OnTrack;
    }
}
