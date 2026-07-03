-- ============================================================================
-- 0201_triggers_immutable.sql
-- Attach public.fn_reject_modify() to append-only tables: any UPDATE/DELETE
-- raises. Only audit.audit_log is fully immutable; conditional immutability
-- (activation_keys-once-consumed) lives in 0202 as a business trigger.
-- ============================================================================

\echo '-- 0201 triggers: append-only enforcement --'

DROP TRIGGER IF EXISTS tg_audit_log__bud_immutable ON audit.audit_log;
CREATE TRIGGER tg_audit_log__bud_immutable
  BEFORE UPDATE OR DELETE ON audit.audit_log
  FOR EACH ROW EXECUTE FUNCTION public.fn_reject_modify();

\echo 'ok: 0201 done.'
