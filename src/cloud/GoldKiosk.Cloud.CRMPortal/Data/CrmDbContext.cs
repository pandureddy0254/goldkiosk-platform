using GoldKiosk.Cloud.CRMPortal.Models.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json.Linq;

namespace GoldKiosk.Cloud.CRMPortal.Data;

/// <summary>
/// EF Core <see cref="DbContext"/> for the CRM. The SQL schema in <c>db/crm/</c>
/// is the source of truth — EF does NOT generate migrations against this context;
/// the configurations exist only so LINQ queries hit the right tables/columns.
/// <para>
/// Tables live across three schemas: <c>crm</c>, <c>billing</c>, <c>audit</c>.
/// Column names are derived by <c>UseSnakeCaseNamingConvention()</c> (configured
/// in Program.cs); only the handful of properties whose snake_case form differs
/// from the column are mapped explicitly here.
/// </para>
/// </summary>
public class CrmDbContext : DbContext
{
    /// <summary>Initializes the context with host-configured options (Npgsql + snake_case + interceptors).</summary>
    /// <param name="options">The options built by the composition root.</param>
    public CrmDbContext(DbContextOptions<CrmDbContext> options) : base(options)
    {
    }

    /// <summary>crm.profiles — CRM staff users.</summary>
    public DbSet<Profile> Profiles => Set<Profile>();

    /// <summary>crm.leads — pipeline opportunities.</summary>
    public DbSet<Lead> Leads => Set<Lead>();

    /// <summary>crm.lead_activities — per-lead activity timeline.</summary>
    public DbSet<LeadActivity> LeadActivities => Set<LeadActivity>();

    /// <summary>crm.partners — converted partners.</summary>
    public DbSet<Partner> Partners => Set<Partner>();

    /// <summary>crm.tenants — provisioned Admin Dashboard tenants.</summary>
    public DbSet<Tenant> Tenants => Set<Tenant>();

    /// <summary>crm.contracts — legal documents per partner.</summary>
    public DbSet<Contract> Contracts => Set<Contract>();

    /// <summary>crm.activation_keys — issued license key hashes.</summary>
    public DbSet<ActivationKey> ActivationKeys => Set<ActivationKey>();

    /// <summary>crm.user_preferences — per-user UI preferences.</summary>
    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();

    /// <summary>billing.subscriptions — recurring licence subscriptions.</summary>
    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    /// <summary>billing.subscription_periods — invoiced billing periods.</summary>
    public DbSet<SubscriptionPeriod> SubscriptionPeriods => Set<SubscriptionPeriod>();

    /// <summary>billing.renewal_reminders — scheduled renewal notifications.</summary>
    public DbSet<RenewalReminder> RenewalReminders => Set<RenewalReminder>();

    /// <summary>audit.audit_log — append-only compliance trail.</summary>
    public DbSet<AuditLogEntry> AuditLog => Set<AuditLogEntry>();

    // ── jsonb <-> JObject converter (Newtonsoft) ───────────────────────────────
    // Serialise compactly on write; parse back on read. NULL/empty round-trips to
    // a C# null so the computed helpers (BillingAddressLine etc.) behave.
    private static readonly ValueConverter<JObject?, string?> _jsonbConverter = new(
        v => v == null ? null : v.ToString(Newtonsoft.Json.Formatting.None),
        v => string.IsNullOrEmpty(v) ? null : JObject.Parse(v));

    // JObject has no usable structural equality for EF's change tracker; compare
    // on the serialised string and snapshot via a fresh parse.
    private static readonly ValueComparer<JObject?> _jsonbComparer = new(
        (a, b) => (a == null && b == null) ||
                  (a != null && b != null && a.ToString(Newtonsoft.Json.Formatting.None) == b.ToString(Newtonsoft.Json.Formatting.None)),
        v => v == null ? 0 : v.ToString(Newtonsoft.Json.Formatting.None).GetHashCode(StringComparison.Ordinal),
        v => v == null ? null : JObject.Parse(v.ToString(Newtonsoft.Json.Formatting.None)));

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── crm.profiles ────────────────────────────────────────────────────
        modelBuilder.Entity<Profile>(e =>
        {
            e.ToTable("profiles", "crm");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.Email).HasColumnType("citext");
            ServerTimestamps(e.Metadata);
        });

        // ── crm.leads ───────────────────────────────────────────────────────
        modelBuilder.Entity<Lead>(e =>
        {
            e.ToTable("leads", "crm");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.ContactEmail).HasColumnType("citext");
            ServerTimestamps(e.Metadata);
        });

        // ── crm.lead_activities ─────────────────────────────────────────────
        modelBuilder.Entity<LeadActivity>(e =>
        {
            e.ToTable("lead_activities", "crm");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            // activity_type column; the property is named Type to keep the views terse.
            e.Property(x => x.Type).HasColumnName("activity_type");
            e.Property(x => x.Payload)
                .HasColumnName("payload")
                .HasColumnType("jsonb")
                .HasConversion(_jsonbConverter)
                .Metadata.SetValueComparer(_jsonbComparer);
            // occurred_at is set by the DB default; never write it.
            e.Property(x => x.OccurredAt).ValueGeneratedOnAdd();
            IgnoreOnWrite(e.Metadata, nameof(LeadActivity.OccurredAt));
        });

        // ── crm.partners ────────────────────────────────────────────────────
        modelBuilder.Entity<Partner>(e =>
        {
            e.ToTable("partners", "crm");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.PrimaryAdminEmail).HasColumnType("citext");
            e.Property(x => x.BillingAddress)
                .HasColumnName("billing_address")
                .HasColumnType("jsonb")
                .HasConversion(_jsonbConverter)
                .Metadata.SetValueComparer(_jsonbComparer);
            ServerTimestamps(e.Metadata);
        });

        // ── crm.tenants ─────────────────────────────────────────────────────
        modelBuilder.Entity<Tenant>(e =>
        {
            e.ToTable("tenants", "crm");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            ServerTimestamps(e.Metadata);
        });

        // ── crm.contracts ───────────────────────────────────────────────────
        modelBuilder.Entity<Contract>(e =>
        {
            e.ToTable("contracts", "crm");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.ContractTypeCode).HasColumnName("contract_type");
            e.Property(x => x.SignedByEmail).HasColumnType("citext");
            ServerTimestamps(e.Metadata);
        });

        // ── crm.activation_keys ─────────────────────────────────────────────
        modelBuilder.Entity<ActivationKey>(e =>
        {
            e.ToTable("activation_keys", "crm");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.IssuedToEmail).HasColumnType("citext");
            // issued_at / created_at are DB-defaulted.
            e.Property(x => x.IssuedAt).ValueGeneratedOnAdd();
            e.Property(x => x.CreatedAt).ValueGeneratedOnAdd();
            IgnoreOnWrite(e.Metadata, nameof(ActivationKey.CreatedAt));
        });

        // ── crm.user_preferences (PK is user_id, supplied by caller) ─────────
        modelBuilder.Entity<UserPreference>(e =>
        {
            e.ToTable("user_preferences", "crm");
            e.HasKey(x => x.UserId);
            e.Property(x => x.UpdatedAt).ValueGeneratedOnAddOrUpdate();
            IgnoreOnWrite(e.Metadata, nameof(UserPreference.UpdatedAt));
        });

        // ── billing.subscriptions ───────────────────────────────────────────
        modelBuilder.Entity<Subscription>(e =>
        {
            e.ToTable("subscriptions", "billing");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            ServerTimestamps(e.Metadata);
        });

        // ── billing.subscription_periods ────────────────────────────────────
        modelBuilder.Entity<SubscriptionPeriod>(e =>
        {
            e.ToTable("subscription_periods", "billing");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.CreatedAt).ValueGeneratedOnAdd();
            IgnoreOnWrite(e.Metadata, nameof(SubscriptionPeriod.CreatedAt));
        });

        // ── billing.renewal_reminders (scheduled_for is a DATE column) ───────
        modelBuilder.Entity<RenewalReminder>(e =>
        {
            e.ToTable("renewal_reminders", "billing");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.ScheduledFor).HasColumnType("date");
            e.Property(x => x.RecipientEmail).HasColumnType("citext");
            e.Property(x => x.CreatedAt).ValueGeneratedOnAdd();
            IgnoreOnWrite(e.Metadata, nameof(RenewalReminder.CreatedAt));
        });

        // ── audit.audit_log (bigint GENERATED ALWAYS AS IDENTITY pk) ─────────
        modelBuilder.Entity<AuditLogEntry>(e =>
        {
            e.ToTable("audit_log", "audit");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.Before)
                .HasColumnType("jsonb")
                .HasConversion(_jsonbConverter)
                .Metadata.SetValueComparer(_jsonbComparer);
            e.Property(x => x.After)
                .HasColumnType("jsonb")
                .HasConversion(_jsonbConverter)
                .Metadata.SetValueComparer(_jsonbComparer);
            e.Property(x => x.CreatedAt).ValueGeneratedOnAdd();
            IgnoreOnWrite(e.Metadata, nameof(AuditLogEntry.CreatedAt));
        });
    }

    /// <summary>
    /// Marks created_at / updated_at as DB-managed and excludes them from INSERT
    /// and UPDATE statements (the DB defaults + the updated_at trigger own them).
    /// </summary>
    private static void ServerTimestamps(IMutableEntityType entity)
    {
        if (entity.FindProperty("CreatedAt") is { } created)
        {
            created.ValueGenerated = ValueGenerated.OnAdd;
            created.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);
            created.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
        }

        if (entity.FindProperty("UpdatedAt") is { } updated)
        {
            updated.ValueGenerated = ValueGenerated.OnAddOrUpdate;
            updated.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);
            updated.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
        }
    }

    /// <summary>Never write the named property on INSERT or UPDATE (DB-managed).</summary>
    private static void IgnoreOnWrite(IMutableEntityType entity, string propertyName)
    {
        if (entity.FindProperty(propertyName) is { } p)
        {
            p.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);
            p.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
        }
    }
}
