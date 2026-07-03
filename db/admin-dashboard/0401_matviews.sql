-- ============================================================================
-- 0401_matviews.sql
-- Materialised views — nightly refresh via pg_cron / Azure Function.
-- Each matview has a refresh procedure that runs CONCURRENTLY when possible.
-- ============================================================================

\echo '── 0401 materialised views ──'

-- ─── reporting.mv_daily_sales_summary ───────────────────────────────────────
CREATE MATERIALIZED VIEW IF NOT EXISTS reporting.mv_daily_sales_summary AS
SELECT * FROM reporting.v_daily_sales
WITH NO DATA;

-- Unique index required for REFRESH MATERIALIZED VIEW CONCURRENTLY.
CREATE UNIQUE INDEX IF NOT EXISTS ux_mv_daily_sales_summary
  ON reporting.mv_daily_sales_summary(tenant_id, kiosk_id, sale_date, kind, currency_code);

-- ─── reporting.mv_daily_inventory_summary ───────────────────────────────────
CREATE MATERIALIZED VIEW IF NOT EXISTS reporting.mv_daily_inventory_summary AS
SELECT * FROM reporting.v_daily_inventory
WITH NO DATA;

CREATE UNIQUE INDEX IF NOT EXISTS ux_mv_daily_inventory_summary
  ON reporting.mv_daily_inventory_summary(tenant_id, kiosk_id, snapshot_date, metal);

-- ─── reporting.mv_daily_profit_summary ──────────────────────────────────────
CREATE MATERIALIZED VIEW IF NOT EXISTS reporting.mv_daily_profit_summary AS
SELECT * FROM reporting.v_daily_profit
WITH NO DATA;

CREATE UNIQUE INDEX IF NOT EXISTS ux_mv_daily_profit_summary
  ON reporting.mv_daily_profit_summary(tenant_id, kiosk_id, profit_date, currency_code);

-- ─── refresh procedure (called by scheduler) ────────────────────────────────
CREATE OR REPLACE PROCEDURE reporting.refresh_all_matviews()
LANGUAGE plpgsql
AS $$
BEGIN
    REFRESH MATERIALIZED VIEW CONCURRENTLY reporting.mv_daily_sales_summary;
    REFRESH MATERIALIZED VIEW CONCURRENTLY reporting.mv_daily_inventory_summary;
    REFRESH MATERIALIZED VIEW CONCURRENTLY reporting.mv_daily_profit_summary;
EXCEPTION
    -- First refresh (with no data) must be non-concurrent.
    WHEN feature_not_supported THEN
        REFRESH MATERIALIZED VIEW reporting.mv_daily_sales_summary;
        REFRESH MATERIALIZED VIEW reporting.mv_daily_inventory_summary;
        REFRESH MATERIALIZED VIEW reporting.mv_daily_profit_summary;
END;
$$;

COMMENT ON PROCEDURE reporting.refresh_all_matviews()
  IS 'Refreshes all reporting matviews concurrently. Schedule nightly via pg_cron or Azure Function.';

\echo '✓ 0401 done.'
