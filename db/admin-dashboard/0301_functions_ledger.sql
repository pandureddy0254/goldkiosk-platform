-- ============================================================================
-- 0301_functions_ledger.sql
-- Atomic ledger posting helpers.
-- Functions:
--   customer.post_ledger_entry(...)
--   merchant.post_franchise_ledger_entry(...)
-- ============================================================================

\echo '── 0301 functions: ledger posting ──'

-- ─── customer wallet ledger posting ─────────────────────────────────────────
CREATE OR REPLACE FUNCTION customer.post_ledger_entry(
    p_wallet_id          uuid,
    p_entry_kind         text,
    p_amount             public.domain_money,
    p_reference_type     text DEFAULT NULL,
    p_reference_id       uuid DEFAULT NULL,
    p_posted_by_user_id  uuid DEFAULT NULL
)
RETURNS uuid
LANGUAGE plpgsql
AS $$
DECLARE
    v_id            uuid;
    v_new_balance   public.domain_money;
BEGIN
    -- Lock the wallet row to serialise concurrent posts.
    SELECT (balance + p_amount) INTO v_new_balance
      FROM customer.customer_wallets
     WHERE id = p_wallet_id
       FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'wallet % not found', p_wallet_id;
    END IF;

    IF v_new_balance < 0 THEN
        RAISE EXCEPTION 'insufficient balance: would go to %', v_new_balance;
    END IF;

    INSERT INTO customer.wallet_ledger_entries(
        wallet_id, entry_kind, amount, balance_after,
        reference_type, reference_id, posted_by_user_id
    ) VALUES (
        p_wallet_id, p_entry_kind, p_amount, v_new_balance,
        p_reference_type, p_reference_id, p_posted_by_user_id
    )
    RETURNING id INTO v_id;
    -- (the AFTER INSERT trigger maintains customer_wallets.balance and last_updated_at)

    RETURN v_id;
END;
$$;

COMMENT ON FUNCTION customer.post_ledger_entry(uuid, text, public.domain_money, text, uuid, uuid)
  IS 'Atomic ledger append + balance check + balance maintenance for customer wallets.';

-- ─── franchise wallet ledger posting ────────────────────────────────────────
CREATE OR REPLACE FUNCTION merchant.post_franchise_ledger_entry(
    p_wallet_id          uuid,
    p_entry_kind         text,
    p_amount             public.domain_money,
    p_reference_type     text DEFAULT NULL,
    p_reference_id       uuid DEFAULT NULL,
    p_posted_by_user_id  uuid DEFAULT NULL
)
RETURNS uuid
LANGUAGE plpgsql
AS $$
DECLARE
    v_id            uuid;
    v_new_balance   public.domain_money;
BEGIN
    SELECT (balance + p_amount) INTO v_new_balance
      FROM merchant.franchise_wallets
     WHERE id = p_wallet_id
       FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'franchise wallet % not found', p_wallet_id;
    END IF;

    IF v_new_balance < 0 THEN
        RAISE EXCEPTION 'insufficient franchise balance: would go to %', v_new_balance;
    END IF;

    INSERT INTO merchant.franchise_wallet_ledger(
        wallet_id, entry_kind, amount, balance_after,
        reference_type, reference_id, posted_by_user_id
    ) VALUES (
        p_wallet_id, p_entry_kind, p_amount, v_new_balance,
        p_reference_type, p_reference_id, p_posted_by_user_id
    )
    RETURNING id INTO v_id;

    RETURN v_id;
END;
$$;

\echo '✓ 0301 done.'
