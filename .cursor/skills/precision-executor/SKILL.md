---
name: precision-executor
description: >-
  Forces strict scoping, failure-history analysis, and minimal-viable-action cycles to accelerate problem-solving and break out of execution loops. Use when a task has failed repeatedly, when execution is slow, or when broad planning is causing context bloat.
---

# Precision Executor (The Loop Breaker)

When a task is failing repeatedly or taking too long, abandon broad planning and adopt the **Precision Executor** protocol. This skill prioritizes speed and accuracy by limiting context and enforcing empirical proof before action.

## When to apply

- The user mentions an issue has been attempted multiple times without success.
- Execution feels slow, or you are reading too many files at once (context bloat).
- You are "spinning your wheels" or guessing at solutions without confirming the root cause.

## Rules

1. **Document the Dead Ends** — Before taking any new action on a recurring issue, you must explicitly list:
   - What was already tried?
   - Why did it fail?
   - What is the new hypothesis?
   *Do not repeat failed approaches.*

2. **Micro-Planning (Max 2 Steps)** — Replace 5+ step plans with **Micro-Plans**.
   - Plan a maximum of 2 steps at a time.
   - Execute, verify the result, and then plan the next 2 steps.
   - Stop immediately if a step fails; do not proceed with the plan.

3. **Minimal Viable Context** — Do not read full files unless absolutely necessary.
   - Use `grep_search` and `glob` with narrow scopes.
   - If a file is large, read only the specific line ranges containing the targeted logic.
   - Never read a file "just in case."

4. **No Speculative Fixes** — If the root cause is ambiguous, do not attempt a fix.
   - Your first action MUST be to run a test, command, or script that outputs the exact error.
   - Once the error is verified, apply a surgical fix.

5. **Surgical Edits** — Change only the exact lines causing the problem.
   - Strictly avoid "refactoring while I'm in here."
   - Prioritize the simplest possible fix that passes the verification step.

## Quick Pattern

```markdown
### 🛑 Anti-Loop Check
- **Previously Tried:** [List failed attempts]
- **Why it failed:** [Technical reason]
- **New Hypothesis:** [Precise reason for current failure]

### ⚡ Micro-Plan
1. [Targeted action to prove hypothesis or apply surgical fix]
2. [Verification command/test]
```
