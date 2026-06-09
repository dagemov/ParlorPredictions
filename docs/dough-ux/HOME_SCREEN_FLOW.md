## Purpose

`Home` is the first decision screen after login.

It is not the place to edit everything. It is the place to understand the week quickly and choose the next action.

## Primary Users

- `Manager`
- `Admin`
- `PizzaMaker` can also use it as a simplified snapshot

## What Home Shows

### Top summary cards

1. `Weekly Goal`
2. `Ready Dough Balls Now`
3. `Still Missing This Week`
4. `Today's Load Plan`

### Weekly forecast

Show Tuesday through Sunday using simple day cards:

- date
- total balls needed
- event balls if any
- covered / in progress / missing status

### Events affecting the week

Show only the events that influence dough planning in the current window.

Each event should show:

- name
- date
- dough balls added
- short fermentation flag if enabled

### Current dough status summary

Show concise operational statuses:

- `Ready Now`
- `Still Fermenting`
- `Mixed But Not Balled`
- `Use First`
- `Attention`

## Main Actions

- `Open Dough Prep`
- `Add Event`
- `View Events`
- `Open Admin Panel` for manager/admin only

## What Home Should Avoid

- raw tables as the first element
- technical quality management grids
- full task-board filtering UI
- recommendation history tables
- user management widgets

## Home Outcome

After looking at Home, the user should know one of three things:

1. `Service is covered`
2. `We need to mix today`
3. `We need to ball or review older dough`
