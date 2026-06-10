# Stitch UX Flow

## Purpose

This document prepares a better Stitch prototype by describing the Dough Quality experience from the human point of view.

It intentionally avoids:

- backend entities
- service names
- repository names
- technical architecture

It focuses on:

- what the manager is trying to decide
- what the pizzamaker is trying to do
- what each screen must say clearly
- what exception cases should challenge the design

## What The Users Believe The App Does

From the kitchen point of view, the app is not a system for records.
It is a system that answers five urgent questions:

1. Do we have enough dough for today?
2. Is some dough old and risky?
3. What dough must be used first?
4. Do we need to recover dough by reballing it?
5. What was lost and why?

If the prototype answers those five questions well, the experience will feel useful.

## Roles As Real Users

## Manager

The manager opens the app to make decisions.
The manager wants to know what is safe, what is urgent, and what action should happen next.

The manager does not want to:

- read a big table first
- decode technical status names
- wonder which button matters most

## PizzaMaker

The pizzamaker opens the app to perform one task clearly.
The pizzamaker wants short instructions, large numbers, and a simple save action.

The pizzamaker does not want to:

- choose between many workflows
- guess whether old dough can be thrown away
- calculate losses manually

## Default Screen Order

The UX should feel like this:

1. Manager opens `Dough Prep Home`
2. Manager sees if there is urgent dough that must be used first
3. Manager reviews older dough if needed
4. Manager sends reball work when recovery makes sense
5. PizzaMaker completes the reball task
6. Manager discards dough only when necessary
7. Team prints a kitchen sheet for the day
8. Manager later reviews losses

## Manager Journey: Start Of Day

The manager opens the page before the kitchen gets busy.

The manager should immediately understand:

- how much is needed today
- how much good dough exists
- how much attention dough exists
- how much still needs to be made
- whether there is dough that must be used first today or tomorrow

The screen should feel like:

"This is what matters right now."

The manager's first decision is one of these:

- `We are covered. Print the sheet.`
- `We are covered, but old dough needs review.`
- `We are short. Create prep work.`
- `We have recoverable dough. Send reball work.`
- `Some dough is unsafe. Discard it.`

## PizzaMaker Journey: Task Time

The pizzamaker does not start from analytics or history.
The pizzamaker starts from a clear task.

The task screen should feel like:

"Here is the dough. Here is what you need to do. Save the result."

The pizzamaker's decisions are smaller:

- `I recovered part of it.`
- `I need the manager because this dough looks bad.`
- `I cannot save because the number is wrong.`

## Screen 1: Dough Prep Home

## User story

"I am the manager. I need to know if today is safe and what my next move is."

## Exact visible content

- Page title: `Dough for Today`
- Date label: `Monday, June 8`
- Big card: `Need Today`
- Big card helper text: `What service needs today`
- Big card: `Good Dough`
- Big card helper text: `Ready and safe to use`
- Big card: `Attention Dough`
- Big card helper text: `Still counts, but review it`
- Big card: `Need To Make`
- Big card helper text: `Still missing for today`
- Warning banner if needed: `Must Use Next Day`
- Warning banner detail: `Use this dough first`
- Human note title: `Manager Recommendation`
- Human note example: `Use yesterday's reballed dough first. Mix one more load after lunch prep.`
- Main button when old dough exists: `Review Older Dough`
- Main button when the kitchen is short: `Create Prep Task`
- Secondary button: `Create Reball Task`
- Secondary button: `Print Kitchen Sheet`

## Manager decisions on this screen

- `I trust today's numbers and move on`
- `I need to review old dough before deciding`
- `I need the team to make more dough`
- `I need a printed sheet for the kitchen`

## What the screen should never force the manager to do

- scan a large table to find the urgent issue
- compare many small badges
- read hidden tabs before acting

## Screen 2: Review Older Dough

## User story

"I am the manager. I need to inspect older dough and decide what stays usable, what needs attention, and what must be escalated."

## Exact visible content

- Page title: `Review Older Dough`
- Filter label: `Show dough from`
- Filter label: `To`
- Filter label: `Reballed on`
- Filter label: `Status`
- Section label: `Needs Review`
- Dough card title example: `Dough from Friday`
- Dough card detail: `56 balls`
- Dough card detail: `Created Jun 5`
- Dough card detail: `3 days old`
- Dough card status label: `Good Dough`
- Dough card explanation: `Still usable today`
- Dough card explanation alternative: `Needs attention before service`
- Button: `Mark Attention`
- Button: `Correct Status`
- Button: `Discard`
- Button: `Back to Dough for Today`

## Manager decisions on this screen

- `This dough is still fine`
- `This dough is usable but needs attention`
- `This dough was marked wrong earlier`
- `This dough should not stay available`

## What the screen should not show first

- audit history
- technical timestamps
- long reason taxonomies

## Screen 3: Reball Dough Task

## User story

"I am the pizzamaker. I just need to reball this dough and save what happened."

## Exact visible content

- Page title: `Reball Dough`
- Task subtitle: `Recover what you safely can`
- Label: `Original Dough`
- Value example: `56 balls`
- Label: `Recovered Dough`
- Input hint: `Enter how many balls were recovered`
- Auto result label: `Dough Lost During Reball`
- Auto result example: `14 balls`
- Priority message: `Recovered dough must be used first tomorrow`
- Main button: `Save Reball Result`
- Secondary button: `Need Manager Help`
- Quiet button: `Cancel`

## PizzaMaker decisions on this screen

- `I recovered part of the dough`
- `I need help because it smells bad or looks wrong`
- `I typed the wrong number and need to fix it`

## What the screen should prevent

- saving a recovered amount bigger than the original amount
- treating reball as full recovery
- making the pizzamaker choose a discard reason alone

## Screen 4: Discard Dough

## User story

"I am the manager. This dough should not stay available, and I need to say why."

## Exact visible content

- Page title: `Discard Dough`
- Warning text: `Discarded dough no longer counts as available`
- Dough label example: `Dough from Saturday`
- Quantity label: `56 balls`
- Scope note: `This action removes this whole dough group`
- Reason field title: `Why are you discarding this dough?`
- Reason option: `Too Hot`
- Reason option: `Over Fermented`
- Reason option: `Stored Too Many Days`
- Reason option: `Contamination`
- Reason option: `FIFO Not Followed`
- Reason option: `Not Sold Enough`
- Reason option: `Over Produced`
- Reason option: `Manager Decision`
- Reason option: `Other`
- Note field title: `Add a short note`
- Confirm button: `Confirm Discard`
- Secondary button: `Go Back`

## Manager decisions on this screen

- `The dough is unsafe`
- `The dough is old beyond use`
- `The dough was overproduced and will not sell`
- `The team needs a loss reason for reporting`

## What the screen should avoid

- soft language that hides the seriousness
- too many extra inputs
- mixing discard with reball on one form

## Screen 5: Kitchen Sheet / Print View

## User story

"I am printing something the whole kitchen can read fast."

## Exact visible content

- Sheet title: `Kitchen Dough Sheet`
- Date line: `For Monday, June 8`
- Big number: `Need Today`
- Big number: `Good Dough`
- Big number: `Attention Dough`
- Big number: `Need To Make`
- Warning box title: `Use First Today`
- Warning box example: `Reballed dough from yesterday`
- Section title: `Pay Attention Today`
- Section example: `6 cases from last week still count, but review them before service`
- Section title: `Mix / Prep Today`
- Section example: `Mix 1 more load after the lunch rush`
- Button: `Print`
- Button: `Back`

## Manager decisions on this screen

- `This is clear enough to hand to the kitchen`
- `I need to go back and fix the day summary first`

## What the sheet should never feel like

- a report for office staff
- a small-font data export
- a decorative marketing page

## Screen 6: Loss Analytics Preview

## User story

"I am the manager. I want to understand what went wrong this week without reading a giant report."

## Exact visible content

- Page title: `Dough Losses`
- Date filter label: `This Week`
- Big card: `Total Dough Lost`
- Section title: `Most Common Reasons`
- Section title: `Losses By Day`
- Insight card example: `Most losses came from Over Produced this week`
- Insight card example: `Saturday event return caused extra waste`
- Button: `Change Dates`
- Button: `Back to Dough`

## Manager decisions on this screen

- `We produced too much`
- `A storage problem caused losses`
- `Weekend events are creating risk`
- `Next week we should plan differently`

## What this screen should avoid

- pretending to be AI already
- too many charts at once
- kitchen-critical actions mixed into analytics

## Exception Cases That Should Challenge The Design

These cases should be imagined during prototyping so the screens stay honest and useful.

## Case 1: Enough Dough Overall, But Wrong Dough Priority

The manager sees enough total dough for today.
But part of it is `Must Use Next Day` and should be used first.

The risk:

- a generic summary can make the day look safe
- the kitchen may ignore the urgent dough

The prototype should make this obvious with a warning area that comes before the normal summary action.

## Case 2: Leftover Dough Crossing Into A New Week

It is Monday.
There is still dough from Friday and Saturday.
Some of it is still usable.
Some of it needs attention.

The risk:

- week changes can hide real kitchen carryover
- staff may think the old dough belongs to "last week" and ignore it

The prototype should show old dough in human terms like:

- `From Friday`
- `From Saturday`
- `Use first`

## Case 3: Reball Looks Successful, But Recovery Is Too High

The pizzamaker enters almost the same number as the original amount.

The risk:

- the system can look permissive
- the kitchen can believe reball is a clean reset

The prototype should show that reball is partial recovery and should reject unrealistic input clearly.

## Case 4: PizzaMaker Thinks Dough Is Bad

The pizzamaker opens a reball task, but the dough smells wrong or looks damaged by heat.

The risk:

- the pizzamaker may feel forced to continue
- the wrong person may end up making a discard decision

The prototype should include a clear path like:

- `Need Manager Help`

That path should feel normal, not like an error.

## Case 5: Manager Marked The Wrong Dough As Attention

The manager was moving fast and marked the wrong dough group.

The risk:

- the screen can feel unforgiving
- correcting the mistake can be hidden or scary

The prototype should make correction visible for authorized users without making it the main action for everyone.

## Case 6: No Old Dough Needs Review

The manager opens `Review Older Dough` and there is nothing risky.

The risk:

- an empty page can look broken

The prototype should show a calm empty state such as:

- `No older dough needs review today`
- `You can go back and print the kitchen sheet`

## Case 7: Human Recommendation And Dough Reality Conflict

The saved note says no extra mixing is needed.
But the summary also shows `Need To Make`.

The risk:

- staff lose trust in the page

The prototype should make it clear that visible numbers win over a stale note, or clearly mark the note as older than the current numbers.

## Case 8: Weekend Event Heat Damage

Dough returns from an event after sun or heat exposure.
Part of the kitchen thinks it can still be saved.
The manager decides it must be discarded.

The risk:

- people disagree on what happened
- the loss reason matters later

The discard screen should make the reason and note feel important and normal to record.

## Case 9: Older Users Feel Lost By Too Many Choices

If one page offers review, discard, reball, printing, analytics, and filters at once, older users may stop and ask for help.

The risk:

- the screen becomes correct but unusable

The prototype should always show one dominant action and keep secondary actions quieter.

## Case 10: The Day Is Fine, But Attention Dough Still Exists

Need Today is already covered.
Need To Make is zero.
But there is still attention dough that someone should look at.

The risk:

- the manager leaves thinking nothing requires action

The prototype should still surface:

- `Attention Dough`
- a short explanation
- a review action

## Case 11: Only Part Of The Dough Looks Bad

The manager wants to keep some of the dough and throw away only part of it.

The risk:

- the first release may only support discarding the whole dough group
- the prototype may accidentally promise partial discard

The prototype should stay honest.
If the first release is full-group discard only, the screen should say that clearly before confirmation.

## Case 12: Staff Confuse Attention Dough With Must Use Next Day

Both labels sound urgent, but they do not mean the same thing.

The risk:

- staff may think both states mean `throw it away soon`
- staff may ignore dough that should actually be used first

The prototype should separate the meanings clearly:

- `Attention Dough`: `Still counts, but review it`
- `Must Use Next Day`: `Use this first`

## Stitch Guidance From These Cases

When using this document in Stitch, the prototype should be judged by these questions:

1. Can the manager understand the next action in under one minute?
2. Can the pizzamaker complete reball without training on technical terms?
3. Can older staff understand urgent dough without reading a table?
4. Does the design still work when the business rules are stressed by edge cases?
5. Does the print view look like a kitchen tool instead of an office report?

## Final Direction

The best Dough Quality prototype will not feel like inventory software.
It will feel like a calm kitchen decision board with a few strong actions, large numbers, and clear warnings.
