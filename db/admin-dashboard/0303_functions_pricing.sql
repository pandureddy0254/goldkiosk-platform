-- ============================================================================
-- 0303_functions_pricing.sql
-- Pricing helpers: latest live metal rate with SLA check.
-- ============================================================================

\echo '── 0303 functions: pricing ──'

CREATE OR REPLACE FUNCTION pricing.latest_metal_rate(
    p_metal     text,
    p_karat     public.domain_karat,
    p_currency  public.domain_currency_code
)
RETURNS pricing.metal_rates
LANGUAGE plpgsql
STABLE
AS $$
DECLARE
    v_rate pricing.metal_rates%ROWTYPE;
    v_sla  integer;
BEGIN
    SELECT mr.* INTO v_rate
      FROM pricing.metal_rates mr
     WHERE mr.metal = p_metal
       AND mr.purity_karat = p_karat
       AND mr.currency_code = p_currency
     ORDER BY mr.retrieved_at DESC
     LIMIT 1;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'no metal rate found for %/%/%', p_metal, p_karat, p_currency;
    END IF;

    -- Honour the source's freshness SLA.
    SELECT freshness_sla_seconds INTO v_sla
      FROM pricing.metal_rate_sources
     WHERE id = v_rate.metal_rate_source_id;

    IF v_sla IS NOT NULL AND now() - v_rate.retrieved_at > make_interval(secs => v_sla) THEN
        RAISE EXCEPTION 'metal rate stale: % older than % seconds', v_rate.retrieved_at, v_sla;
    END IF;

    RETURN v_rate;
END;
$$;

COMMENT ON FUNCTION pricing.latest_metal_rate(text, public.domain_karat, public.domain_currency_code)
  IS 'Returns the most recent metal rate for the given metal/karat/currency, or raises if stale beyond the source''s SLA.';

\echo '✓ 0303 done.'
