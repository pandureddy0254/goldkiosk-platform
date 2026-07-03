-- ============================================================================
-- 0400_views.sql
-- Read-only reporting views. Created WITH (security_invoker = true) (PG15+) so
-- they run with the *querying* role's privileges + RLS - this avoids the
-- view-owner-bypasses-RLS gotcha the Supabase chain hit with
-- activation_keys_public. Grants are applied in 0700.
-- ============================================================================

\echo '-- 0400 reporting views --'

-- --- crm.v_lead_funnel: live lead counts + pipeline value by stage ----------
CREATE OR REPLACE VIEW crm.v_lead_funnel
  WITH (security_invoker = true) AS
SELECT l.stage,
       count(*)                               AS lead_count,
       coalesce(sum(l.estimated_value), 0)    AS pipeline_value,
       l.currency_code
  FROM crm.leads l
 WHERE l.deleted_at IS NULL
 GROUP BY l.stage, l.currency_code;
COMMENT ON VIEW crm.v_lead_funnel IS 'Live (non-deleted) lead counts + summed estimated value, by stage and currency.';

-- --- crm.v_partner_directory: partner + tenant + live key state -------------
CREATE OR REPLACE VIEW crm.v_partner_directory
  WITH (security_invoker = true) AS
SELECT p.id              AS partner_id,
       p.display_name,
       p.region,
       p.tenant_status,
       p.mrr_amount,
       p.currency_code,
       p.initial_kiosk_count,
       t.id              AS tenant_id,
       t.admin_tenant_id,
       t.provisioning_status,
       ak.key_prefix     AS live_key_prefix,
       ak.expires_at     AS live_key_expires_at
  FROM crm.partners p
  LEFT JOIN crm.tenants t ON t.partner_id = p.id
  LEFT JOIN crm.activation_keys ak
         ON ak.tenant_id = t.id AND ak.consumed_at IS NULL AND ak.revoked_at IS NULL;
COMMENT ON VIEW crm.v_partner_directory IS 'Partner row joined to its tenant + current live (unconsumed, unrevoked) activation key, if any.';

-- --- billing.v_renewals_upcoming: subscriptions renewing within 60 days -----
CREATE OR REPLACE VIEW billing.v_renewals_upcoming
  WITH (security_invoker = true) AS
SELECT s.id            AS subscription_id,
       s.partner_id,
       p.display_name  AS partner_name,
       s.plan_code,
       s.status,
       s.billing_cycle,
       s.annual_amount,
       s.currency_code,
       s.current_period_end,
       (s.current_period_end::date - current_date) AS days_until_renewal,
       s.auto_renew
  FROM billing.subscriptions s
  JOIN crm.partners p ON p.id = s.partner_id
 WHERE s.current_period_end >= now()
   AND s.current_period_end <  now() + interval '60 days'
   AND s.status IN ('active','trialing','past_due');
COMMENT ON VIEW billing.v_renewals_upcoming IS 'Subscriptions whose current_period_end lands in the next 60 days (the renewal pipeline).';

\echo 'ok: 0400 views done.'
