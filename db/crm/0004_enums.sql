-- ============================================================================
-- 0004_enums.sql
-- No-op (kept as a numbered file for ordering).
--
-- The CRM schema no longer uses native PostgreSQL enum types. Every former
-- enum domain is now a plain `text` column constrained by a `CHECK (... IN
-- (...))` constraint on its table (named `ck_<table>__<col>`), matching the
-- sibling GoldKiosk.AdminDashboard schema. This lets the EF Core / Npgsql
-- layer map every such column to a C# `string` with no enum mapping.
--
-- The allowed values for each former enum (now enforced per-column) are:
--   crm_role                   : sales_manager, sales_rep, superadmin
--   lead_source                : website, inbound_call, partner_referral, conference, outbound
--   lead_stage                 : new, contacted, qualified, proposal, won, lost
--   activity_type              : note, call, email, stage_change, proposal_sent, won, lost
--   tenant_status              : provisioning, active, suspended, churned
--   provisioning_status        : pending, in_progress, succeeded, failed
--   contract_type              : msa, sow, dpa, sla, nda, addendum
--   contract_status            : draft, out_for_signature, signed, terminated
--   entity_type                : lead, partner, tenant, activation_key, contract, profile
--   audit_action               : create, update, delete, provision, revoke_key, reissue_key, sign_in, export
--   kiosk_lifecycle            : ordered, installed, live, maintenance, decommissioned
--   subscription_status        : trialing, active, past_due, suspended, cancelled, expired
--   billing_cycle              : monthly, quarterly, annual, biennial
--   payment_method             : bank_transfer, card, invoice, crypto
--   reminder_kind              : T-60, T-30, T-14, T-7, T-1, T+1, T+7, T+30
--   reminder_delivery_status   : queued, sent, bounced, opened, failed, suppressed
--   subscription_period_status : pending, paid, refunded, voided
-- ============================================================================

\echo '------------------------------------------------------------------------'
\echo '0004 - Enum domains are now text columns + CHECK constraints (no-op)'
\echo '------------------------------------------------------------------------'

\echo ''
\echo 'ok: 0004 complete.'
