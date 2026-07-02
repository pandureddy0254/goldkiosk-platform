---
description: Run the full architect -> developer -> test-writer -> code-reviewer loop for one ticket
argument-hint: <ticket-id or ticket description>
---

Execute the standard GoldKiosk delivery loop for: $ARGUMENTS

1. Create/confirm feature branch feature/<ticket>-<slug>.
2. If the change spans multiple projects or touches Contracts, data model, config
   layering, or packaging: invoke the **architect** agent for a design note. Show it
   to me and WAIT for approval.
3. **developer** implements per the note and standards.
4. **test-writer** covers the change (NUnit + Moq + FluentAssertions only).
5. **code-reviewer** reviews the full diff.
6. On REQUEST-CHANGES: developer fixes blockers, test-writer updates coverage,
   re-review. Repeat until APPROVE / APPROVE-WITH-NITS.
7. Verify dotnet build GoldKiosk.slnx -warnaserror and dotnet format --verify-no-changes.
   Do NOT run dotnet test.
8. Commit (conventional message) and summarize: files, contract/config deltas,
   behaviors covered, remaining nits, and what I should verify in Visual Studio.
