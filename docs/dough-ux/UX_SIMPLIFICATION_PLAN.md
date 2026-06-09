## Goal

Simplify ParlorPrediction for kitchen staff, managers, and older users without changing the dough calculation rules already implemented in the application layer.

This phase keeps the existing backend services and changes how the product is organized and explained on screen.

## Navigation

Primary navigation becomes:

1. `Home`
2. `Dough Prep`
3. `Admin Panel`

### Home

Home becomes the first screen after login for the simplified experience.

It should answer:

- How much dough does this week need?
- How many dough balls are ready right now?
- How much is still missing this week?
- How many loads should be made today?
- Which days or events are driving the plan?
- What dough needs review or special attention?

### Dough Prep

Dough Prep becomes the kitchen operating screen.

It should answer:

- What is ready now?
- What is the weekly goal?
- What is still missing this week?
- What load work should happen today?
- Which tasks need action now?
- Which older dough batches need review, reball, or discard?

### Admin Panel

Admin Panel becomes the home for advanced or technical flows:

- Users
- Weekly Closing
- Losses
- Dough Quality advanced view
- Prep Dough Data
- Recommendations
- Other future admin/history screens

## Experience Rules

- Show fewer top-level choices.
- Keep card-first layouts.
- Avoid leading with tables.
- Use large numbers and plain language.
- Always separate `loads` from `balls`.
- Do not present `Make Dough Load` as available inventory.
- Present advanced quality/history tools behind `Admin Panel`.

## Data Sources To Reuse

- `DoughPrepCalculationService`
- `DoughProductionPlanningService`
- `PrepWeeklyDoughCalendarService`
- `PrepTaskService`
- `DoughQuality` services
- `Weekly Closing` / `Carryover` services
- existing event management flow in `PrepData`

## Key Non-Negotiable Rules

- Operational week is `Tuesday -> Sunday`.
- `1 full load = 14 cases = 168 balls`.
- `Ready Dough Balls` includes available good dough and attention dough.
- `Mixed But Not Balled` stays separate from available.
- `Make Dough Load` creates future capacity only.
- `Ball Dough` completion increases available balls.
- Weekly closing carryover must continue feeding the next week.
- Previous week used/finished remains historical reference only.

## This Phase Includes

- simplified role navigation
- operational Home screen
- simpler Dough Prep layout
- embedded daily dough tasks
- embedded older dough candidate section
- restored event-entry access from the main flow
- tests that protect `load vs balls` behavior

## This Phase Does Not Remove

- current services
- current controllers/routes
- weekly closing logic
- dough quality backend
- manual recommendation flow
- task board flow

Older screens may stay reachable by direct route or through `Admin Panel`, but they should no longer be the primary kitchen navigation.
