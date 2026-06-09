# Dough Quality Screen Flow

## Scope

This document translates the Dough Quality business flow into future screens.

It does not implement UI.
It defines what each screen must communicate, what action it supports, and what should be intentionally hidden to avoid overload.

## Screen 1: Dough Prep Home

### Objective

Give the manager a one-minute operational summary and one clear next step.

### Primary user

Manager

### Visible data

- selected target date
- `Need Today`
- `Good Dough`
- `Attention Dough`
- `Need To Make`
- `Must Use Next Day` alert, if quantity exists
- short human recommendation
- optional secondary line for `Total Available Dough`
- optional timestamp for latest recommendation or last update

### Actions available

- `Review Old Dough`
- `Create Reball Task`
- `Create Prep Task`
- `Print Kitchen Sheet`
- change target date

### Errors and validations

- if summary data cannot load, show one calm message with a retry action
- if date is missing or invalid, default to today
- if the recommendation is unavailable, keep the summary visible and show a short fallback note

### Expected backend call

- existing Dough Prep page composition
- future `GET /prep/dough-quality/summary?targetDate=YYYY-MM-DD`

The future summary endpoint should compose:

- existing planning numbers from Dough Prep
- existing human recommendation
- Dough Quality summary values

Implementation note:

- current `DoughQualityReadService.GetSummaryAsync` is global, so target-date-specific summary behavior belongs to future controller composition or a later read-model refinement

### What this screen must not show

- dense record tables above the summary cards
- full batch history by default
- multi-step forms
- too many colored badges
- raw enum names without human wording

## Screen 2: Dough Quality Review

### Objective

Help a manager review older dough quickly and decide whether it stays usable, needs attention, or needs correction.

### Primary user

Manager or Admin

### Visible data

- page title in plain language such as `Review Older Dough`
- filter group:
  - reference date
  - created/balled from
  - created/balled to
  - reballed from
  - reballed to
  - optional status filter
- large candidate cards showing:
  - batch id or short label
  - source date
  - created or balled date
  - age in days
  - current status
  - candidate reason
  - quantity in balls

### Actions available

- `Mark Attention`
- `Correct Status`
- `Open Discard`
- return to Dough Prep Home

### Errors and validations

- reference date is required to evaluate candidates
- if no candidates exist, show a reassuring empty state
- status correction must explain that only admin can finalize it
- if a batch was already changed by another user, show a refresh prompt

### Expected backend call

- future `GET /prep/dough-quality/attention-candidates`
- optional controller composition using:
  - `EvaluateDoughAttentionCandidatesAsync`
  - `SearchAsync`

### What this screen must not show

- a spreadsheet-like wall of columns
- unrelated prep tasks
- weekly dashboard analytics
- advanced admin metadata first

## Screen 3: Reball Dough Task

### Objective

Guide a PizzaMaker through a safe and simple reball workflow.

### Primary user

PizzaMaker

### Visible data

- task title such as `Reball Dough`
- batch label
- original quantity before reball
- short instruction text
- recovered quantity input
- automatic lost quantity preview after input
- final outcome note:
  - `Recovered dough will be marked Must Use Next Day`

### Actions available

- `Save Reball Result`
- `Cancel`
- manager-only fallback action `Discard Instead`

### Errors and validations

- recovered quantity must be greater than zero for partial recovery
- recovered quantity must be less than original quantity
- recovered quantity cannot be blank
- if the user selects discard instead, discard reason becomes required

### Expected backend call

- future `POST /prep/dough-quality/reball`

### What this screen must not show

- long historical batch logs
- analytics charts
- multiple task types on the same form
- technical field names like `PartialRecovered` without friendly wording

## Screen 4: Discard Dough

### Objective

Let Manager or Admin discard dough in a deliberate, auditable way.

### Primary user

Manager or Admin

### Visible data

- batch label
- current quantity
- current status
- reason selector with plain labels
- optional note field
- warning message that discarded dough stops counting as available
- note if the first release only supports full-batch discard

### Actions available

- `Confirm Discard`
- `Go Back`

### Errors and validations

- only Manager or Admin can submit
- discard reason is required
- if the first MVC version keeps the current backend behavior, the screen should clearly confirm that the whole tracked batch will be discarded
- if the batch is already discarded, block duplicate submission

### Expected backend call

- future `POST /prep/dough-quality/discard`

Implementation note:

- current backend discard behavior is full-record discard, not partial-quantity discard
- if partial discard is required later, that should be a separate backend enhancement instead of hidden controller behavior

### What this screen must not show

- unrelated dashboard widgets
- too many optional fields
- raw internal ids as the main headline
- ambiguous danger actions

## Screen 5: Kitchen Sheet / Print View

### Objective

Produce a large, printable kitchen sheet that older staff can read at a glance.

### Primary user

Manager prints it.
PizzaMaker and kitchen staff read it.

### Visible data

- date
- `Need Today`
- `Good Dough`
- `Attention Dough`
- `Need To Make`
- `Must Use Next Day` warning if present
- human recommendation in one short paragraph
- section called `Pay Attention Today`
- section called `Mix / Prep Today`

### Actions available

- `Print`
- `Back to Dough Prep`

### Errors and validations

- if some data is missing, print the sheet with a visible warning instead of hiding the whole page
- avoid paginated print output when possible

### Expected backend call

- no new Dough Quality write call is required
- print view should compose already-available Dough Prep data with future Dough Quality summary data

### What this screen must not show

- tiny tables
- small footer metadata
- hidden legends that require training
- decorative UI that wastes print space

## Screen 6: Loss Analytics Preview

### Objective

Give managers a simple preview of dough losses before a fuller dashboard exists.

### Primary user

Manager or Admin

### Visible data

- total lost balls for selected date range
- losses by reason
- losses by day or week
- plain-language insight placeholder such as:
  - `Most losses came from OverProduced this week`

### Actions available

- change date range
- filter by reason
- return to Dough Prep

### Errors and validations

- if no losses exist, show a meaningful empty state
- if date range is invalid, show a short correction message

### Expected backend call

- future `GET /prep/dough-quality/loss-analytics`

### What this screen must not show

- predictive AI claims
- dense business-intelligence controls
- every historical batch row by default
- multiple chart types competing at once

## Navigation Order

The recommended default navigation is:

1. Dough Prep Home
2. Dough Quality Review, if risk exists
3. Reball Dough Task or Discard Dough, depending on decision
4. Kitchen Sheet / Print View
5. Loss Analytics Preview for follow-up, not for urgent kitchen work

## Overload Guardrails

Across all Dough Quality screens:

- cards should come before tables
- one main action should be visually dominant
- short text should explain status in plain language
- destructive actions should require confirmation
- status should never rely on color alone
