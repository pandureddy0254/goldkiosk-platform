-- ============================================================================
-- 0012_tables_billing.sql
-- Schema: billing
-- Tables: subscriptions, subscription_periods, renewal_reminders,
--         plan_catalog, stripe_event_log, ses_message_log
-- (Consolidates the Supabase 0500_subscriptions + 0600_stripe_ses migrations:
--  external_subscription_id is folded straight into subscriptions here.)
-- ============================================================================

\echo '-- 0012 billing --'

-- --- subscriptions (one row per partner; the live billing record) -----------
CREATE TABLE IF NOT EXISTS billing.subscriptions (
    id                        uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    partner_id                uuid                    NOT NULL,
    plan_code                 text                    NOT NULL DEFAULT 'basic',
    annual_amount             numeric(14,2)           NOT NULL,
    currency_code             text                    NOT NULL DEFAULT 'USD',
    billing_cycle             text                    NOT NULL DEFAULT 'annual',
    status                    text                    NOT NULL DEFAULT 'trialing',
    auto_renew                boolean                 NOT NULL DEFAULT true,
    payment_method            text                    NOT NULL DEFAULT 'bank_transfer',
    started_at                timestamptz             NOT NULL DEFAULT now(),
    current_period_start      timestamptz             NOT NULL DEFAULT now(),
    current_period_end        timestamptz             NOT NULL,
    trial_ends_at             timestamptz             NULL,
    cancelled_at              timestamptz             NULL,
    cancellation_reason       text                    NULL,
    cancellation_effective_at timestamptz             NULL,
    last_renewal_at           timestamptz             NULL,
    external_customer_id      text                    NULL,   -- e.g. Stripe customer id
    external_subscription_id  text                    NULL,   -- Stripe subscription id (sub_...)
    notes                     text                    NULL,
    created_at                timestamptz             NOT NULL DEFAULT now(),
    updated_at                timestamptz             NOT NULL DEFAULT now(),
    CONSTRAINT uq_subscriptions__partner UNIQUE (partner_id),
    CONSTRAINT fk_subscriptions__partners FOREIGN KEY (partner_id) REFERENCES crm.partners(id) ON DELETE CASCADE,
    CONSTRAINT ck_subscriptions__currency_supported CHECK (currency_code IN ('USD','AED','INR','EUR','GBP','SAR','QAR','KWD','BHD','OMR')),
    CONSTRAINT ck_subscriptions__billing_cycle CHECK (billing_cycle IN ('monthly','quarterly','annual','biennial')),
    CONSTRAINT ck_subscriptions__status CHECK (status IN ('trialing','active','past_due','suspended','cancelled','expired')),
    CONSTRAINT ck_subscriptions__payment_method CHECK (payment_method IN ('bank_transfer','card','invoice','crypto'))
);
CREATE INDEX IF NOT EXISTS ix_subscriptions__status     ON billing.subscriptions (status);
CREATE INDEX IF NOT EXISTS ix_subscriptions__period_end ON billing.subscriptions (current_period_end);
CREATE INDEX IF NOT EXISTS ix_subscriptions__partner    ON billing.subscriptions (partner_id);
CREATE UNIQUE INDEX IF NOT EXISTS ux_subscriptions__external_sub
  ON billing.subscriptions (external_subscription_id) WHERE external_subscription_id IS NOT NULL;
COMMENT ON TABLE billing.subscriptions IS 'Partner subscriptions. One row per partner. current_period_end is the renewal anchor.';

-- --- subscription_periods (append-only-ish billing history) ------------------
CREATE TABLE IF NOT EXISTS billing.subscription_periods (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    subscription_id     uuid                          NOT NULL,
    period_start        timestamptz                   NOT NULL,
    period_end          timestamptz                   NOT NULL,
    amount              numeric(14,2)                 NOT NULL,
    currency_code       text                          NOT NULL DEFAULT 'USD',
    status              text                          NOT NULL DEFAULT 'pending',
    external_invoice_id text                          NULL,
    external_payment_id text                          NULL,
    invoiced_at         timestamptz                   NULL,
    paid_at             timestamptz                   NULL,
    refunded_at         timestamptz                   NULL,
    created_at          timestamptz                   NOT NULL DEFAULT now(),
    CONSTRAINT fk_subscription_periods__subscriptions FOREIGN KEY (subscription_id) REFERENCES billing.subscriptions(id) ON DELETE CASCADE,
    CONSTRAINT ck_subscription_periods__currency_supported CHECK (currency_code IN ('USD','AED','INR','EUR','GBP','SAR','QAR','KWD','BHD','OMR')),
    CONSTRAINT ck_subscription_periods__status CHECK (status IN ('pending','paid','refunded','voided'))
);
CREATE INDEX IF NOT EXISTS ix_subscription_periods__sub    ON billing.subscription_periods (subscription_id, period_start DESC);
CREATE INDEX IF NOT EXISTS ix_subscription_periods__status ON billing.subscription_periods (status);
COMMENT ON TABLE billing.subscription_periods IS 'One row per billed period / invoice. Written only via the renew/webhook RPC path.';

-- --- renewal_reminders (idempotency-safe email queue) -----------------------
CREATE TABLE IF NOT EXISTS billing.renewal_reminders (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    subscription_id     uuid                          NOT NULL,
    kind                text                          NOT NULL,
    scheduled_for       date                          NOT NULL,
    recipient_email     citext                        NOT NULL,
    delivery_status     text                          NOT NULL DEFAULT 'queued',
    external_message_id text                          NULL,
    sent_at             timestamptz                   NULL,
    failure_reason      text                          NULL,
    created_at          timestamptz                   NOT NULL DEFAULT now(),
    CONSTRAINT fk_renewal_reminders__subscriptions FOREIGN KEY (subscription_id) REFERENCES billing.subscriptions(id) ON DELETE CASCADE,
    CONSTRAINT uq_renewal_reminders__dedup UNIQUE (subscription_id, kind, scheduled_for),
    CONSTRAINT ck_renewal_reminders__kind CHECK (kind IN ('T-60','T-30','T-14','T-7','T-1','T+1','T+7','T+30')),
    CONSTRAINT ck_renewal_reminders__delivery_status CHECK (delivery_status IN ('queued','sent','bounced','opened','failed','suppressed'))
);
CREATE INDEX IF NOT EXISTS ix_renewal_reminders__queued
  ON billing.renewal_reminders (delivery_status, scheduled_for) WHERE delivery_status = 'queued';
COMMENT ON TABLE billing.renewal_reminders IS 'Per-reminder log. Unique (subscription, kind, scheduled_for) makes the daily scheduler re-runnable.';

-- --- plan_catalog (maps Stripe price IDs to internal plan_code) -------------
CREATE TABLE IF NOT EXISTS billing.plan_catalog (
    id                uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    plan_code         text              NOT NULL,
    display_name      text              NOT NULL,
    stripe_product_id text              NOT NULL,
    stripe_price_id   text              NOT NULL,
    billing_cycle     text              NOT NULL,
    annual_amount     numeric(14,2)     NOT NULL,
    currency_code     text              NOT NULL DEFAULT 'USD',
    is_active         boolean           NOT NULL DEFAULT true,
    created_at        timestamptz       NOT NULL DEFAULT now(),
    CONSTRAINT uq_plan_catalog__stripe_price     UNIQUE (stripe_price_id),
    CONSTRAINT uq_plan_catalog__code_cycle_ccy   UNIQUE (plan_code, billing_cycle, currency_code),
    CONSTRAINT ck_plan_catalog__currency_supported CHECK (currency_code IN ('USD','AED','INR','EUR','GBP','SAR','QAR','KWD','BHD','OMR')),
    CONSTRAINT ck_plan_catalog__billing_cycle CHECK (billing_cycle IN ('monthly','quarterly','annual','biennial'))
);
COMMENT ON TABLE billing.plan_catalog IS 'Bridge between Stripe Products/Prices and subscription plan_code. Placeholder rows seeded in 0800.';

-- --- stripe_event_log (webhook idempotency) ---------------------------------
CREATE TABLE IF NOT EXISTS billing.stripe_event_log (
    id              bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    stripe_event_id text        NOT NULL,
    event_type      text        NOT NULL,
    api_version     text        NULL,
    payload         jsonb       NOT NULL,
    processed_at    timestamptz NULL,
    error           text        NULL,
    received_at     timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_stripe_event_log__event UNIQUE (stripe_event_id)
);
CREATE INDEX IF NOT EXISTS ix_stripe_event_log__type        ON billing.stripe_event_log (event_type, received_at DESC);
CREATE INDEX IF NOT EXISTS ix_stripe_event_log__unprocessed ON billing.stripe_event_log (received_at) WHERE processed_at IS NULL;
COMMENT ON TABLE billing.stripe_event_log IS 'Every Stripe webhook event, keyed on stripe_event_id for idempotency.';

-- --- ses_message_log (email audit trail) ------------------------------------
CREATE TABLE IF NOT EXISTS billing.ses_message_log (
    id                 bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    message_id         text        NOT NULL,
    reminder_id        uuid        NULL,
    recipient_email    citext      NOT NULL,
    template_key       text        NULL,
    subject            text        NULL,
    configuration_set  text        NULL,
    sent_at            timestamptz NOT NULL DEFAULT now(),
    last_event         text        NULL,   -- delivery, bounce, complaint, open, click
    last_event_at      timestamptz NULL,
    last_event_payload jsonb       NULL,
    CONSTRAINT uq_ses_message_log__message UNIQUE (message_id),
    CONSTRAINT fk_ses_message_log__reminders FOREIGN KEY (reminder_id) REFERENCES billing.renewal_reminders(id) ON DELETE SET NULL
);
CREATE INDEX IF NOT EXISTS ix_ses_message_log__reminder  ON billing.ses_message_log (reminder_id);
CREATE INDEX IF NOT EXISTS ix_ses_message_log__recipient ON billing.ses_message_log (recipient_email);
COMMENT ON TABLE billing.ses_message_log IS 'Opt-in audit trail of every SES email sent + its last delivery event.';

\echo 'ok: 0012 billing done.'
