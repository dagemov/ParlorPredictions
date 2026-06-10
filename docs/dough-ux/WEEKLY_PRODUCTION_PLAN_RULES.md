## Core Planning Rules

- Operational week: `Tuesday -> Sunday`
- `1 full load = 14 cases = 168 balls`
- `Make Dough Load` adds future capacity only
- `Ball Dough` converts a finished load into available balls
- `Mixed But Not Balled` is visible separately and does not count as available

## Weekly Metrics

### Ready Now

Counts dough balls that can be used immediately.

Includes:

- good dough
- attention dough that still counts as available
- carryover ready balls
- carryover attention balls

Does not include:

- mixed loads
- fermenting dough
- discarded dough

### Still Fermenting

Shows dough already mixed and aging for this week's window.

It helps reduce the future shortage but is not usable now.

### Mixed But Not Balled

Shows dough load capacity that exists but still needs `Ball Dough`.

It is separate from `Ready Now`.

### Still Missing This Week

Formula:

`Week Needed - Ready Now - Still Fermenting - Mixed But Not Balled`

### Previous Week Used / Finished

Historical only.

It must stay separate and must not reduce the current week's shortage by itself.

## Carryover Rules

- leftover ready balls carry into the next week as available
- leftover attention balls carry into the next week as available with attention
- leftover mixed loads carry into the next week as mixed but not balled
- weekly closing may exist only once per week

## Kitchen Task Rules

### Tuesday example

If Tuesday starts with:

- `1` ready load already balled
- `2` mixed loads not yet balled
- `1` new load mixed today

then:

- `Ready Now = 168 balls`
- `Mixed But Not Balled = 2 existing loads`
- completing `Make Dough Load` today does not increase `Ready Now`
- the new load becomes next-day `Ball Dough` work

### Wednesday example

When Wednesday completes `Ball Dough` for the load made Tuesday:

- `Ready Now` increases by `168`
- `Mixed But Not Balled` decreases by `1 load`

This separation is mandatory across Home, Dough Prep, weekly planning, and tasks.
