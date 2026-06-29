# MCP Internal Roadmap Checklist

## Working Rules

- No separate worktree.
- Use a dedicated branch with commits by intention.
- Keep clean architecture boundaries intact.
- Treat natural language as untrusted input.
- Do not add direct write or delete tools to the model surface.

## Current Branching Intent

- Recommended branch name for this stream: `codex/mcp-operational-governance`

## Phase 0 - Governance First

- [x] Create technical governance document for `AI Chat + MCP Operational Governance`.
- [x] Create roadmap checklist for phased delivery.
- [ ] Approve the initial MCP tool allowlist.
- [ ] Approve roles and human approval boundary.
- [ ] Approve backup, audit, and rollback policy.
- [ ] Freeze the first set of operational regression stories.

Definition of done:

- the team agrees what the AI can read, explain, draft, and never save alone

## Phase 1 - Internal MCP Read-Only Foundation

- [ ] Create `src/ParlorPrediction.Mcp` host project.
- [ ] Register only read-safe tool contracts.
- [ ] Add structured DTOs for every tool input and output.
- [ ] Wire `read_weekly_closing` to `IWeeklyDoughClosingReadService`.
- [ ] Wire `read_dough_inventory` to the existing inventory read path.
- [ ] Wire `explain_weekly_goal` to application services without duplicating business logic.
- [ ] Add correlation ID and tool audit envelope to every call.
- [ ] Block any non-allowlisted tool registration by default.

Definition of done:

- a caller can safely read and explain operational state through MCP without changing live data

## Phase 2 - Draft Layer

- [ ] Design `OperationalDraft` persistence model.
- [ ] Add `draft_weekly_correction`.
- [ ] Add `draft_dough_task`.
- [ ] Add `validate_closing_before_save`.
- [ ] Store natural-language source text with normalized interpretation.
- [ ] Store before snapshot and after preview in each draft.
- [ ] Return warnings for duplicate-load and carryover conflicts.

Definition of done:

- the AI can prepare a correction proposal without writing to production entities

## Phase 3 - Approval And Save Workflow

- [ ] Create admin review surface in MVC or API.
- [ ] Require explicit human approval for each draft execution.
- [ ] Verify database backup exists before important save operations.
- [ ] Save only through existing application use cases.
- [ ] Record approver, timestamp, reason, and target entities.
- [ ] Link each approval to rollback instructions or rollback artifact.
- [ ] Add idempotency protection to approval execution.

Definition of done:

- a draft can become a real correction only through a traceable human approval workflow

## Phase 4 - Tests And SSD Hardening

- [ ] Add unit tests for interpretation mapping and validation.
- [ ] Add MCP contract tests.
- [ ] Add audit logging tests.
- [ ] Add authorization tests for admin-only and manager-safe paths.
- [ ] Add duplicate-load regression tests.
- [ ] Add prompt-injection and malformed-input negative tests.
- [ ] Add rollback procedure tests where feasible.

Definition of done:

- the MCP path is covered functionally and defensively before chat exposure

## Phase 5 - Chat Read-Only

- [ ] Add chat endpoint or adapter for natural-language interpretation.
- [ ] Restrict phase 5 chat to read-only and explanation behavior.
- [ ] Show explicit calculation output before any draft offer.
- [ ] Normalize kitchen language like `lineas`, `cargas`, `boleado`, and carryover terms.
- [ ] Log interpreted intent and confidence markers for review.

Definition of done:

- the boss can describe what happened and receive a trustworthy explanation without changing data

## Phase 6 - Chat With Drafts

- [ ] Allow chat to create `OperationalDraft` artifacts only.
- [ ] Show before and after preview inside the review flow.
- [ ] Let admin approve or reject each draft.
- [ ] Save human note with every accepted correction.
- [ ] Reject silent execution paths.

Definition of done:

- the chat becomes an assistant for draft creation, not an autonomous operator

## Phase 7 - Controlled Learning

- [ ] Store approved and rejected examples with labels.
- [ ] Classify corrections by pattern such as carryover, late balling, stale pending load, or duplicate source.
- [ ] Promote approved examples into prompt fixtures and regression tests.
- [ ] Add admin-curated operational notes store.
- [ ] Review learning assets periodically before promotion.

Definition of done:

- the system improves from supervised history, not from unsupervised self-modification

## Initial Regression Stories To Freeze Early

- [ ] `Sun Jun 21 load -> Mon Jun 22 balling -> LeftoverReadyBalls = 504 -> LeftoverMixedLoads = 0`
- [ ] `3 lineas sobraron -> 504 ready balls`
- [ ] `Mixed load carries over but does not count as available until BallDough`
- [ ] `Same physical dough appears in MakeDoughLoad + BallDough + DoughBatch but must count once`
- [ ] `Manager can review dashboard state but admin-only destructive corrections stay restricted`

## Commit Plan By Intention

- [x] `docs: add AI chat and MCP operational governance`
- [ ] `feat(mcp): scaffold internal MCP host and read-only tool contracts`
- [ ] `test(mcp): add MCP contract and regression coverage`
- [ ] `feat(drafts): add operational draft model and validation services`
- [ ] `feat(approval): add admin approval, audit, backup, and rollback flow`
- [ ] `feat(chat): add read-only operational chat adapter`
- [ ] `feat(chat): add draft-generation chat flow`
- [ ] `feat(learning): add supervised example feedback loop`

## First Implementation Recommendation

Build phases `1` through `4` before exposing the boss-facing chat. That gives us:

- stable tool boundaries
- predictable auditability
- safer approvals
- less security debt
