-- ============================================================================
-- 0800_seed_demo.sql
-- Demo data so the screens and the DB tell the same story. Ported from the
-- Supabase 0300_seed_data + 0500_subscriptions seed blocks, minus auth.users
-- (profiles are seeded directly).
--
-- GUARDED: the whole block only runs when the database name starts with
-- 'goldkiosk_crm_local' or 'goldkiosk_crm_sit'. Applying this file to UAT/Prod
-- is a no-op, so the business tables start empty there.
--
-- Idempotent: every insert is ON CONFLICT DO NOTHING / DO UPDATE.
-- Runs as the deploy/superuser role, so FORCE RLS is bypassed for the seed.
-- ============================================================================

\echo '-- 0800 demo seed (local/SIT only) --'

DO $seed$
BEGIN
  IF current_database() NOT LIKE 'goldkiosk_crm_local%'
     AND current_database() NOT LIKE 'goldkiosk_crm_sit%' THEN
    RAISE NOTICE '  skipped demo seed (database % is not local/SIT)', current_database();
    RETURN;
  END IF;

  -- --- Internal staff profiles ---------------------------------------------
  INSERT INTO crm.profiles (id, full_name, email, role, password_hash) VALUES
    ('00000000-0000-0000-0000-000000000001','Sara Al-Khouri',    'sara.alkhouri@aikiosks.com',    'sales_manager', public.crypt('CRM-demo-2026!', public.gen_salt('bf'))),
    ('00000000-0000-0000-0000-000000000002','Aman Choudhury',    'aman.choudhury@aikiosks.com',   'sales_rep',     public.crypt('CRM-demo-2026!', public.gen_salt('bf'))),
    ('00000000-0000-0000-0000-000000000003','Rashid Al-Mazrouei','rashid.almazrouei@aikiosks.com','sales_rep',     public.crypt('CRM-demo-2026!', public.gen_salt('bf'))),
    ('00000000-0000-0000-0000-000000000004','Nadia Khanna',      'nadia.khanna@aikiosks.com',     'sales_rep',     public.crypt('CRM-demo-2026!', public.gen_salt('bf'))),
    ('00000000-0000-0000-0000-000000000005','Pandu Aluvala',     'paluvala@goldkiosk.com',        'sales_manager', public.crypt('AiKiosk135#',    public.gen_salt('bf')))
  ON CONFLICT (id) DO UPDATE
    SET full_name = excluded.full_name, email = excluded.email, role = excluded.role;

  -- --- Leads ----------------------------------------------------------------
  INSERT INTO crm.leads
    (id, company_name, contact_name, contact_email, contact_phone, country, region,
     source, stage, estimated_value, currency_code, estimated_kiosk_count, owner_id, notes, created_at)
  VALUES
    ('11111111-1111-1111-1111-000000000001','Emirates Gold Bank PJSC','Sara Al Mansoori','cio@emiratesgold.ae','+97145550148','United Arab Emirates','Dubai',
     'partner_referral','won',2400000,'USD',12,'00000000-0000-0000-0000-000000000001','Bank CIO + CRO walked through XRF + biometric flow. MSA counter-signed 22 May.', now() - interval '52 days'),
    ('11111111-1111-1111-1111-000000000002','Damas Jewellery LLC','Hisham Damas','h.damas@damasjewellery.com','+97142100200','United Arab Emirates','Dubai',
     'outbound','won',1800000,'USD',9,'00000000-0000-0000-0000-000000000001','Multi-store rollout.', now() - interval '38 days'),
    ('11111111-1111-1111-1111-000000000003','Mashreq Gold','Khalid Bin Hamoodah','khalid.h@mashreq.com','+97126100200','United Arab Emirates','Abu Dhabi',
     'inbound_call','won',1200000,'USD',6,'00000000-0000-0000-0000-000000000002','AUH-led; CIO follow-up booked.', now() - interval '46 days'),
    ('11111111-1111-1111-1111-000000000004','Joyalukkas Gulf','Asha Joy','asha.j@joyalukkas.com','+97142200300','United Arab Emirates','Dubai',
     'conference','proposal',980000,'USD',12,'00000000-0000-0000-0000-000000000001','Met at Dubai Gold Forum 2026.', now() - interval '21 days'),
    ('11111111-1111-1111-1111-000000000005','Al-Futtaim Retail','Rana Al Futtaim','rana@alfuttaim.ae','+97144500600','United Arab Emirates','Dubai',
     'outbound','contacted',680000,'USD',6,'00000000-0000-0000-0000-000000000003','Procurement lead; awaiting RFP.', now() - interval '12 days'),
    ('11111111-1111-1111-1111-000000000006','Pure Gold Group','Firoz Merchant','firoz@puregoldjewellers.com','+97165553400','United Arab Emirates','Sharjah',
     'outbound','proposal',540000,'USD',8,'00000000-0000-0000-0000-000000000001','Proposal v1 sent 16 May.', now() - interval '14 days'),
    ('11111111-1111-1111-1111-000000000007','ENBD Wealth - Pilot','Yousef Al Maktoum','y.maktoum@enbd.com','+97140150100','United Arab Emirates','Dubai',
     'website','qualified',360000,'USD',2,'00000000-0000-0000-0000-000000000002','Private-branch pilot. Awaiting compliance review.', now() - interval '18 days'),
    ('11111111-1111-1111-1111-000000000008','Liali Jewellery','Vidya Liali','vidya@lialijewellery.com','+97143570333','United Arab Emirates','Dubai',
     'partner_referral','new',160000,'USD',1,'00000000-0000-0000-0000-000000000003','Souk Madinat single-kiosk pilot.', now() - interval '8 days')
  ON CONFLICT (id) DO NOTHING;

  -- --- Partners -------------------------------------------------------------
  INSERT INTO crm.partners
    (id, legal_name, display_name, billing_address, region, primary_admin_name, primary_admin_email,
     primary_admin_role_title, initial_kiosk_count, mrr_amount, currency_code, tenant_status, lead_id, created_at, provisioned_at)
  VALUES
    ('22222222-2222-2222-2222-000000000001','Mashreq Gold','Mashreq Gold',
     jsonb_build_object('line1','Mashreq Tower, Khalid bin Al-Waleed Rd','city','Dubai','country','AE'),
     'UAE - DXB','Khalid Bin Hamoodah','khalid.h@mashreq.com','Chief Information Officer',
     22, 340000, 'USD', 'active', '11111111-1111-1111-1111-000000000003', now() - interval '40 days', now() - interval '32 days'),
    ('22222222-2222-2222-2222-000000000002','Emirates Gold Bank PJSC','Emirates Gold Bank',
     jsonb_build_object('line1','DIFC Gate Building 4, Level 12','city','Dubai','country','AE'),
     'UAE - DXB','Sara Al Mansoori','cio@emiratesgold.ae','Chief Information Officer',
     12, 200000, 'USD', 'provisioning', '11111111-1111-1111-1111-000000000001', now() - interval '1 hour', null)
  ON CONFLICT (id) DO NOTHING;

  -- --- Tenants --------------------------------------------------------------
  INSERT INTO crm.tenants (id, partner_id, admin_tenant_id, region_code, provisioning_status) VALUES
    ('33333333-3333-3333-3333-000000000001','22222222-2222-2222-2222-000000000001','a1a1a1a1-a1a1-a1a1-a1a1-a1a1a1a1a1a1','ae-1','succeeded'),
    ('33333333-3333-3333-3333-000000000002','22222222-2222-2222-2222-000000000002','b2b2b2b2-b2b2-b2b2-b2b2-b2b2b2b2b2b2','ae-1','in_progress')
  ON CONFLICT (id) DO NOTHING;

  -- --- Activation keys (Mashreq consumed; Emirates live) --------------------
  INSERT INTO crm.activation_keys (tenant_id, key_prefix, key_hash, issued_to_email, issued_at, expires_at, consumed_at) VALUES
    ('33333333-3333-3333-3333-000000000001','AIKI-2D4F',
     encode(public.digest('AIKI-2D4F-A001-9C77','sha256'),'hex'),'khalid.h@mashreq.com',
     now() - interval '40 days', now() - interval '10 days', now() - interval '32 days')
  ON CONFLICT DO NOTHING;
  INSERT INTO crm.activation_keys (tenant_id, key_prefix, key_hash, issued_to_email, issued_at, expires_at) VALUES
    ('33333333-3333-3333-3333-000000000002','AIKI-7F2A',
     encode(public.digest('AIKI-7F2A-9C31-B4E8','sha256'),'hex'),'cio@emiratesgold.ae',
     now() - interval '1 hour', now() + interval '30 days')
  ON CONFLICT DO NOTHING;

  -- --- Lead activities ------------------------------------------------------
  INSERT INTO crm.lead_activities (lead_id, actor_id, activity_type, payload, occurred_at) VALUES
    ('11111111-1111-1111-1111-000000000001','00000000-0000-0000-0000-000000000001','call',         '{"summary":"Discovery call - 38 min - CIO + CRO"}', now() - interval '49 days'),
    ('11111111-1111-1111-1111-000000000001','00000000-0000-0000-0000-000000000001','email',        '{"subject":"Demo recap + architecture"}',           now() - interval '47 days'),
    ('11111111-1111-1111-1111-000000000001','00000000-0000-0000-0000-000000000001','stage_change', '{"from":"contacted","to":"qualified"}',             now() - interval '40 days'),
    ('11111111-1111-1111-1111-000000000001','00000000-0000-0000-0000-000000000001','proposal_sent','{"version":1,"value":2400000}',                     now() - interval '30 days'),
    ('11111111-1111-1111-1111-000000000001','00000000-0000-0000-0000-000000000001','call',         '{"summary":"Counter-proposal - SLA upgrade"}',      now() - interval '20 days'),
    ('11111111-1111-1111-1111-000000000001','00000000-0000-0000-0000-000000000001','proposal_sent','{"version":2,"value":2400000}',                     now() - interval '15 days'),
    ('11111111-1111-1111-1111-000000000001','00000000-0000-0000-0000-000000000001','won',          '{"contract":"MSA-EGB-2026-v2"}',                    now() - interval '1 day'),
    ('11111111-1111-1111-1111-000000000003','00000000-0000-0000-0000-000000000002','call',         '{"summary":"Mashreq AUH demo"}',                    now() - interval '45 days'),
    ('11111111-1111-1111-1111-000000000003','00000000-0000-0000-0000-000000000002','won',          '{"contract":"MSA-MG-2026-v1"}',                     now() - interval '40 days'),
    ('11111111-1111-1111-1111-000000000004','00000000-0000-0000-0000-000000000001','proposal_sent','{"version":1,"value":980000}',                      now() - interval '5 days'),
    ('11111111-1111-1111-1111-000000000005','00000000-0000-0000-0000-000000000003','email',        '{"subject":"Intro + capability deck"}',             now() - interval '9 days'),
    ('11111111-1111-1111-1111-000000000007','00000000-0000-0000-0000-000000000002','stage_change', '{"from":"contacted","to":"qualified"}',             now() - interval '11 days');

  -- --- Contracts ------------------------------------------------------------
  INSERT INTO crm.contracts (partner_id, contract_type, status, signed_at, term_months, value_amount, currency_code, signed_by_name, signed_by_email) VALUES
    ('22222222-2222-2222-2222-000000000001','msa','signed', now() - interval '38 days', 36, 4080000, 'USD','Khalid Bin Hamoodah','khalid.h@mashreq.com'),
    ('22222222-2222-2222-2222-000000000002','msa','signed', now() - interval '1 day',   36, 7200000, 'USD','Sara Al Mansoori','cio@emiratesgold.ae'),
    ('22222222-2222-2222-2222-000000000002','sla','signed', now() - interval '1 day',   36,    null, 'USD','Sara Al Mansoori','cio@emiratesgold.ae');

  -- --- Audit log (representative entries) -----------------------------------
  INSERT INTO audit.audit_log (actor_id, entity_type, entity_id, action, after, created_at) VALUES
    ('00000000-0000-0000-0000-000000000001','partner','22222222-2222-2222-2222-000000000002','create',
     jsonb_build_object('legal_name','Emirates Gold Bank PJSC'), now() - interval '1 hour'),
    ('00000000-0000-0000-0000-000000000001','tenant','33333333-3333-3333-3333-000000000002','provision',
     jsonb_build_object('admin_email','cio@emiratesgold.ae','key_prefix','AIKI-7F2A'), now() - interval '1 hour'),
    ('00000000-0000-0000-0000-000000000002','lead','11111111-1111-1111-1111-000000000003','update',
     jsonb_build_object('stage','won'), now() - interval '40 days');

  -- --- Plan catalog (placeholder Stripe IDs - replace once Products exist) --
  INSERT INTO billing.plan_catalog (plan_code, display_name, stripe_product_id, stripe_price_id, billing_cycle, annual_amount, currency_code) VALUES
    ('basic',      'Basic - annual',      'prod_TBD_basic',      'price_TBD_basic_annual',      'annual',  504000, 'USD'),
    ('pro',        'Pro - annual',        'prod_TBD_pro',        'price_TBD_pro_annual',        'annual', 2400000, 'USD'),
    ('enterprise', 'Enterprise - annual', 'prod_TBD_enterprise', 'price_TBD_enterprise_annual', 'annual', 4920000, 'USD')
  ON CONFLICT (stripe_price_id) DO NOTHING;

  -- --- Subscriptions (fires the partner MRR/status sync triggers) -----------
  -- Mashreq: renews in ~25 days (lands in the "Renewals next 30d" KPI bucket).
  INSERT INTO billing.subscriptions
    (partner_id, plan_code, annual_amount, currency_code, billing_cycle, status, auto_renew,
     payment_method, started_at, current_period_start, current_period_end, last_renewal_at)
  VALUES
    ('22222222-2222-2222-2222-000000000001','pro',4080000,'USD','annual','active',true,
     'bank_transfer', now() - interval '11 months', now() - interval '11 months', now() + interval '25 days', now() - interval '11 months')
  ON CONFLICT (partner_id) DO NOTHING;

  -- Emirates Gold Bank: just onboarded, trialing.
  INSERT INTO billing.subscriptions
    (partner_id, plan_code, annual_amount, currency_code, billing_cycle, status, auto_renew,
     payment_method, started_at, current_period_start, current_period_end)
  VALUES
    ('22222222-2222-2222-2222-000000000002','enterprise',2400000,'USD','annual','trialing',true,
     'bank_transfer', now() - interval '1 hour', now() - interval '1 hour', now() + interval '1 year')
  ON CONFLICT (partner_id) DO NOTHING;

  -- Matching billed periods.
  INSERT INTO billing.subscription_periods
    (subscription_id, period_start, period_end, amount, currency_code, status, invoiced_at, paid_at, external_invoice_id)
  SELECT id, current_period_start, current_period_end, 4080000, currency_code, 'pending', current_period_start, current_period_start, 'INV-MG-2026-001'
    FROM billing.subscriptions WHERE partner_id = '22222222-2222-2222-2222-000000000001'
  ON CONFLICT DO NOTHING;
  INSERT INTO billing.subscription_periods
    (subscription_id, period_start, period_end, amount, currency_code, status, invoiced_at, external_invoice_id)
  SELECT id, current_period_start, current_period_end, 2400000, currency_code, 'pending', current_period_start, 'INV-EGB-2026-001'
    FROM billing.subscriptions WHERE partner_id = '22222222-2222-2222-2222-000000000002'
  ON CONFLICT DO NOTHING;

  -- A few Mashreq reminders so the Renewals UI has rows.
  INSERT INTO billing.renewal_reminders (subscription_id, kind, scheduled_for, recipient_email, delivery_status, sent_at, external_message_id)
  SELECT s.id, 'T-60', (s.current_period_end - interval '60 days')::date, 'khalid.h@mashreq.com', 'sent', s.current_period_end - interval '60 days', 'ses-msg-001'
    FROM billing.subscriptions s WHERE s.partner_id = '22222222-2222-2222-2222-000000000001'
  ON CONFLICT DO NOTHING;
  INSERT INTO billing.renewal_reminders (subscription_id, kind, scheduled_for, recipient_email, delivery_status, sent_at, external_message_id)
  SELECT s.id, 'T-30', (s.current_period_end - interval '30 days')::date, 'khalid.h@mashreq.com', 'opened', s.current_period_end - interval '30 days', 'ses-msg-014'
    FROM billing.subscriptions s WHERE s.partner_id = '22222222-2222-2222-2222-000000000001'
  ON CONFLICT DO NOTHING;
  INSERT INTO billing.renewal_reminders (subscription_id, kind, scheduled_for, recipient_email, delivery_status)
  SELECT s.id, 'T-14', (s.current_period_end - interval '14 days')::date, 'khalid.h@mashreq.com', 'queued'
    FROM billing.subscriptions s WHERE s.partner_id = '22222222-2222-2222-2222-000000000001'
  ON CONFLICT DO NOTHING;

  RAISE NOTICE '  ok: demo seed applied to %', current_database();
END
$seed$;

\echo 'ok: 0800 demo seed done.'
