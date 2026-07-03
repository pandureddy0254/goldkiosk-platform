-- ============================================================================
-- 0202_triggers_business.sql
-- Business triggers ported from the Supabase chain:
--   crm.leads          -> emit a lead_activities row on stage change
--   crm.partners       -> stamp provisioned_at / churned_at on status flip
--   crm.activation_keys-> refuse UPDATE once consumed
--   billing.subscriptions -> mirror status to partner.tenant_status + cache MRR
--
-- Functions that write to OTHER tables are SECURITY DEFINER and re-owned to
-- gk_crm_backend in 0700 so they run with BYPASSRLS. Functions that only
-- mutate NEW or RAISE stay SECURITY INVOKER.
-- ============================================================================

\echo '-- 0202 business triggers --'

-- --- leads: emit a lead_activities row on stage change ----------------------
CREATE OR REPLACE FUNCTION crm.fn_log_stage_change()
RETURNS trigger
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = crm, public, pg_temp
AS $$
BEGIN
  IF NEW.stage IS DISTINCT FROM OLD.stage THEN
    INSERT INTO crm.lead_activities (lead_id, actor_id, activity_type, payload)
    VALUES (NEW.id, crm.current_user_id(), 'stage_change',
            jsonb_build_object('from', OLD.stage, 'to', NEW.stage));
  END IF;
  RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS tg_leads__au_activity_on_stage_change ON crm.leads;
CREATE TRIGGER tg_leads__au_activity_on_stage_change
  AFTER UPDATE ON crm.leads
  FOR EACH ROW WHEN (NEW.stage IS DISTINCT FROM OLD.stage)
  EXECUTE FUNCTION crm.fn_log_stage_change();

-- --- partners: stamp provisioned_at / churned_at on status flip -------------
CREATE OR REPLACE FUNCTION crm.fn_partners_on_provisioned()
RETURNS trigger
LANGUAGE plpgsql
SET search_path = crm, public, pg_temp
AS $$
BEGIN
  IF NEW.tenant_status = 'active'
     AND (OLD.tenant_status IS DISTINCT FROM 'active')
     AND NEW.provisioned_at IS NULL THEN
    NEW.provisioned_at := now();
  END IF;
  IF NEW.tenant_status = 'churned'
     AND (OLD.tenant_status IS DISTINCT FROM 'churned')
     AND NEW.churned_at IS NULL THEN
    NEW.churned_at := now();
  END IF;
  RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS tg_partners__bu_stamp_provisioned ON crm.partners;
CREATE TRIGGER tg_partners__bu_stamp_provisioned
  BEFORE UPDATE ON crm.partners
  FOR EACH ROW EXECUTE FUNCTION crm.fn_partners_on_provisioned();

-- --- activation_keys: once consumed, refuse all UPDATEs ----------------------
CREATE OR REPLACE FUNCTION crm.fn_activation_keys_immutable_once_consumed()
RETURNS trigger
LANGUAGE plpgsql
SET search_path = crm, public, pg_temp
AS $$
BEGIN
  IF OLD.consumed_at IS NOT NULL THEN
    RAISE EXCEPTION 'activation_keys row is immutable once consumed (id=%, tenant=%)',
      OLD.id, OLD.tenant_id USING ERRCODE = '23514';
  END IF;
  RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS tg_activation_keys__bu_no_modify_consumed ON crm.activation_keys;
CREATE TRIGGER tg_activation_keys__bu_no_modify_consumed
  BEFORE UPDATE ON crm.activation_keys
  FOR EACH ROW EXECUTE FUNCTION crm.fn_activation_keys_immutable_once_consumed();

-- --- subscriptions -> mirror status to partner.tenant_status ----------------
CREATE OR REPLACE FUNCTION billing.fn_subscriptions_sync_partner_status()
RETURNS trigger
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = billing, crm, public, pg_temp
AS $$
BEGIN
  IF NEW.status IS DISTINCT FROM OLD.status THEN
    IF NEW.status = 'suspended' THEN
      UPDATE crm.partners SET tenant_status = 'suspended', updated_at = now()
       WHERE id = NEW.partner_id AND tenant_status <> 'churned';
    ELSIF NEW.status IN ('cancelled','expired') THEN
      UPDATE crm.partners SET tenant_status = 'churned', updated_at = now()
       WHERE id = NEW.partner_id AND tenant_status <> 'churned';
    ELSIF NEW.status = 'active' AND OLD.status IN ('suspended','past_due') THEN
      UPDATE crm.partners SET tenant_status = 'active', updated_at = now()
       WHERE id = NEW.partner_id;
    END IF;
  END IF;
  RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS tg_subscriptions__au_sync_partner_status ON billing.subscriptions;
CREATE TRIGGER tg_subscriptions__au_sync_partner_status
  AFTER UPDATE ON billing.subscriptions
  FOR EACH ROW EXECUTE FUNCTION billing.fn_subscriptions_sync_partner_status();

-- --- subscriptions -> cache partners.mrr_amount + currency (annual / 12) -----
CREATE OR REPLACE FUNCTION billing.fn_subscriptions_sync_partner_mrr()
RETURNS trigger
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = billing, crm, public, pg_temp
AS $$
DECLARE v_mrr numeric(14,2);
BEGIN
  IF TG_OP = 'DELETE' THEN
    UPDATE crm.partners SET mrr_amount = 0 WHERE id = OLD.partner_id;
    RETURN OLD;
  END IF;

  IF NEW.status IN ('active','trialing','past_due') THEN
    v_mrr := round(NEW.annual_amount / 12.0, 2);
  ELSE
    v_mrr := 0;
  END IF;

  UPDATE crm.partners
     SET mrr_amount    = v_mrr,
         currency_code = NEW.currency_code,
         updated_at    = now()
   WHERE id = NEW.partner_id;
  RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS tg_subscriptions__aiu_sync_partner_mrr ON billing.subscriptions;
CREATE TRIGGER tg_subscriptions__aiu_sync_partner_mrr
  AFTER INSERT OR UPDATE OF annual_amount, status, currency_code ON billing.subscriptions
  FOR EACH ROW EXECUTE FUNCTION billing.fn_subscriptions_sync_partner_mrr();

DROP TRIGGER IF EXISTS tg_subscriptions__ad_sync_partner_mrr ON billing.subscriptions;
CREATE TRIGGER tg_subscriptions__ad_sync_partner_mrr
  AFTER DELETE ON billing.subscriptions
  FOR EACH ROW EXECUTE FUNCTION billing.fn_subscriptions_sync_partner_mrr();

\echo 'ok: 0202 business triggers done.'
