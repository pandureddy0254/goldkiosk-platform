-- ============================================================================
-- 0032_seed_rbac_roles_and_permissions.sql
-- Seeds the canonical permission codes (global) and the 6 system roles per
-- tenant (Owner, Manager, Operator, Analyst, Support, Auditor) with their
-- predefined permission grants. Also installs a tenant-bootstrap helper that
-- applies these defaults every time a new tenant is created.
--
-- Permission code grammar: <module>:<verb> where module is the admin-dashboard
-- module slug (matches the sidebar) and verb is one of:
--   read    — view the module
--   write   — create/edit records inside the module
--   delete  — destructive operations on the module
--   admin   — administrative actions (export, revoke, override)
--
-- Idempotent — every INSERT is ON CONFLICT DO NOTHING.
-- ============================================================================

\echo '── 0032 seed RBAC catalogue ──'

-- ─── 1. Global permission catalogue ────────────────────────────────────────
INSERT INTO identity.permissions (code, display_name, description, scope) VALUES
    -- Dashboard / overview
    ('dashboard:read',         'View dashboard',              'See the operator home and KPIs.',                      'tenant'),

    -- Fleet
    ('kiosks:read',            'View kiosks',                 'See kiosk list, status, and detail pages.',           'tenant'),
    ('kiosks:write',           'Manage kiosks',               'Create, configure, suspend kiosks.',                  'tenant'),
    ('kiosks:admin',           'Decommission kiosks',         'Permanently retire kiosks and rotate hardware.',      'tenant'),
    ('users:read',             'View users',                  'See the user roster.',                                'tenant'),
    ('users:write',            'Manage users',                'Invite, edit, suspend users.',                        'tenant'),
    ('users:admin',            'Assign elevated roles',       'Grant Manager/Analyst/Auditor roles.',                'tenant'),
    ('customers:read',         'View customers',              'See customer records (PII masked by default).',       'tenant'),
    ('customers:write',        'Edit customers',              'Update customer details and KYC notes.',              'tenant'),
    ('customers:unmask',       'Unmask customer PII',         'Reveal full PII (reason logged to audit).',           'tenant'),
    ('merchants:read',         'View merchants',              'See merchant directory and wallets.',                 'tenant'),
    ('merchants:write',        'Manage merchants',            'Onboard merchants, approve top-ups.',                 'tenant'),

    -- Commerce
    ('sales:read',             'View sales',                  'See sales register and pawn ledgers.',                'tenant'),
    ('sales:write',            'Edit sales',                  'Adjust offers, mark sales reviewed.',                 'tenant'),
    ('vouchers:read',          'View vouchers',               'See voucher catalogue + redemptions.',                'tenant'),
    ('vouchers:write',         'Manage vouchers',             'Create + edit vouchers, define policies.',            'tenant'),
    ('reports:read',           'View reports',                'Run the 9 matview reports.',                          'tenant'),
    ('reports:export',         'Export reports',              'Download report CSVs.',                               'tenant'),

    -- Support & ops
    ('helpdesk:read',          'View tickets',                'See helpdesk ticket queue.',                          'tenant'),
    ('helpdesk:write',         'Manage tickets',              'Own, transition, resolve tickets.',                   'tenant'),
    ('sos:read',               'View SOS',                    'See the SOS request queue.',                          'tenant'),
    ('sos:write',              'Respond to SOS',              'Acknowledge / dispatch / resolve SOS.',               'tenant'),
    ('feedback:read',          'View feedback',               'Read customer feedback inbox.',                       'tenant'),
    ('feedback:write',         'Reply to feedback',           'Send replies to customer feedback.',                  'tenant'),
    ('audit:read',             'View audit log',              'Read immutable audit stream.',                        'tenant'),
    ('audit:export',           'Export audit log',            'Download audit CSV for compliance.',                  'tenant'),
    ('monitoring:read',        'View monitoring',             'See API health, exceptions, activity feeds.',         'tenant'),
    ('operations:read',        'View operations',             'See deployment, maintenance, technician queues.',     'tenant'),
    ('operations:write',       'Manage operations',           'Dispatch field tickets, update technicians.',         'tenant'),

    -- Administration
    ('roles:read',             'View roles',                  'See the RBAC matrix and role definitions.',           'tenant'),
    ('roles:write',            'Manage roles',                'Edit role permission grants (custom roles).',         'tenant'),
    ('tenant:read',            'View tenant settings',        'See tenant configuration.',                           'tenant'),
    ('tenant:write',           'Manage tenant settings',      'Edit branding, configs, feature flags.',              'tenant'),
    ('tenant:admin',           'Tenant administration',       'Billing, legal entity, data-residency changes.',      'tenant'),

    -- Integrations / API
    ('api_credentials:read',   'View API credentials',        'See partner app_id list (never app_key).',            'tenant'),
    ('api_credentials:write',  'Manage API credentials',      'Generate, label, rotate API credentials.',            'tenant'),
    ('api_credentials:admin',  'Revoke API credentials',      'Revoke partner-generated app_id/app_key pairs.',      'tenant'),
    ('webhooks:read',          'View webhooks',               'See webhook endpoints and deliveries.',               'tenant'),
    ('webhooks:write',         'Manage webhooks',             'Create webhook endpoints, rotate signing keys.',      'tenant')
ON CONFLICT (code) DO NOTHING;

\echo '  ✓ permission catalogue seeded'

-- ─── 2. Role + permission templates ────────────────────────────────────────
-- We model the 6 canonical roles as a JSON blueprint here, then expand them
-- via a helper function whenever a tenant is created (or via the bootstrap
-- block at the bottom of this file for existing tenants).

CREATE TABLE IF NOT EXISTS identity.role_templates (
    code         citext PRIMARY KEY,
    display_name text NOT NULL,
    description  text NOT NULL,
    sort_order   integer NOT NULL,
    permissions  jsonb NOT NULL   -- array of permission codes
);

INSERT INTO identity.role_templates (code, display_name, description, sort_order, permissions) VALUES
    ('owner', 'Owner', 'Primary admin. Full access. One per tenant — bound to the activation key.', 10,
        '["dashboard:read","kiosks:read","kiosks:write","kiosks:admin","users:read","users:write","users:admin","customers:read","customers:write","customers:unmask","merchants:read","merchants:write","sales:read","sales:write","vouchers:read","vouchers:write","reports:read","reports:export","helpdesk:read","helpdesk:write","sos:read","sos:write","feedback:read","feedback:write","audit:read","audit:export","monitoring:read","operations:read","operations:write","roles:read","roles:write","tenant:read","tenant:write","tenant:admin","api_credentials:read","api_credentials:write","api_credentials:admin","webhooks:read","webhooks:write"]'::jsonb),

    ('manager', 'Manager', 'Branch / regional manager. Operates day-to-day, cannot change roles or tenant settings.', 20,
        '["dashboard:read","kiosks:read","kiosks:write","users:read","customers:read","customers:write","merchants:read","merchants:write","sales:read","sales:write","vouchers:read","vouchers:write","reports:read","reports:export","helpdesk:read","helpdesk:write","sos:read","sos:write","feedback:read","feedback:write","audit:read","monitoring:read","operations:read","operations:write","roles:read","tenant:read","api_credentials:read","webhooks:read"]'::jsonb),

    ('operator', 'Operator', 'Floor staff. Reads fleet + customers, processes transactions, raises tickets.', 30,
        '["dashboard:read","kiosks:read","customers:read","sales:read","helpdesk:read","helpdesk:write","sos:read","feedback:read"]'::jsonb),

    ('analyst', 'Analyst', 'Finance / compliance read-everywhere. Runs reports, exports CSV. No write.', 40,
        '["dashboard:read","kiosks:read","users:read","customers:read","merchants:read","sales:read","vouchers:read","reports:read","reports:export","helpdesk:read","sos:read","feedback:read","audit:read","audit:export","monitoring:read","operations:read","roles:read","tenant:read","api_credentials:read","webhooks:read"]'::jsonb),

    ('support', 'Support', 'Helpdesk agent. Owns tickets, SOS workflow, customer feedback. May unmask PII (logged).', 50,
        '["dashboard:read","kiosks:read","customers:read","customers:unmask","helpdesk:read","helpdesk:write","sos:read","sos:write","feedback:read","feedback:write"]'::jsonb),

    ('auditor', 'Auditor', 'External / time-boxed auditor. Audit log + reports read-only. Cannot see PII.', 60,
        '["dashboard:read","reports:read","audit:read","audit:export","monitoring:read"]'::jsonb)
ON CONFLICT (code) DO UPDATE SET
    display_name = EXCLUDED.display_name,
    description  = EXCLUDED.description,
    sort_order   = EXCLUDED.sort_order,
    permissions  = EXCLUDED.permissions;

\echo '  ✓ 6 role templates seeded'

-- ─── 3. Helper: apply system roles to a tenant ──────────────────────────────
-- Inserts the 6 system roles for the given tenant (idempotent) and wires their
-- role_permissions rows from the role_templates blueprint. Called by:
--   a) tenancy.provision_tenant() at tenant-creation time (out of scope here)
--   b) the bootstrap loop at the bottom of THIS file for existing tenants
CREATE OR REPLACE FUNCTION identity.apply_system_roles(p_tenant_id uuid)
RETURNS void
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = identity, tenancy, public, pg_temp
AS $$
DECLARE
    v_template record;
    v_role_id  uuid;
    v_perm_code text;
BEGIN
    FOR v_template IN
        SELECT code, display_name, description, permissions
        FROM identity.role_templates
        ORDER BY sort_order
    LOOP
        -- upsert the role (system roles are flagged is_system = true so the UI
        -- can render them differently and block deletion)
        INSERT INTO identity.roles (tenant_id, code, name, description, is_system, is_active)
        VALUES (p_tenant_id, v_template.code, v_template.display_name, v_template.description, true, true)
        ON CONFLICT (tenant_id, code) DO UPDATE SET
            name        = EXCLUDED.name,
            description = EXCLUDED.description,
            is_system   = true
        RETURNING id INTO v_role_id;

        -- expand the permissions array into role_permissions rows
        FOR v_perm_code IN
            SELECT jsonb_array_elements_text(v_template.permissions)
        LOOP
            INSERT INTO identity.role_permissions (role_id, permission_id)
            SELECT v_role_id, p.id
              FROM identity.permissions p
             WHERE p.code = v_perm_code
            ON CONFLICT (role_id, permission_id) DO NOTHING;
        END LOOP;
    END LOOP;
END;
$$;

COMMENT ON FUNCTION identity.apply_system_roles(uuid) IS
    'Creates / refreshes the 6 canonical system roles (Owner..Auditor) for a tenant '
    'with their predefined permission grants. Idempotent. Call from tenant provisioning.';

\echo '  ✓ apply_system_roles() installed'

-- ─── 4. Bootstrap existing tenants ──────────────────────────────────────────
-- Apply the system roles to every existing tenant so the matrix is consistent
-- everywhere immediately after migration.
DO $$
DECLARE
    t record;
BEGIN
    FOR t IN SELECT id FROM tenancy.tenants WHERE deleted_at IS NULL LOOP
        PERFORM identity.apply_system_roles(t.id);
    END LOOP;
    RAISE NOTICE '  ✓ system roles applied to all existing tenants';
END $$;

\echo '✓ 0032 done.'
