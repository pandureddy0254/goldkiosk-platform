using GoldKiosk.Infrastructure.Entities.Audit;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Infrastructure.Data;

/// <summary>
/// Audit-schema DbSets. The <c>audit_events</c> table is append-only; never
/// update or delete rows — a DB trigger (<c>db/0201</c>) will raise an
/// exception. Services should only call <c>Add</c> + <c>SaveChanges</c>.
/// </summary>
public partial class AppDbContext
{
    /// <summary>Set.</summary>
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
}
