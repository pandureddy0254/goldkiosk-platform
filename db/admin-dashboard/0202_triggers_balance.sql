-- ============================================================================
-- 0202_triggers_balance.sql
-- Maintains derived state when ledger rows are inserted:
--   * customer.customer_wallets.balance + last_updated_at
--   * merchant.franchise_wallets.balance + last_topup_at
--   * voucher.vouchers.used_count
-- ============================================================================

\echo '── 0202 triggers: derived state maintenance ──'

-- ─── customer wallet balance ────────────────────────────────────────────────
CREATE OR REPLACE FUNCTION customer.fn_maintain_wallet_balance()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE customer.customer_wallets
       SET balance         = NEW.balance_after,
           last_updated_at = NEW.occurred_at
     WHERE id = NEW.wallet_id;
    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS tg_wallet_ledger_entries__ai_balance ON customer.wallet_ledger_entries;
CREATE TRIGGER tg_wallet_ledger_entries__ai_balance
AFTER INSERT ON customer.wallet_ledger_entries
FOR EACH ROW EXECUTE FUNCTION customer.fn_maintain_wallet_balance();

-- ─── franchise wallet balance ───────────────────────────────────────────────
CREATE OR REPLACE FUNCTION merchant.fn_maintain_franchise_wallet_balance()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE merchant.franchise_wallets
       SET balance       = NEW.balance_after,
           last_topup_at = CASE WHEN NEW.entry_kind = 'topup' THEN NEW.occurred_at ELSE last_topup_at END
     WHERE id = NEW.wallet_id;
    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS tg_franchise_wallet_ledger__ai_balance ON merchant.franchise_wallet_ledger;
CREATE TRIGGER tg_franchise_wallet_ledger__ai_balance
AFTER INSERT ON merchant.franchise_wallet_ledger
FOR EACH ROW EXECUTE FUNCTION merchant.fn_maintain_franchise_wallet_balance();

-- ─── voucher usage counter ──────────────────────────────────────────────────
CREATE OR REPLACE FUNCTION voucher.fn_increment_usage_count()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE voucher.vouchers
       SET used_count = used_count + 1
     WHERE id = NEW.voucher_id;
    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS tg_voucher_redemptions__ai_usage_count ON voucher.voucher_redemptions;
CREATE TRIGGER tg_voucher_redemptions__ai_usage_count
AFTER INSERT ON voucher.voucher_redemptions
FOR EACH ROW EXECUTE FUNCTION voucher.fn_increment_usage_count();

\echo '✓ 0202 done.'
