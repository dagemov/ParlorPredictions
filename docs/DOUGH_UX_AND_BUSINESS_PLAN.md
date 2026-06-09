# Dough UX And Business Plan

## Why this plan exists

The current product already supports the main dough workflow:

1. Review a day
2. Calculate dough guidance
3. Save the recommendation
4. Convert the recommendation into a dough task
5. Complete the task

That flow is useful, but it still misses an operational reality from the kitchen:

- dough carries over from one day and one week to the next
- some leftover dough is still usable but needs attention
- some leftover dough must be re-balled
- some leftover dough must be discarded with a reason
- older staff need a much simpler and more legible screen than a dense task table

This document defines the next business and UX layer before generating a better prototype in Stitch.

## Current system behavior

### What already exists

- The app reads the latest inventory snapshot on or before the selected date and uses it as available dough.
- The app can calculate a dough recommendation for a date.
- The app can save that recommendation as an auditable snapshot.
- The app can turn the saved recommendation into a dough task.
- Managers can already create manual dough tasks day by day.
- Managers can already save human recommendations as notes.

### What is still missing

- There is no explicit carryover workflow between operational weeks.
- There is no first-class concept for "attention dough".
- There is no specific reball task type.
- There is no discard outcome with a required reason.
- There is no day-close reconciliation screen for leftovers.
- There is no print-friendly PDF summary for kitchen use.

## Business rules to add

### 1. Daily opening and closing dough state

Every day should have a simple dough reconciliation step with these buckets:

- `ready_good`: dough ready to use with no warning
- `ready_attention`: dough that still counts as available but needs special attention
- `new_unready`: fresh dough still fermenting
- `reserved`: dough held for a future event or service need
- `used_today`: dough actually consumed
- `discarded`: dough thrown away

### 2. Carryover rule between days and weeks

The next day starts from the previous closing state.

Suggested rule:

`opening_available = previous_ready_good + previous_ready_attention + reballed_recovered - discarded`

Important product rule:

- carryover must work the same way whether the next date is the next day in the same week or the first day of a new week
- changing the calendar week must never reset dough by accident

### 3. Attention dough rule

Some leftover dough still counts in `dough finished` / available inventory, but it must be flagged.

Example:

- last week ended with `14 cases` leftover
- `6 cases` need reball attention
- those `6 cases` still count in available dough
- but they must appear with a visible warning and require follow-up

Suggested flags:

- `attention_reason`: age, sun exposure, event handling, texture, manager concern, other
- `attention_notes`: free text

### 4. Reball task rule

When dough is marked as attention dough, the system should create a `Reball` dough task.

Suggested task types:

- `Make Dough`
- `Ball Dough`
- `Reball Dough`
- `Discard Dough`
- `Check Event Dough`

For a `Reball Dough` task, completion should require an outcome:

- `reballed`
- `discarded`
- `partial`

If the outcome is `discarded`, the user must enter a reason.

Suggested discard reasons:

- spoiled by sun / heat
- over-fermented
- contamination
- event handling issue
- damaged during transport
- manager decision
- other

### 5. Human recommendation rule

Human recommendations should not just be freeform notes.

They should support structured operational overrides such as:

- "Carry over 1 full load from last week"
- "6 cases require reball"
- "2 cases must be discarded because they were exposed to sun"
- "Use older dough first"

Recommended behavior:

- keep the existing free-text note
- add structured override fields
- make human guidance visible inside Dough Prep, not in a separate screen only

## Proposed data model direction

This can be implemented in phases.

### Phase 1: minimal extension

Keep the current inventory snapshot, but add:

- `attention_balls`
- `attention_reason`
- `attention_notes`

And extend tasks with:

- `task_type`
- `completion_outcome`
- `discard_reason`
- `discarded_balls`
- `reballed_balls`

### Phase 2: cleaner operational model

Split dough movement into explicit transactions:

- opening carryover
- recommendation output
- manual manager override
- task completion
- reball recovery
- discard event

That would make auditing much easier later.

## UX plan for older kitchen users

The Dough Prep page should stop feeling like a data screen and start feeling like a guided work board.

### Main principle

One screen, one story, one next action.

### New top section

Replace dense summaries with four oversized numbers:

- `Needed Today`
- `We Have Now`
- `Need Attention`
- `What To Do Next`

The fourth card should use plain language, for example:

- `No action needed`
- `Make 1 full load today`
- `Reball 6 cases first`
- `Throw away 2 cases and record reason`

### New action strip

Primary buttons should be large and ordered:

1. `Calculate`
2. `Save Plan`
3. `Create Task`
4. `Print`

Secondary actions:

- `Weekly View`
- `Manager Note`
- `Help`

### Simplify the dough task experience

Current tables can stay for admin detail, but kitchen staff should first see task cards.

Each task card should show:

- action verb
- quantity in the unit they understand
- why it matters
- one giant completion button

Example card:

- `Reball 6 cases`
- `These came from last week and need attention before service`
- `Mark as Reballed`
- `Mark as Discarded`

### Make status readable at a glance

Use only a few strong statuses:

- `OK`
- `Do Today`
- `Attention`
- `Done`
- `Discarded`

Avoid showing too many neutral labels at once.

### Hide advanced details by default

Keep calculations and tables behind a `View details` section for admins and power users.

## Admin workflow plan

The current product already allows a manager/admin to create manual dough tasks day by day.

That should remain, but improve it in two ways:

### Option A: manual daily control

Admin creates and adjusts tasks manually each day:

- make dough
- ball dough
- reball dough
- discard dough

Best when operations are highly variable.

### Option B: hybrid recommended flow

System generates task suggestions from the recommendation and carryover state, then the admin confirms or edits them.

Recommended generated tasks:

- make dough if shortage exists
- reball dough if attention dough exists
- discard task if dough was flagged as unusable

Recommended product direction:

- use the hybrid flow as default
- keep manual override available at all times

## Print / PDF plan

Add a `Print PDF` action on Dough Prep and Weekly View.

The printout should be kitchen-first, not management-first.

### PDF content

- target date
- day of week
- dough needed today
- dough available now
- dough still missing
- attention dough with reason
- tasks for the shift
- manager note

### PDF layout

- large font
- high contrast
- very little decoration
- one page when possible

### Suggested buttons

- `Print Kitchen Sheet`
- `Download PDF`

## Recommended implementation phases

### Phase 1: clarify workflow without major schema change

- redesign Dough Prep screen
- expose human recommendation more clearly
- add print-friendly layout
- keep manual task creation

### Phase 2: add carryover and attention dough

- add attention dough fields
- show carryover explicitly on the screen
- create reball task suggestions

### Phase 3: add reball and discard outcomes

- extend task model with task type and completion outcome
- require discard reason
- show reballed versus discarded in reporting

### Phase 4: refine weekly logic

- make week transitions explicit
- show carried dough from prior week
- avoid misleading week totals based only on one selected snapshot

## Better Stitch prompt

Use this prompt after aligning on the business rules above:

```text
Design a responsive web app prototype for a pizza restaurant dough workflow called Parlor Prediction.

Primary users:
- Manager
- Pizza maker
- Older kitchen staff with low system comfort

Main goal:
Help the kitchen understand, in very plain language, what dough is needed today, what dough is already available, what leftover dough needs attention, and what action must happen next.

Core workflow:
1. Dashboard with today and weekly summary
2. Dough Prep page for one selected date
3. Saved recommendation with clear next-step language
4. Carryover dough from prior days and prior week
5. Attention dough section for leftover dough that still counts as available but needs follow-up
6. Reball task flow with outcomes: reballed or discarded
7. Task board for kitchen execution
8. Print-friendly kitchen sheet / PDF

Business rules to reflect:
- Dough can carry over from one day or week to the next
- Some leftover dough is still usable but needs attention
- Example: if 14 cases remain and 6 need reball, those 6 still count in available dough but must be shown as attention dough
- If attention dough exists, show a Reball task
- Reball task outcomes: reballed, discarded, partial
- If discarded, require a visible reason such as sun exposure, over-fermented, contamination, or manager decision

UX requirements:
- Extremely legible for older users
- Very large primary numbers
- One primary action per section
- Plain-language labels instead of technical labels
- Big buttons for Calculate, Save Plan, Create Task, Print
- Task cards first, dense tables second
- Strong visual distinction between OK, Attention, Do Today, Done, and Discarded
- Mobile-friendly and printable
- Operational kitchen feel, not generic SaaS
```

## Recommendation

Before generating the next prototype, align on these three decisions:

1. Whether attention dough still counts as available until proven bad
2. Whether reball should be a special task type instead of a normal dough task
3. Whether day-close reconciliation becomes mandatory for managers

If those three are defined first, the next Stitch prototype will be much closer to the real product.
