using GoldKiosk.Infrastructure.Entities.Ops;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Infrastructure.Data;

/// <summary>
/// DbSets for the <c>ops</c> schema. Only the entities currently exercised by
/// shipped controllers are registered here; the dedicated Operations module
/// agent will append additional DbSets in a separate pass.
/// </summary>
public partial class AppDbContext
{
    /// <summary>Set.</summary>
    public DbSet<KioskInventorySnapshot> KioskInventorySnapshots => Set<KioskInventorySnapshot>();

    // ─── Operations module — added by the Operations agent ──────────────────
    /// <summary>Set.</summary>
    public DbSet<TechnicianCluster> TechnicianClusters => Set<TechnicianCluster>();
    /// <summary>Set.</summary>
    public DbSet<Technician> Technicians => Set<Technician>();
    /// <summary>Set.</summary>
    public DbSet<DeploymentTicket> DeploymentTickets => Set<DeploymentTicket>();
    /// <summary>Set.</summary>
    public DbSet<MaintenanceTicket> MaintenanceTickets => Set<MaintenanceTicket>();
    /// <summary>Set.</summary>
    public DbSet<TicketStatusHistory> TicketStatusHistory => Set<TicketStatusHistory>();
    /// <summary>Set.</summary>
    public DbSet<CollectionRun> CollectionRuns => Set<CollectionRun>();
    /// <summary>Set.</summary>
    public DbSet<CollectionTicket> CollectionTickets => Set<CollectionTicket>();
    /// <summary>Set.</summary>
    public DbSet<SystemHealthSnapshot> SystemHealthSnapshots => Set<SystemHealthSnapshot>();
}
