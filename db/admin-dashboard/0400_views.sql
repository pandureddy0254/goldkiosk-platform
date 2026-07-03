-- ============================================================================
-- 0400_views.sql
-- Standard views used by the AdminDashboard reports + monitoring pages.
-- Note: dependencies defined first (source views), then the views that join them.
-- ============================================================================

\echo '── 0400 views ──'

-- ─── reporting.expenses_source (placeholder until tx.expense_entries exists) ─
-- expense_entries table is planned but not in 0010-0027; once added, replace
-- this view with a real query over that table.
CREATE OR REPLACE VIEW reporting.expenses_source AS
SELECT NULL::uuid AS tenant_id,
       NULL::uuid AS kiosk_id,
       NULL::timestamptz AS incurred_at,
       NULL::public.domain_currency_code AS currency_code,
       NULL::public.domain_money AS amount
WHERE false;

-- ─── reporting.expenses_per_day (depends on expenses_source) ────────────────
CREATE OR REPLACE VIEW reporting.expenses_per_day AS
SELECT
    e.tenant_id,
    e.kiosk_id,
    date_trunc('day', e.incurred_at)::date AS expense_date,
    e.currency_code,
    SUM(e.amount) AS amount
FROM reporting.expenses_source e
GROUP BY e.tenant_id, e.kiosk_id, date_trunc('day', e.incurred_at), e.currency_code;

-- ─── reporting.v_daily_sales ────────────────────────────────────────────────
CREATE OR REPLACE VIEW reporting.v_daily_sales AS
SELECT
    t.tenant_id,
    t.kiosk_id,
    date_trunc('day', t.occurred_at)::date AS sale_date,
    t.kind,
    count(*)                              AS transaction_count,
    count(DISTINCT t.customer_id)         AS customer_count,
    COALESCE(SUM(ti.billed_weight_g), 0)  AS total_weight_g,
    COALESCE(SUM(t.net_payout), 0)        AS total_payout,
    t.currency_code
FROM tx.transactions t
LEFT JOIN tx.transaction_items ti ON ti.transaction_id = t.id
WHERE t.status IN ('paid','accepted')
GROUP BY t.tenant_id, t.kiosk_id, date_trunc('day', t.occurred_at), t.kind, t.currency_code;

-- ─── reporting.v_daily_inventory ────────────────────────────────────────────
CREATE OR REPLACE VIEW reporting.v_daily_inventory AS
SELECT
    k.tenant_id,
    k.id AS kiosk_id,
    date_trunc('day', s.captured_at)::date AS snapshot_date,
    s.metal,
    SUM(s.weight_g) AS total_weight_g,
    AVG(s.carat)    AS avg_carat
FROM kiosk.kiosks k
JOIN ops.kiosk_inventory_snapshots s ON s.kiosk_id = k.id
GROUP BY k.tenant_id, k.id, date_trunc('day', s.captured_at), s.metal;

-- ─── reporting.v_daily_profit (depends on expenses_per_day) ─────────────────
CREATE OR REPLACE VIEW reporting.v_daily_profit AS
WITH sales AS (
    SELECT
        t.tenant_id,
        t.kiosk_id,
        date_trunc('day', t.occurred_at)::date AS profit_date,
        t.currency_code,
        SUM(t.total_amount)                AS gross_sales,
        SUM(t.net_payout)                  AS cost_of_acquisition,
        SUM(t.total_amount - t.net_payout) AS expected_profit
    FROM tx.transactions t
    WHERE t.status = 'paid'
    GROUP BY t.tenant_id, t.kiosk_id, date_trunc('day', t.occurred_at), t.currency_code
)
SELECT
    s.tenant_id,
    s.kiosk_id,
    s.profit_date,
    s.gross_sales,
    s.cost_of_acquisition,
    s.expected_profit,
    COALESCE(e.amount, 0) AS expenses,
    s.currency_code
FROM sales s
LEFT JOIN reporting.expenses_per_day e
       ON e.tenant_id    = s.tenant_id
      AND e.kiosk_id     = s.kiosk_id
      AND e.expense_date = s.profit_date
      AND e.currency_code = s.currency_code;

-- ─── reporting.v_active_kiosks ──────────────────────────────────────────────
CREATE OR REPLACE VIEW reporting.v_active_kiosks AS
SELECT k.tenant_id, k.id AS kiosk_id, k.code, k.friendly_name, k.last_ping_at,
       (k.last_ping_at > now() - interval '5 minutes') AS is_online
FROM kiosk.kiosks k
WHERE k.is_active = true;

-- ─── monitor.v_api_success_pct ──────────────────────────────────────────────
CREATE OR REPLACE VIEW monitor.v_api_success_pct AS
SELECT
    endpoint,
    count(*)                            AS total,
    count(*) FILTER (WHERE is_success)  AS successes,
    ROUND(100.0 * count(*) FILTER (WHERE is_success) / NULLIF(count(*),0), 2) AS success_pct,
    date_trunc('hour', requested_at)    AS hour
FROM monitor.api_request_logs
WHERE requested_at > now() - interval '24 hours'
GROUP BY endpoint, date_trunc('hour', requested_at);

-- ─── tx.v_transaction_full ──────────────────────────────────────────────────
-- DROP first because CREATE OR REPLACE cannot change column order, and `t.*`
-- expands differently as new columns are added to tx.transactions.
DROP VIEW IF EXISTS tx.v_transaction_full;
CREATE VIEW tx.v_transaction_full AS
SELECT
    t.*,
    o.id              AS offer_id,
    o.total_offered   AS offer_total,
    o.status          AS offer_status,
    (SELECT count(*) FROM tx.transaction_items ti WHERE ti.transaction_id = t.id) AS items_count
FROM tx.transactions t
LEFT JOIN pricing.offers o ON o.transaction_id = t.id;

\echo '✓ 0400 done.'
