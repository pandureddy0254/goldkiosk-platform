using GoldKiosk.Infrastructure.Entities.Helpdesk;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Infrastructure.Data;

/// <summary>
/// Helpdesk-schema DbSets. Declared on the partial <see cref="AppDbContext"/>
/// so each domain module owns its own surface area.
/// </summary>
public partial class AppDbContext
{
    /// <summary>Set.</summary>
    public DbSet<TicketCategory> TicketCategories => Set<TicketCategory>();
    /// <summary>Set.</summary>
    public DbSet<TicketSubCategory> TicketSubCategories => Set<TicketSubCategory>();
    /// <summary>Set.</summary>
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
    /// <summary>Set.</summary>
    public DbSet<SosRequest> SosRequests => Set<SosRequest>();
    /// <summary>Set.</summary>
    public DbSet<Feedback> Feedback => Set<Feedback>();
}
