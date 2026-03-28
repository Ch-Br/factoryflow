namespace FactoryFlow.Modules.Tickets.Domain.Services;

/// <summary>
/// Generates human-readable ticket numbers using a PostgreSQL sequence
/// to guarantee uniqueness under concurrent inserts.
/// Format: FF-{year}-{6-digit sequence}, e.g. FF-2026-000042.
/// </summary>
public interface ITicketNumberGenerator
{
    Task<string> GenerateAsync(CancellationToken ct = default);
}
