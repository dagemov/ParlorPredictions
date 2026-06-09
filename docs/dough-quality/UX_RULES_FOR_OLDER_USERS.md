# UX Rules For Older Users

## Purpose

These rules define how Dough Quality screens should be designed for older kitchen users.

The goal is not to make the app simplistic.
The goal is to make it understandable under time pressure.

## Primary Design Principle

The user should understand the next action without studying the screen.

## Content Rules

### Use plain language

Prefer:

- `Need Today`
- `Good Dough`
- `Review Older Dough`
- `Must Use Next Day`

Avoid leading with technical wording such as:

- `Batch Quality Status`
- `Operational Age Window`
- `PartialRecovered`

### Keep text short

- short labels
- short helper text
- one sentence at a time

### Explain risk directly

Prefer:

- `Use this dough first tomorrow`
- `This dough still counts, but needs review`
- `Discarded dough no longer counts as available`

Avoid vague status-only wording.

## Layout Rules

### Cards before tables

The first view should be made of large cards, banners, and actions.
Tables can exist later for detail, but should not be the first thing the user sees.

### One dominant action

Each screen should emphasize one main action.
Secondary actions should stay visible but quieter.

### Strong spacing

Use large gaps between sections so users can separate:

- summary
- warning
- recommendation
- action

### Keep the top of the page calm

The first screen area should not contain:

- too many badges
- multiple filters
- long paragraphs
- charts

## Typography Rules

### Favor large readable type

- primary metrics should be visually large
- section titles should be easy to scan
- helper text should still be readable without strain

### Use consistent wording

Do not switch between different labels for the same concept.

Examples:

- always use `Attention Dough`
- always use `Must Use Next Day`
- always use `Need To Make`

## Color And Status Rules

### Color must support meaning, not carry it alone

Every colored state must also include:

- text label
- short explanation when needed

### Recommended semantic states

- `Good Dough`: safe and stable
- `Attention Dough`: warning
- `Must Use Next Day`: urgent priority
- `Discarded Dough`: stopped or removed

### Avoid visual noise

Do not use too many accent colors at once.
One screen should make the most urgent state obvious.

## Interaction Rules

### Avoid hidden next steps

Buttons should say what happens:

- `Mark Attention`
- `Save Reball Result`
- `Confirm Discard`
- `Print Kitchen Sheet`

Avoid unclear labels such as `Continue` or `Submit` when a more specific label is possible.

### Confirm destructive actions clearly

Discard should always feel deliberate.
The user should know:

- what batch is affected
- what quantity is affected
- why a reason is required

### Reduce memory load

The screen should show the key values needed for the decision.
Do not require the user to remember numbers from the previous page.

## Form Rules

### Keep forms short

Show only the fields required for the current decision.

### Use safe defaults carefully

- default the date to today where reasonable
- do not default a discard reason
- do not hide required fields inside advanced sections

### Show validation beside the action

Validation messages should be short and practical, for example:

- `Pick a discard reason`
- `Recovered dough must be less than the original amount`
- `Reference date is required`

## Date And Time Rules

### Prefer operational wording

Where helpful, pair dates with human meaning:

- `Today`
- `Tomorrow`
- `Created on Jun 4`
- `Must use by Jun 9`

### Avoid date ambiguity

If a screen uses a relative term such as `Next Day`, also show the actual date.

## Print Rules

### Print for kitchen reality

The print view should assume:

- staff may read it while moving
- lighting may be imperfect
- the page may be taped to a wall or laid on a counter

### Print priorities

- large metrics
- clear warning section
- short recommendation
- very few decorative elements

## Anti-Patterns To Avoid

- tiny dashboards
- dense data grids as the default view
- multiple competing primary buttons
- status meaning explained only by color
- long modal workflows
- jargon-heavy labels
- hidden destructive actions
- charts on urgent task screens

## Success Criteria

The design is working if an older kitchen user can:

1. understand what dough is safe
2. identify what dough needs attention
3. know what must be used first
4. complete reball without guessing
5. understand discard without training on technical terms
