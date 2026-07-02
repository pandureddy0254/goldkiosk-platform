---
description: Review current branch changes with the code-reviewer agent
argument-hint: [base branch, default main]
---

Invoke the **code-reviewer** agent on all changes of the current branch against
${ARGUMENTS:-main}. Present the verdict and findings verbatim, then propose a fix
plan for blockers — do not implement until I confirm.
