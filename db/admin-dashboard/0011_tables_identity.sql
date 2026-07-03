-- ============================================================================
-- 0011_tables_identity.sql
-- Schema: identity
-- Internal users, MFA, sessions, RBAC.
-- Tables: users, user_mfa, user_sessions, roles, permissions,
--         role_permissions, user_roles, modules
-- ============================================================================

\echo '── 0011 identity ──'

-- ─── users ──────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS identity.users (
    id                       uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id                uuid NOT NULL,
    external_subject         text NULL,
    email                    public.domain_email NOT NULL,
    mobile                   text NULL,
    first_name               text NOT NULL,
    last_name                text NOT NULL,
    password_hash            text NULL,
    password_reset_required  boolean NOT NULL DEFAULT false,
    otp_mode                 text NOT NULL DEFAULT 'off',
    status                   text NOT NULL DEFAULT 'invited',
    locale                   text NOT NULL DEFAULT 'en',
    last_signed_in_at        timestamptz NULL,
    created_at               timestamptz NOT NULL DEFAULT now(),
    updated_at               timestamptz NOT NULL DEFAULT now(),
    deleted_at               timestamptz NULL,
    CONSTRAINT fk_users__tenants FOREIGN KEY (tenant_id) REFERENCES tenancy.tenants(id) ON DELETE CASCADE,
    CONSTRAINT ck_users__otp_mode CHECK (otp_mode IN ('email','sms','totp','webauthn','off')),
    CONSTRAINT ck_users__status   CHECK (status   IN ('invited','active','suspended','locked'))
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_users__tenant_email ON identity.users(tenant_id, email) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_users__tenant ON identity.users(tenant_id);

-- ─── user_mfa ───────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS identity.user_mfa (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id         uuid NOT NULL,
    method          text NOT NULL,
    secret_enc      bytea NULL,
    public_key_enc  bytea NULL,
    is_primary      boolean NOT NULL DEFAULT false,
    enrolled_at     timestamptz NOT NULL DEFAULT now(),
    last_used_at    timestamptz NULL,
    revoked_at      timestamptz NULL,
    CONSTRAINT fk_user_mfa__users FOREIGN KEY (user_id) REFERENCES identity.users(id) ON DELETE CASCADE,
    CONSTRAINT ck_user_mfa__method CHECK (method IN ('totp','webauthn','sms'))
);

CREATE INDEX IF NOT EXISTS ix_user_mfa__user ON identity.user_mfa(user_id) WHERE revoked_at IS NULL;

-- ─── user_sessions ──────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS identity.user_sessions (
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id       uuid NOT NULL,
    token_hash    bytea NOT NULL,
    issued_at     timestamptz NOT NULL DEFAULT now(),
    expires_at    timestamptz NOT NULL,
    last_seen_at  timestamptz NOT NULL DEFAULT now(),
    revoked_at    timestamptz NULL,
    source_ip     inet NULL,
    user_agent    text NULL,
    CONSTRAINT fk_user_sessions__users FOREIGN KEY (user_id) REFERENCES identity.users(id) ON DELETE CASCADE,
    CONSTRAINT uq_user_sessions__token UNIQUE (token_hash)
);

CREATE INDEX IF NOT EXISTS ix_user_sessions__user ON identity.user_sessions(user_id) WHERE revoked_at IS NULL;

-- ─── roles ──────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS identity.roles (
    id           uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id    uuid NOT NULL,
    code         citext NOT NULL,
    name         text NOT NULL,
    description  text NULL,
    is_system    boolean NOT NULL DEFAULT false,
    is_active    boolean NOT NULL DEFAULT true,
    created_at   timestamptz NOT NULL DEFAULT now(),
    deleted_at   timestamptz NULL,
    CONSTRAINT fk_roles__tenants FOREIGN KEY (tenant_id) REFERENCES tenancy.tenants(id) ON DELETE CASCADE,
    CONSTRAINT uq_roles__tenant_code UNIQUE (tenant_id, code)
);

-- ─── permissions (global) ───────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS identity.permissions (
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    code          citext NOT NULL,
    display_name  text NOT NULL,
    description   text NULL,
    scope         text NOT NULL,
    CONSTRAINT uq_permissions__code UNIQUE (code),
    CONSTRAINT ck_permissions__scope CHECK (scope IN ('tenant','store','kiosk'))
);

-- ─── role_permissions ───────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS identity.role_permissions (
    role_id       uuid NOT NULL,
    permission_id uuid NOT NULL,
    granted_at    timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT pk_role_permissions PRIMARY KEY (role_id, permission_id),
    CONSTRAINT fk_role_permissions__roles       FOREIGN KEY (role_id)       REFERENCES identity.roles(id) ON DELETE CASCADE,
    CONSTRAINT fk_role_permissions__permissions FOREIGN KEY (permission_id) REFERENCES identity.permissions(id) ON DELETE CASCADE
);

-- ─── user_roles ─────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS identity.user_roles (
    id                   uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id              uuid NOT NULL,
    role_id              uuid NOT NULL,
    store_id             uuid NULL,
    granted_at           timestamptz NOT NULL DEFAULT now(),
    granted_by_user_id   uuid NULL,
    expires_at           timestamptz NULL,
    revoked_at           timestamptz NULL,
    CONSTRAINT fk_user_roles__users FOREIGN KEY (user_id) REFERENCES identity.users(id) ON DELETE CASCADE,
    CONSTRAINT fk_user_roles__roles FOREIGN KEY (role_id) REFERENCES identity.roles(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ix_user_roles__user ON identity.user_roles(user_id) WHERE revoked_at IS NULL;

-- ─── modules (admin dashboard registry) ─────────────────────────────────────
CREATE TABLE IF NOT EXISTS identity.modules (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    controller      text NOT NULL,
    action          text NOT NULL,
    module_name     text NOT NULL,
    sub_module      text NULL,
    sub_sub_module  text NULL,
    type            text NOT NULL DEFAULT 'page',
    is_active       boolean NOT NULL DEFAULT true,
    CONSTRAINT uq_modules__controller_action UNIQUE (controller, action),
    CONSTRAINT ck_modules__type CHECK (type IN ('page','api','job'))
);

\echo '✓ 0011 identity done.'
