# AI Chat + MCP Operational Governance for ParlorPrediction

## Purpose

This document defines the safe architecture for introducing AI-assisted operational workflows into `ParlorPrediction` without breaking carryover truth, clean architecture boundaries, or human accountability.

It is intentionally written before chat implementation so the team can agree on:

- what the AI may read
- what the AI may explain
- what the AI may draft
- what the AI may never save by itself
- how approval, audit, backup, rollback, and security must work

Related existing business references:

- [WEEKLY_DOUGH_CLOSING_RULES.md](/C:/Users/Hombr/source/repos/ParlorPredictions/docs/dough-quality/WEEKLY_DOUGH_CLOSING_RULES.md)
- [END_TO_END_WEEKLY_CARRYOVER_FLOW.md](/C:/Users/Hombr/source/repos/ParlorPredictions/docs/dough-quality/END_TO_END_WEEKLY_CARRYOVER_FLOW.md)
- [2026-06-23_WEEKLY_CLOSING_OPERATIONAL_HISTORY.md](/C:/Users/Hombr/source/repos/ParlorPredictions/docs/dough-quality/2026-06-23_WEEKLY_CLOSING_OPERATIONAL_HISTORY.md)
- [ADMIN_DOUGH_CORRECTION_TOOLS.md](/C:/Users/Hombr/source/repos/ParlorPredictions/docs/dough-quality/ADMIN_DOUGH_CORRECTION_TOOLS.md)

## Executive Decision

The recommended production design is:

`ParlorPrediction UI or API -> AI orchestration layer -> internal MCP server -> application services -> repositories`

Not recommended as the main production path:

`ParlorPrediction -> Codex or ChatGPT session -> internal MCP -> database`

`Codex` is excellent for design, debugging, supervised review, prompt iteration, and scenario testing. It should not be the required runtime bridge for live kitchen operations.

## Operational Cut V1

The approved first implementation cut is intentionally smaller than the final architecture vision.

Keep as-is:

- no `ParlorPrediction.Api` project
- no separate `ParlorPrediction.AI` project yet
- no DTO migration out of `ParlorPrediction.Contracts`
- no event sourcing yet
- no new bounded contexts split across the solution yet

Add now:

- `src/ParlorPrediction.Mcp`
- `Application/Services/AIOrchestration`
- `Application/Services/OperationalSimulation`
- `Application/Services/OperationalDrafts`
- `Domain/Entities/OperationalDraft`
- `Domain/Entities/OperationalAuditEntry`

This is the correct MVP because it extends the current architecture instead of replacing it.

## Why This Decision Is Better

### Best use of Codex / ChatGPT

Use `Codex` or `ChatGPT` for:

- designing prompts and tool schemas
- reviewing ambiguous operational narratives
- validating tricky carryover stories with a human in the loop
- generating new regression examples
- helping evolve the governance rules

### Best use of the internal MCP

Use the internal `MCP` server for:

- controlled read access to operational data
- deterministic business explanations
- draft generation with structured outputs
- validation before a human approval
- tool-level logging and auditability

### Why not depend on Codex as middleware in production

Making a live kitchen workflow depend on an interactive `Codex` session would create avoidable problems:

- runtime dependency on an external human-facing session
- weaker operational audit boundaries
- harder service authentication and authorization
- more moving parts during outages
- less predictable latency and reliability

## Scope

This governance covers AI-assisted workflows related to:

- `Weekly Closing`
- `Daily Closing` interpretation
- `Dough Inventory`
- `Dough Usage`
- `Prep Tasks`
- `BallDough`
- `MakeDoughLoad`
- carryover explanation and correction drafts

This governance does not authorize:

- direct AI database mutation
- raw SQL generation by the model
- direct delete tools
- automatic approval by the model
- self-modifying business rules

## Operational Principle

The AI may:

- read
- explain
- suggest
- calculate
- validate
- prepare drafts

The AI may not:

- approve
- save live corrections by itself
- bypass role checks
- bypass backups
- bypass audit logging

Golden rule:

`Only an authorized human confirms real changes.`

## Conceptual Phases vs Implementation Order

The user-facing concept and the implementation order should not be identical.

### User-facing concept

1. `Chat read-only`
2. `Chat with drafts`
3. `Internal MCP`
4. `Controlled learning`

### Recommended implementation order

1. `Governance document and roadmap`
2. `Internal MCP read-only`
3. `Internal MCP draft tools`
4. `Validation, audit, backup, rollback, SSD hardening`
5. `Chat read-only`
6. `Chat with drafts`
7. `Controlled learning from approved outcomes`

This order is safer because the tool boundary becomes trustworthy before the natural-language surface is exposed.

For `Operational Cut V1`, implementation starts at steps `2` through `4` with a deterministic internal MVP before any boss-facing chat is exposed.

## Roles And Permissions

### Boss / Operations Lead

Can:

- describe what happened in natural language
- request explanation
- request a draft
- review the AI interpretation

Cannot:

- save final changes unless separately authorized in the app

### Manager

Can:

- use the chat
- review calculations
- review draft proposals
- add human notes

Cannot:

- execute destructive admin-only corrections unless already allowed by current domain rules

### Admin

Can:

- review drafts
- approve or reject drafts
- execute correction flows already permitted by application policy
- trigger backup-confirm-save workflow

### MCP Service Identity

Can:

- call only the allowlisted application services
- create draft artifacts
- validate a proposal

Cannot:

- directly mutate production entities outside approved application use cases
- bypass role checks using service-level privilege

## Allowed MCP Tools

Initial tool allowlist:

- `read_weekly_closing`
- `read_dough_inventory`
- `explain_weekly_goal`
- `simulate_operational_narrative`
- `draft_weekly_correction`
- `draft_dough_task`
- `validate_closing_before_save`

### Explicitly forbidden in phase 1 and phase 2

- `update_database`
- `delete_task`
- `delete_weekly_closing`
- `run_sql`
- `approve_draft`
- `auto_correct_inventory`

Human approval should stay outside the model toolset. Approval belongs to the application workflow and role-protected UI/API.

## Read vs Write Boundary

### Read-only actions

Safe for AI:

- load weekly closing history
- load carryover inputs
- load current dough inventory summary
- explain how weekly goal is being calculated
- explain why a load does or does not count as available dough
- detect likely inconsistency between tasks, batches, and closing

### Draft actions

Still safe for AI if stored as pending drafts:

- propose a weekly closing correction
- propose a task backfill
- propose an explanatory note
- produce a before/after preview
- produce validation warnings

### Write actions

Must remain human-confirmed:

- creating a real corrected `WeeklyClosing`
- saving a live correction to `PrepTask`
- voiding a batch
- changing inventory-affecting records

## Clean Architecture Placement

The internal MCP should not contain business logic that duplicates `Application`.

Recommended placement:

- `Domain`: invariants and business rules remain here
- `Application`: orchestration, validation, use cases, draft builders
- `Persistence`: repositories and draft/audit storage
- `MCP host`: transport adapter only
- `MVC or API`: human approval and review surfaces

### Important rule

The `MCP` host translates tool input into application requests. It must not invent alternate rules for carryover, batch availability, or weekly closing math.

The same rule applies to the AI orchestration layer:

- the AI does not calculate another truth
- the AI classifies, simulates, drafts, and explains on top of existing services
- `WeeklyClosing`, inventory, weekly goal, and carryover math remain owned by current application logic

## Recommended Internal Components

### 1. MCP Host Project

Suggested future project:

- `src/ParlorPrediction.Mcp`

Responsibility:

- register tools
- authenticate caller identity
- authorize tool access
- map structured tool DTOs to application services
- attach correlation IDs and audit metadata

### 2. AI Orchestration Layer

Suggested future application services:

- `OperationalNarrativeInterpreter`
- `OperationalIntentClassifier`
- `OperationalSimulationService`
- `WeeklyClosingExplanationService`
- `WeeklyCorrectionDraftService`
- `DoughTaskDraftService`
- `ClosingValidationService`

Responsibility:

- interpret normalized operational facts
- compose tool calls
- build structured drafts
- never save final corrections directly

Internal intent classes for v1:

- `SalesIntent`
- `ProductionIntent`
- `ConsumptionIntent`
- `InventoryIntent`
- `WeeklyClosingIntent`

### 3. Draft Store

Recommended future persistence concept:

- `OperationalDraft`
- `OperationalDraftDecision`
- `OperationalAuditEntry`

For `Operational Cut V1`, persistence can remain minimal at first as long as draft and audit structures are defined cleanly and the save workflow is still blocked behind human approval.

Each draft should store:

- draft type
- source natural-language request
- normalized interpretation
- proposed fields
- before snapshot
- after preview
- validation result
- created by
- reviewed by
- approved or rejected at
- final linked entity IDs if executed

## Security By Design Requirements

The internal MCP must be designed as untrusted-input software, because natural language is not trustworthy input.

Mandatory controls:

- strict DTO schemas for every tool
- no free-form SQL or repository access from prompts
- least-privilege service account
- role checks revalidated in application layer
- correlation ID per tool call and per approval flow
- immutable audit entries for request, draft, decision, and save
- backup required before important data-changing approvals
- before/after preview required before approval
- documented rollback procedure for each correction type
- idempotency protections for approval endpoints
- rate limiting and auth checks on external-facing chat endpoints
- secret isolation so prompts never receive connection strings or raw credentials
- prompt injection resistance by treating user narrative as data, not instructions for tool expansion

## Backup, Preview, Approval, Rollback

Before every important correction:

1. Create or verify a database backup.
2. Record AI intent.
3. Show before state.
4. Show proposed after state.
5. Show validation warnings.
6. Require human approval.
7. Save through existing application use cases.
8. Record audit trail.
9. Generate or link rollback procedure.

For `Operational Cut V1`, the audit trail minimum is:

- `OperationalDraft`
- `OperationalAuditEntry`
- diff JSON preview
- validation warning JSON
- human approval still outside the model toolset

Minimum audit fields:

- `DraftId`
- `CorrelationId`
- `ToolName`
- `PromptIntent`
- `NormalizedInterpretation`
- `ApproverUserId`
- `ApprovedAtUtc`
- `TargetEntityIds`
- `Reason`
- `BackupReference`
- `RollbackReference`

## Validation Rules Before Save

`validate_closing_before_save` should fail or warn when:

- more than one closing would exist for the same week
- a proposed carryover duplicates an already-counted load
- `LeftoverMixedLoads` is greater than zero while the same load is already represented as balled
- `BallDough` and `MakeDoughLoad` would cause the same physical dough to count twice
- the narrative implies Monday balling of a Sunday load, but the proposal leaves it pending
- proposed ready balls do not reconcile with known line-to-ball conversion assumptions
- the acting role is not permitted for the eventual correction path

## Real Example Mapping

### Example A

Natural language:

`Esta semana sobraron 3 lineas y no quedo carga pendiente.`

Expected AI interpretation:

- `3 lineas = 3 loads = 504 ready balls`
- `LeftoverReadyBalls = 504`
- `LeftoverMixedLoads = 0`
- explanation only in read-only mode
- draft only in draft mode

Expected response in phase 1 chat:

- `Entiendo: 504 ready balls y 0 mixed loads pendientes.`
- `Puedo preparar un borrador de correccion para Weekly Closing si lo deseas.`

### Example B

Natural language:

`El domingo se hizo una carga y el lunes se boleo.`

Expected normalized rule outcome:

- do not count it as a still-pending mixed load
- do count it as ready carryover if it physically remained available
- attach a human note explaining that Monday completed the prior Sunday recovery story
- verify it is not duplicated across `MakeDoughLoad`, `BallDough`, `DoughBatch`, and closing carryover

### Example C

Operational history already documented in this repo

For the week `Jun 15 - Jun 21, 2026`, the authoritative corrected state is:

- `LeftoverReadyBalls = 504`
- `LeftoverMixedLoads = 0`
- reason: `Recovered Sunday load balled Monday morning.`

This exact scenario should become one of the first MCP regression fixtures.

## MVP Decision Summary

The approved MVP is not a chatbot-first release.

It is:

- internal `MCP`
- deterministic intent classification
- simulation over existing truth
- draft generation
- diff preview
- minimal audit model

The chat UI comes later.

## Learning Model

The AI must not "learn" by silently rewriting rules or prompts in production.

Allowed learning inputs:

- admin-approved recommendations
- structured operational notes
- reviewed correction history
- labeled examples of good and bad interpretations
- audited decisions linked to final outcomes

Recommended learning loop:

1. Human states what happened.
2. AI interprets and explains.
3. AI produces structured draft.
4. Human approves or rejects.
5. System stores outcome and rationale.
6. Approved examples are promoted into reference fixtures, prompt examples, or policy notes.

## Testing Strategy

### Unit tests

- mapping from natural-language interpretation result to draft DTO
- carryover validation logic
- duplicate-load detection
- approval policy enforcement

### Integration tests

- MCP tool contract tests
- application service wiring tests
- draft persistence tests
- audit logging tests

### Regression tests

Use real operational stories from:

- `Weekly Closing`
- `Dough Used`
- `BallDough`
- `MakeDoughLoad`
- carryover reconstruction

The first mandatory regression example should be the `Sun load -> Mon balling -> 504 ready balls` scenario.

### Security tests

- unauthorized caller cannot approve
- model cannot call non-allowlisted tools
- malformed narrative cannot bypass DTO validation
- duplicate approval requests are idempotent

## Debate: Best Integration Topology

### Option A: Parlor app calls Codex or ChatGPT, which calls the MCP

Pros:

- fast experimentation
- great for supervised debugging
- easier during design phase

Cons:

- wrong dependency for production runtime
- weaker operational ownership boundary
- harder to guarantee stable audit and retries
- chat session becomes part of production availability

### Option B: Parlor app calls OpenAI directly, and the app uses the internal MCP

Pros:

- best production architecture
- strongest audit and authorization boundary
- clean ownership inside the product
- easier to test and secure

Cons:

- more implementation work up front
- requires prompt/version management inside the product

### Recommended answer

Use a hybrid:

- `Codex` remains the design and supervision partner
- `ParlorPrediction` owns the production AI runtime
- the production runtime calls your model provider and your internal MCP
- approved examples from real operations are fed back into prompt assets and regression tests

This gives you both safety and speed.

## Decision Summary

The next implementation milestone should be:

1. build the internal `MCP` first
2. keep it read-only and draft-only at the tool level
3. add validation, backup, preview, rollback, and audit
4. only then expose the natural-language chat surface

That sequence matches clean architecture, protects carryover truth, and avoids opening security holes too early.
