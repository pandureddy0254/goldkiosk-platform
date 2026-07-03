-- ============================================================================
-- 0011_tables_audit.sql
-- Schema: audit
-- Append-only compliance trail. Immutability is enforced by the
-- fn_reject_modify trigger (attached in 0201) AND by RLS (0600).
-- ============================================================================

\echo '-- 0011 audit --'

CREATE TABLE IF NOT EXISTS audit.audit_log (
    id          bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    actor_id    uuid             NULL,
    entity_type text             NOT NULL,
    entity_id   uuid             NULL,
    action      text             NOT NULL,
    before      jsonb            NULL,
    after       jsonb            NULL,
    created_at  timestamptz      NOT NULL DEFAULT now(),
    CONSTRAINT fk_audit_log__actor FOREIGN KEY (actor_id) REFERENCES crm.profiles(id) ON DELETE SET NULL,
    CONSTRAINT ck_audit_log__entity_type CHECK (entity_type IN ('lead','partner','tenant','activation_key','contract','profile')),
    CONSTRAINT ck_audit_log__action CHECK (action IN ('create','update','delete','provision','revoke_key','reissue_key','sign_in','export'))
);
CREATE INDEX IF NOT EXISTS ix_audit_log__entity ON audit.audit_log (entity_type, entity_id, created_at DESC);
CREATE INDEX IF NOT EXISTS ix_audit_log__actor  ON audit.audit_log (actor_id, created_at DESC);
COMMENT ON TABLE audit.audit_log IS 'Append-only audit trail. Immutable by trigger; readable by sales_manager only (RLS).';

\echo 'ok: 0011 audit done.'
