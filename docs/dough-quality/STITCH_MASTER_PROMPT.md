# Stitch Master Prompt

Use this document as the final prompt to paste into Google Stitch.

It is intentionally focused on human flow, visible content, role behavior, and UX priorities.
It avoids backend implementation details.

## Paste Into Stitch

```text
Design a high-quality responsive web prototype for a product called ParlorPrediction, used by Parlor Pizza to plan dough, track dough work, review older dough, recover reballable dough, discard lost dough, and print a clear kitchen sheet.

This is a visual UX prototype only.
Do not implement real code.
Do not invent backend architecture.
Do not show developer tools, database tables, technical status codes, or engineering concepts.
Do not use backend names.

The product should feel like a kitchen decision board, not an ERP, not office inventory software, and not a generic SaaS dashboard.

Core product context:
ParlorPrediction already helps the restaurant calculate:
- dough needed
- dough available
- dough missing
- dough preparation tasks
- human recommendations
- events that affect planning

This new experience is called Dough Quality.
It must help the team answer 5 urgent questions:
1. Do we have enough dough for today?
2. Is some dough old and risky?
3. What dough must be used first?
4. Do we need to recover dough by reballing it?
5. What was lost and why?

Primary users:
1. Manager / Admin
   - makes decisions
   - reviews old dough
   - discards dough
   - creates reball work
   - needs to understand the most urgent action in less than 1 minute

2. PizzaMaker
   - performs clear tasks
   - does not decide discard
   - records how much dough was recovered during reball
   - needs large buttons, large numbers, and zero ambiguity

3. Owner
   - reviews losses
   - wants to know if too much dough is being produced
   - wants to understand the main loss reasons
   - will later use this information to improve decisions and future AI, but do not design AI now

Business rules that the prototype must respect visually:
- Good Dough counts as available
- Attention Dough counts as available
- Must Use Next Day counts as available, but should be used first
- Discarded Dough does not count as available
- Attention Dough means: still counts, but review it
- Must Use Next Day means: use this first
- Reball never recovers 100%
- Reball is partial recovery
- Reball leaves the dough in a Must Use Next Day state
- PizzaMaker can report partial recovery
- PizzaMaker can ask for manager help
- Only Manager / Admin can discard dough
- Discard requires a reason
- The first release may support only full-group discard, so do not design a misleading partial-discard experience
- The system must show carryover across days and weeks; older dough does not disappear because the calendar moved to a new week

Official operational priority:
1. Must Use Next Day
2. Attention Dough
3. Need To Make
4. Print Kitchen Sheet
5. Loss Analytics

Central UX rule:
The manager should never need more than one click to discover today's most urgent dough.

Desired emotional tone:
- calm
- operational
- clear
- trustworthy
- kitchen-friendly
- easy for older users

Visual style:
- card-first design
- large numbers
- large buttons
- short labels
- high contrast
- warm but professional
- clear hierarchy
- works on desktop and tablet
- easy to read from a few feet away

Important visual restrictions:
- do not make it look like a finance dashboard
- do not make it look like warehouse software
- do not make tables the first thing users see
- use semantic colors, but never depend on color alone
- always pair color with explicit text meaning
- avoid technical words
- avoid too many actions on one screen

Tablet and touch requirements:
- large tap targets
- no tiny dropdowns as main controls
- important warnings above the fold
- PizzaMaker task screen should be usable with one hand

Print requirements:
- kitchen print view must work in black and white
- very large text
- low clutter
- should feel like a real kitchen tool, not an office report

Design the following 7 linked screens:

SCREEN 1: Dough Prep Home
Purpose:
Manager sees whether today is safe and what to do next.

Page title:
Dough for Today

Show four large cards:
- Need Today
  helper: What service needs today
- Good Dough
  helper: Ready and safe to use
- Attention Dough
  helper: Still counts, but review it
- Need To Make
  helper: Still missing for today

If Must Use Next Day exists, show a warning before secondary content:
- title: Use First
- text: Reballed dough must be used first today or tomorrow

Show Manager Recommendation:
- title: Manager Recommendation
- example text: Use yesterday's reballed dough first. Mix one more load after lunch prep.

Primary action logic:
- if Must Use Next Day exists, primary action = View Use First Dough
- if Attention Dough exists, primary action = Review Older Dough
- if Need To Make is greater than zero, primary action = Create Prep Task
- if the day is covered, primary action = Print Kitchen Sheet

Secondary actions:
- Create Reball Task
- Review Older Dough
- Print Kitchen Sheet

Avoid on this screen:
- big tables at the top
- too many badges
- hidden urgent warnings
- analytics on the home screen

SCREEN 2: Review Older Dough
Purpose:
Manager reviews old dough and decides if it is still good, attention, or should be escalated.

Page title:
Review Older Dough

Filters:
- Show dough from
- To
- Reballed on
- Status

Use cards, not a table-first layout.

Card example:
- Dough from Friday
- 56 balls
- Created Jun 5
- 3 days old
- Status: Good Dough
- Explanation: Still usable today

Actions on each card:
- Mark Attention
- Correct Status
- Reball
- Discard
- Back to Dough for Today

Empty state:
- No older dough needs review today.
- You can go back and print the kitchen sheet.

Avoid on this screen:
- audit history first
- technical timestamps
- long reason lists before action

SCREEN 3: Active Tasks Board
Purpose:
Manager and PizzaMaker see today's dough work clearly.

Page title:
Today's Dough Tasks

Sections:
- Must Do Now
- In Progress
- Completed

Task card types:
- Make Dough
- Reball Dough
- Review Old Dough

Each task card should show:
- task type
- quantity
- who should do it
- due time or priority
- status
- one big action button

Task card example:
- Reball Dough
- 56 balls from Friday
- Assigned to PizzaMaker
- Priority: Use first tomorrow
- Button: Open Task

Avoid on this screen:
- mixing analytics into the task board
- making PizzaMaker choose manager-only actions

SCREEN 4: Reball Dough Task
Purpose:
PizzaMaker records how much dough was recovered.

Page title:
Reball Dough

Subtitle:
Recover what you safely can.

Visible content:
- Original Dough: 56 balls
- Recovered Dough: numeric input
- Dough Lost During Reball: auto-calculated
- Priority message: Recovered dough must be used first tomorrow.

Buttons:
- Save Reball Result
- Need Manager Help
- Cancel

Validation behavior:
- recovered amount cannot be greater than original amount
- recovered amount should not imply 100% recovery
- if dough looks bad, PizzaMaker should use Need Manager Help

Avoid on this screen:
- discard reason
- technical status names
- too many fields

SCREEN 5: Discard Dough
Purpose:
Manager removes unusable dough from available inventory and records why.

Page title:
Discard Dough

Warning:
Discarded dough no longer counts as available.

Visible content:
- Dough from Saturday
- Quantity: 56 balls
- Scope note: This action removes this whole dough group.

Reason field title:
Why are you discarding this dough?

Reason options:
- Too Hot
- Over Fermented
- Stored Too Many Days
- Contamination
- FIFO Not Followed
- Not Sold Enough
- Over Produced
- Manager Decision
- Other

Optional note:
- Add a short note

Buttons:
- Confirm Discard
- Go Back

Avoid on this screen:
- soft language
- combining discard and reball in one form
- letting PizzaMaker discard

SCREEN 6: Kitchen Sheet / Print View
Purpose:
Printable sheet for kitchen staff.

Page title:
Kitchen Dough Sheet

Date example:
For Monday, June 8

Large sections:
- Need Today
- Good Dough
- Attention Dough
- Need To Make

Priority section:
- USE FIRST
- example: 25 reballed balls from Sunday

Next section:
- PAY ATTENTION
- example: 40 attention balls from Saturday. Still counts, but review before service.

Mix / Prep Today section:
- example: Mix 1 more load after lunch rush.

Task list examples:
- Make 30 balls
- Reball 25 balls
- Use reballed dough first

Buttons:
- Print
- Back

Requirements for this screen:
- black and white printable
- very large font
- no decorative clutter
- must look like a kitchen tool

SCREEN 7: Loss Analytics Preview
Purpose:
Owner or Manager sees why dough was lost.

Page title:
Dough Losses

Filters:
- This Week
- Change Dates

Large cards:
- Total Dough Lost
- Main Reason
- Loss Trend

Sections:
- Most Common Reasons
- Losses By Day
- Notes From Manager

Insight examples:
- Most losses came from Over Produced this week.
- Saturday event return caused extra waste.
- FIFO problems caused 18 balls lost.

Avoid on this screen:
- pretending AI already exists
- too many charts
- mixing urgent kitchen actions into analytics

Role-based behavior the prototype should make obvious:

Manager / Admin:
- sees urgent dough first
- reviews older dough
- can create reball work
- can discard dough
- needs strong action hierarchy and low cognitive load

PizzaMaker:
- sees a task, not a complex dashboard
- should never be asked to decide discard
- should get one clean reball workflow
- should have a clear path to ask the manager for help

Owner:
- wants a clean summary of losses and patterns
- does not need day-to-day kitchen forms

Important edge cases the prototype must handle honestly and simply:

1. Enough dough overall, but Must Use Next Day must be used first.
   Design implication: show urgent dough above everything else.

2. Leftover dough crosses into a new week.
   Design implication: show labels like From Friday or From Saturday so old dough remains visible and real.

3. Reball recovery is unrealistically high.
   Design implication: show clear validation and reinforce that reball is partial recovery.

4. PizzaMaker thinks dough smells bad or looks unsafe.
   Design implication: Need Manager Help should feel normal and prominent enough to use.

5. Manager marked the wrong dough as Attention.
   Design implication: correction should exist, but should not dominate the screen.

6. No old dough needs review.
   Design implication: show a calm empty state, not a blank page.

7. Manager recommendation conflicts with current numbers.
   Design implication: the visual truth of the current numbers should stay clear, and stale recommendations should look secondary.

8. Weekend event heat damage causes discard.
   Design implication: discard reasons and note-taking should feel practical and natural.

9. Older users feel lost because there are too many choices.
   Design implication: each screen should have one dominant action and quieter secondary actions.

10. Need To Make is zero, but Attention Dough still needs review.
    Design implication: surface review work even when the production need is already covered.

11. Only part of the dough looks bad, but first release supports full-group discard only.
    Design implication: the prototype must not promise precise partial discard if that is not supported yet.

12. Staff confuse Attention Dough with Must Use Next Day.
    Design implication: keep the meanings visually separate:
    - Attention Dough = Still counts, but review it
    - Must Use Next Day = Use this first

Important copy rules:
- use short labels
- use plain operational language
- avoid technical jargon
- avoid code-like names
- avoid long paragraphs

The final prototype should feel like:
- a calm kitchen decision board
- easy to scan quickly
- clear enough for older users
- practical for real restaurant operations

Deliver a polished web prototype with all 7 screens visually connected, consistent, and realistic for desktop and tablet use.
```

## Success Check

The prompt is working if Stitch produces a prototype where:

- the manager can spot the most urgent dough immediately
- the pizzamaker sees a simple task flow instead of a system dashboard
- older users would not need to decode tables or technical labels
- print view looks operational and usable in a kitchen
- analytics stays secondary to daily kitchen decisions
