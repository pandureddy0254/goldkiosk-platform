-- ============================================================================
-- 0030_tables_tx_reversal.sql
-- Adds a reversal marker to tx.transactions to support the AdminDashboard
-- TransactionHistory "Is Reversed" column and the (stub) ReverseTransaction
-- POST action on CustomerManagementController. The actual reversal workflow
-- (ledger compensating entry, payout refund, audit row) lands in a later pass.
-- ============================================================================

\echo '── 0030 tx reversal marker ──'

ALTER TABLE tx.transactions
    ADD COLUMN IF NOT EXISTS reversed_at         timestamptz NULL,
    ADD COLUMN IF NOT EXISTS reversed_by_user_id uuid NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
         WHERE conname = 'fk_transactions__reversed_by_user'
    ) THEN
        ALTER TABLE tx.transactions
            ADD CONSTRAINT fk_transactions__reversed_by_user
            FOREIGN KEY (reversed_by_user_id) REFERENCES identity.users(id);
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS ix_transactions__reversed
    ON tx.transactions(tenant_id, reversed_at DESC)
    WHERE reversed_at IS NOT NULL;

\echo '✓ 0030 done.'
