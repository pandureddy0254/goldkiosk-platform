using Newtonsoft.Json.Linq;

namespace GoldKiosk.Cloud.CRMPortal.Models.Domain;

/// <summary>Mirrors <c>crm.entity_type</c> (text column).</summary>
public static class AuditEntityType
{
    /// <summary>Audit rows about a lead.</summary>
    public const string Lead = "lead";

    /// <summary>Audit rows about a partner.</summary>
    public const string Partner = "partner";

    /// <summary>Audit rows about a tenant.</summary>
    public const string Tenant = "tenant";

    /// <summary>Audit rows about an activation key.</summary>
    public const string ActivationKey = "activation_key";

    /// <summary>Audit rows about a contract.</summary>
    public const string Contract = "contract";

    /// <summary>Audit rows about a staff profile.</summary>
    public const string Profile = "profile";
}

/// <summary>Mirrors <c>crm.audit_action</c> (text column).</summary>
public static class AuditAction
{
    /// <summary>Entity created.</summary>
    public const string Create = "create";

    /// <summary>Entity updated.</summary>
    public const string Update = "update";

    /// <summary>Entity deleted.</summary>
    public const string Delete = "delete";

    /// <summary>Tenant/license provisioned.</summary>
    public const string Provision = "provision";

    /// <summary>Activation key revoked.</summary>
    public const string RevokeKey = "revoke_key";

    /// <summary>Activation key re-issued.</summary>
    public const string ReissueKey = "reissue_key";

    /// <summary>User signed in.</summary>
    public const string SignIn = "sign_in";

    /// <summary>Data exported.</summary>
    public const string Export = "export";
}

/// <summary>Maps to <c>audit.audit_log</c> — the append-only compliance trail.</summary>
public class AuditLogEntry
{
    /// <summary>Primary key (bigint GENERATED ALWAYS AS IDENTITY).</summary>
    public long Id { get; set; }

    /// <summary>The staff profile that performed the action, or <c>null</c> for system actions.</summary>
    public Guid? ActorId { get; set; }

    /// <summary>Kind of entity affected — one of <see cref="AuditEntityType"/>.</summary>
    public string EntityType { get; set; } = AuditEntityType.Lead;

    /// <summary>Id of the affected entity, when applicable.</summary>
    public Guid? EntityId { get; set; }

    /// <summary>What happened — one of <see cref="AuditAction"/>.</summary>
    public string Action { get; set; } = AuditAction.Create;

    /// <summary>
    /// Entity snapshot before the change. jsonb in Postgres — <see cref="JObject"/> keeps the
    /// deserializer from trying to fit a JSON object into a string.
    /// </summary>
    public JObject? Before { get; set; }

    /// <summary>Entity snapshot after the change (jsonb, see <see cref="Before"/>).</summary>
    public JObject? After { get; set; }

    /// <summary>Row creation timestamp (DB default; never written by the app).</summary>
    public DateTime CreatedAt { get; set; }
}
