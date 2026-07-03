-- ============================================================================
-- 0028_extend_users_and_identity_aux.sql
-- Adds the ASP.NET Core Identity-required columns to identity.users and
-- creates the three auxiliary Identity tables (claims, logins, tokens).
-- We deliberately skip identity.aspnet_role_claims because we don't use
-- claims-based role authorization — permissions are looked up directly
-- from identity.role_permissions.
-- Idempotent.
-- ============================================================================

\echo '── 0028 extend identity.users + aux identity tables ──'

-- ─── Extra columns on identity.users (Identity needs these) ──────────────────
ALTER TABLE identity.users
    ADD COLUMN IF NOT EXISTS normalized_email      citext       NULL,
    ADD COLUMN IF NOT EXISTS normalized_user_name  citext       NULL,
    ADD COLUMN IF NOT EXISTS email_confirmed       boolean      NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS phone_number_confirmed boolean     NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS two_factor_enabled    boolean      NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS lockout_enabled       boolean      NOT NULL DEFAULT true,
    ADD COLUMN IF NOT EXISTS lockout_end           timestamptz  NULL,
    ADD COLUMN IF NOT EXISTS access_failed_count   integer      NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS security_stamp        text         NULL,
    ADD COLUMN IF NOT EXISTS concurrency_stamp     text         NULL,
    ADD COLUMN IF NOT EXISTS user_name             citext       NULL;  -- Identity's UserName; we keep it in sync with email

-- Index on normalized_email used by Identity's user-by-email lookup
CREATE UNIQUE INDEX IF NOT EXISTS ux_users__normalized_email
    ON identity.users(normalized_email)
    WHERE deleted_at IS NULL;

CREATE INDEX IF NOT EXISTS ix_users__normalized_user_name
    ON identity.users(normalized_user_name)
    WHERE deleted_at IS NULL;

-- Backfill: where Identity columns are null, derive them from email
UPDATE identity.users
   SET user_name             = email,
       normalized_user_name  = upper(email::text)::citext,
       normalized_email      = upper(email::text)::citext,
       security_stamp        = gen_random_uuid()::text,
       concurrency_stamp     = gen_random_uuid()::text
 WHERE user_name IS NULL OR normalized_email IS NULL;

\echo '  ✓ identity.users extended'

-- ─── identity.aspnet_user_claims ────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS identity.aspnet_user_claims (
    id           integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id      uuid NOT NULL,
    claim_type   text NULL,
    claim_value  text NULL,
    CONSTRAINT fk_aspnet_user_claims__users FOREIGN KEY (user_id) REFERENCES identity.users(id) ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS ix_aspnet_user_claims__user ON identity.aspnet_user_claims(user_id);

-- ─── identity.aspnet_user_logins ────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS identity.aspnet_user_logins (
    login_provider          text NOT NULL,
    provider_key            text NOT NULL,
    provider_display_name   text NULL,
    user_id                 uuid NOT NULL,
    CONSTRAINT pk_aspnet_user_logins PRIMARY KEY (login_provider, provider_key),
    CONSTRAINT fk_aspnet_user_logins__users FOREIGN KEY (user_id) REFERENCES identity.users(id) ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS ix_aspnet_user_logins__user ON identity.aspnet_user_logins(user_id);

-- ─── identity.aspnet_user_tokens ────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS identity.aspnet_user_tokens (
    user_id         uuid NOT NULL,
    login_provider  text NOT NULL,
    name            text NOT NULL,
    value           text NULL,
    CONSTRAINT pk_aspnet_user_tokens PRIMARY KEY (user_id, login_provider, name),
    CONSTRAINT fk_aspnet_user_tokens__users FOREIGN KEY (user_id) REFERENCES identity.users(id) ON DELETE CASCADE
);

\echo '✓ 0028 done.'
