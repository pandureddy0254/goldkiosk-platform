-- ============================================================================
-- 0300_functions_tenancy.sql
-- Tenant context + gap-less invoice numbering.
-- Functions:
--   tenancy.set_tenant_context(tenant_id)
--   tenancy.set_actor_context(user_id, label)
--   tenancy.current_tenant_id()
--   tenancy.next_invoice_number(tenant_id, sequence_code)
-- ============================================================================

\echo '── 0300 functions: tenancy ──'

-- ─── set the tenant GUC for RLS scoping ─────────────────────────────────────
CREATE OR REPLACE FUNCTION tenancy.set_tenant_context(p_tenant_id uuid)
RETURNS void
LANGUAGE plpgsql
SECURITY DEFINER
AS $$
BEGIN
    PERFORM set_config('app.tenant_id', p_tenant_id::text, true);  -- LOCAL = true
END;
$$;

COMMENT ON FUNCTION tenancy.set_tenant_context(uuid)
  IS 'Sets app.tenant_id for the current transaction (LOCAL). RLS policies read this.';

-- ─── set the actor for audit triggers ───────────────────────────────────────
CREATE OR REPLACE FUNCTION tenancy.set_actor_context(p_user_id uuid, p_label text DEFAULT NULL)
RETURNS void
LANGUAGE plpgsql
SECURITY DEFINER
AS $$
BEGIN
    PERFORM set_config('app.actor_user_id', COALESCE(p_user_id::text, ''), true);
    PERFORM set_config('app.actor_label',   COALESCE(p_label, ''),          true);
END;
$$;

-- ─── read the current tenant GUC ────────────────────────────────────────────
CREATE OR REPLACE FUNCTION tenancy.current_tenant_id()
RETURNS uuid
LANGUAGE sql
STABLE
AS $$
    SELECT NULLIF(current_setting('app.tenant_id', true), '')::uuid;
$$;

-- ─── gap-less invoice numbering ─────────────────────────────────────────────
CREATE OR REPLACE FUNCTION tenancy.next_invoice_number(p_tenant_id uuid, p_sequence_code text)
RETURNS text
LANGUAGE plpgsql
AS $$
DECLARE
    v_next   bigint;
    v_prefix text;
BEGIN
    -- Lock the row to prevent concurrent assignments grabbing the same number.
    UPDATE doc.invoice_numbers
       SET next_value = next_value + 1
     WHERE tenant_id     = p_tenant_id
       AND sequence_code = p_sequence_code
    RETURNING next_value - 1, prefix INTO v_next, v_prefix;

    IF NOT FOUND THEN
        INSERT INTO doc.invoice_numbers(tenant_id, sequence_code, next_value, prefix)
        VALUES (p_tenant_id, p_sequence_code, 2, NULL)
        RETURNING 1, prefix INTO v_next, v_prefix;
    END IF;

    RETURN COALESCE(v_prefix || '-', '') || to_char(v_next, 'FM000000000');
END;
$$;

COMMENT ON FUNCTION tenancy.next_invoice_number(uuid, text)
  IS 'Returns the next gap-less invoice number for a tenant + sequence code. Row-locks the counter row.';

\echo '✓ 0300 done.'
