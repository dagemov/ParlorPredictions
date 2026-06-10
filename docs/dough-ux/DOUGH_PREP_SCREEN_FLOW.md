## Purpose

`Dough Prep` is the kitchen execution screen.

It should support fast action for kitchen staff without hiding the real production rules.

## Main Cards

Show these four cards first:

1. `Ready Dough Balls`
   - available right now
   - includes attention dough if it still counts as available
   - does not include mixed loads

2. `Weekly Goal`
   - total balls needed for the operational week

3. `Still Missing This Week`
   - `Week Needed - Ready Now - Still Fermenting - Mixed But Not Balled`

4. `Today's Load Plan`
   - how many full loads to make today
   - explain that this is future capacity, not available inventory

## Embedded Sections

### Today's Tasks

Show open tasks first:

- `Make Dough Load`
- `Ball Dough`
- other dough tasks if present

Completed tasks should stay below or collapsed behind a secondary section.

### Older Dough / Reball Candidates

Show a smaller section under tasks:

- age
- quantity
- reason it needs review
- current status

Actions should be simple and role-aware:

- `Keep / Still Good`
- `Reball`
- `Discard`
- `Advanced Review` link for manager/admin

## Messaging Rules

- `Ready Dough Balls` means usable now.
- `Mixed But Not Balled` means potential only.
- `Use First` means must use next day / reballed priority.
- `Still Missing` should never be confused with yesterday's waste or last week's usage.

## What Dough Prep Should Avoid

- top-level analytics
- admin maintenance filters
- large historical tables by default
- duplicated navigation to many technical pages

## Desired User Outcome

The kitchen should be able to answer quickly:

- Do we need to mix?
- Do we need to ball?
- Which dough should we use first?
- Which older dough needs review today?
