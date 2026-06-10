# Stitch MVC Integration Plan

## Purpose

This document defines how the Stitch-generated Dough Quality UX should be integrated into the real ParlorPrediction MVC application.

This phase is documentation and preparation only.

It does not:

- import Tailwind CDN
- modify login flows
- modify user management flows
- replace the global layout
- change backend business logic
- change app settings

## Integration Goals

The integration must:

- preserve the current MVC architecture
- keep login and users stable
- reuse existing controllers and view models where possible
- isolate Dough Quality styles and scripts
- connect new screens to the existing Dough Quality backend gradually
- avoid fake production data

## Stitch Inventory

Stitch package root:

- `C:\Users\Hombr\Downloads\stitch_ai_dough_prep_optimizer`

Screens found:

1. `dough_for_today_parlorprediction`
   - `code.html`
   - `screen.png`
2. `review_older_dough_parlorprediction`
   - `code.html`
   - `screen.png`
3. `today_s_dough_tasks_parlorprediction`
   - `code.html`
   - `screen.png`
4. `reball_dough_parlorprediction`
   - `code.html`
   - `screen.png`
5. `discard_dough_parlorprediction`
   - `code.html`
   - `screen.png`
6. `kitchen_dough_sheet_parlorprediction`
   - `code.html`
   - `screen.png`
7. `dough_losses_parlorprediction`
   - `code.html`
   - `screen.png`
8. `kitchen_operational_hub`
   - `DESIGN.md`

## What Stitch Actually Generated

Each Stitch screen is a standalone HTML document with:

- Tailwind CDN
- Google Fonts
- Material Symbols
- inline style blocks
- inline scripts in some screens
- its own mock top bar and side navigation
- mock dates, mock profile labels, and mock copy

Some screens also include:

- remote mock images
- AI suggestion copy that does not reflect the real app state

There are no reusable local CSS or JS files in the Stitch export.

## MVC Equivalents

## Existing MVC screens that match Stitch concepts

### 1. Dough Prep Home

Stitch source:

- `dough_for_today_parlorprediction/code.html`

Real MVC target:

- [Views/Prep/Dough.cshtml](/C:/Users/Hombr/source/repos/ParlorPredictions/src/ParlorPrediction.Mvc/Views/Prep/Dough.cshtml)

Related partials:

- [Views/Prep/_DoughPrepWorkspacePartial.cshtml](/C:/Users/Hombr/source/repos/ParlorPredictions/src/ParlorPrediction.Mvc/Views/Prep/_DoughPrepWorkspacePartial.cshtml)
- [Views/Prep/_DoughRecommendationPartial.cshtml](/C:/Users/Hombr/source/repos/ParlorPredictions/src/ParlorPrediction.Mvc/Views/Prep/_DoughRecommendationPartial.cshtml)
- [Views/Prep/_DoughTasksPartial.cshtml](/C:/Users/Hombr/source/repos/ParlorPredictions/src/ParlorPrediction.Mvc/Views/Prep/_DoughTasksPartial.cshtml)

Must preserve:

- real data from `DoughPrepPageViewModel`
- current HTMX interactions
- current `PrepController.Dough` flow

### 2. Active Tasks Board

Stitch source:

- `today_s_dough_tasks_parlorprediction/code.html`

Real MVC target:

- [Views/PrepTasks/Index.cshtml](/C:/Users/Hombr/source/repos/ParlorPredictions/src/ParlorPrediction.Mvc/Views/PrepTasks/Index.cshtml)

Must preserve:

- current task filters
- current completion workflow
- current task permissions

### 3. Manager Recommendation

Stitch visual source:

- visual cues from `dough_for_today_parlorprediction/code.html`

Real MVC target:

- [Views/PrepRecommendations/Create.cshtml](/C:/Users/Hombr/source/repos/ParlorPredictions/src/ParlorPrediction.Mvc/Views/PrepRecommendations/Create.cshtml)

Goal:

- make manager recommendation more legible and visible
- do not present it as active AI

### 4. Weekly / Calendar context

Stitch visual source:

- visual direction from the Dough Prep dashboard hierarchy

Real MVC target:

- [Views/Prep/DoughWeek.cshtml](/C:/Users/Hombr/source/repos/ParlorPredictions/src/ParlorPrediction.Mvc/Views/Prep/DoughWeek.cshtml)

Goal:

- preserve current weekly calculations
- do not fake carryover behavior the current view does not yet compose

### 5. Dashboard Summary

Stitch visual source:

- `dough_losses_parlorprediction/code.html` for analytics card hierarchy

Real MVC target:

- [Views/Dashboard/Index.cshtml](/C:/Users/Hombr/source/repos/ParlorPredictions/src/ParlorPrediction.Mvc/Views/Dashboard/Index.cshtml)

Goal:

- keep current dashboard intact
- defer Dough Quality analytics styling to the dedicated Losses screen first

## New screens with no current MVC equivalent

These should be added as new MVC views:

1. Review Older Dough
2. Reball Dough Task
3. Discard Dough
4. Kitchen Sheet / Print View
5. Loss Analytics Preview

## Planned MVC files to touch

## Existing files likely to be modified

- [Views/Shared/_Layout.cshtml](/C:/Users/Hombr/source/repos/ParlorPredictions/src/ParlorPrediction.Mvc/Views/Shared/_Layout.cshtml)
  - only for optional `@RenderSection("Styles", required: false)`
  - keep `Scripts` section support intact
- [Views/Prep/Dough.cshtml](/C:/Users/Hombr/source/repos/ParlorPredictions/src/ParlorPrediction.Mvc/Views/Prep/Dough.cshtml)
- [Views/Prep/_DoughPrepWorkspacePartial.cshtml](/C:/Users/Hombr/source/repos/ParlorPredictions/src/ParlorPrediction.Mvc/Views/Prep/_DoughPrepWorkspacePartial.cshtml)
- [Views/Prep/_DoughRecommendationPartial.cshtml](/C:/Users/Hombr/source/repos/ParlorPredictions/src/ParlorPrediction.Mvc/Views/Prep/_DoughRecommendationPartial.cshtml)
- [Views/Prep/_DoughTasksPartial.cshtml](/C:/Users/Hombr/source/repos/ParlorPredictions/src/ParlorPrediction.Mvc/Views/Prep/_DoughTasksPartial.cshtml)
- [Views/PrepTasks/Index.cshtml](/C:/Users/Hombr/source/repos/ParlorPredictions/src/ParlorPrediction.Mvc/Views/PrepTasks/Index.cshtml)
- [Views/PrepRecommendations/Create.cshtml](/C:/Users/Hombr/source/repos/ParlorPredictions/src/ParlorPrediction.Mvc/Views/PrepRecommendations/Create.cshtml)
- [Views/Prep/DoughWeek.cshtml](/C:/Users/Hombr/source/repos/ParlorPredictions/src/ParlorPrediction.Mvc/Views/Prep/DoughWeek.cshtml)

## New files likely to be added

- `src/ParlorPrediction.Mvc/wwwroot/css/dough-quality.css`
- `src/ParlorPrediction.Mvc/wwwroot/js/dough-quality.js`
- `src/ParlorPrediction.Mvc/Controllers/DoughQualityController.cs`
- `src/ParlorPrediction.Mvc/Models/DoughQuality/`
- `src/ParlorPrediction.Mvc/Views/DoughQuality/Review.cshtml`
- `src/ParlorPrediction.Mvc/Views/DoughQuality/Reball.cshtml`
- `src/ParlorPrediction.Mvc/Views/DoughQuality/Discard.cshtml`
- `src/ParlorPrediction.Mvc/Views/DoughQuality/KitchenSheet.cshtml`
- `src/ParlorPrediction.Mvc/Views/DoughQuality/Losses.cshtml`

## Existing view models to reuse when possible

- [Models/Prep/DoughPrepPageViewModel.cs](/C:/Users/Hombr/source/repos/ParlorPredictions/src/ParlorPrediction.Mvc/Models/Prep/DoughPrepPageViewModel.cs)
- [Models/Prep/DoughRecommendationViewModel.cs](/C:/Users/Hombr/source/repos/ParlorPredictions/src/ParlorPrediction.Mvc/Models/Prep/DoughRecommendationViewModel.cs)
- [Models/Prep/DoughTaskViewModel.cs](/C:/Users/Hombr/source/repos/ParlorPredictions/src/ParlorPrediction.Mvc/Models/Prep/DoughTaskViewModel.cs)
- [Models/Prep/PrepTaskListPageViewModel.cs](/C:/Users/Hombr/source/repos/ParlorPredictions/src/ParlorPrediction.Mvc/Models/Prep/PrepTaskListPageViewModel.cs)
- [Models/Prep/WeeklyDoughCalendarViewModel.cs](/C:/Users/Hombr/source/repos/ParlorPredictions/src/ParlorPrediction.Mvc/Models/Prep/WeeklyDoughCalendarViewModel.cs)
- [Models/Dashboard/PrepDashboardViewModel.cs](/C:/Users/Hombr/source/repos/ParlorPredictions/src/ParlorPrediction.Mvc/Models/Dashboard/PrepDashboardViewModel.cs)

## Backend services to use

- `IDoughQualityReadService`
- `IDoughQualityManagementService`

Controllers should orchestrate only:

- route handling
- permission checks
- request mapping
- response to view model mapping

They should not duplicate Dough Quality business rules.

## What will be reused from Stitch

The reusable value is visual and structural, not technical.

Reuse:

- card-first metric layout
- warning banners
- stronger primary action hierarchy
- bento-style task grouping
- review cards
- large-touch reball form layout
- print screen composition
- analytics card hierarchy
- typography scale and spacing ideas from `DESIGN.md`

## What will be discarded from Stitch

Do not import:

- Tailwind CDN
- Google Font includes as direct view imports
- Material Symbols imports as direct view imports
- standalone HTML wrappers
- mock sidebars
- mock top navigation
- mock user avatars
- remote mock images
- mock dates
- AI suggestion cards that imply AI is active now
- inline scripts that bypass MVC behavior
- inline styles copied per view

## CSS and JS integration strategy

## CSS

Create:

- `wwwroot/css/dough-quality.css`

Rules:

- prefix custom selectors with `.dq-`
- avoid overriding Bootstrap globally
- do not change `site.css` unless a very small neutral fix is required
- load Dough Quality styles only from relevant views

Examples of what belongs there:

- Dough Quality metric cards
- warning banner
- action tiles
- review cards
- task board visual improvements
- print-specific rules for kitchen sheet

## JS

Create only if needed:

- `wwwroot/js/dough-quality.js`

Allowed uses:

- lightweight client-side UI helpers
- print helpers
- large-input UX helpers for reball

Not allowed:

- business rule duplication
- permission checks
- fake data injection

## Layout strategy

The global layout must remain in place.

Allowed minimal change:

- add optional `@RenderSection("Styles", required: false)` in the `<head>` if needed

This change must remain neutral:

- Login does not load Dough Quality CSS
- Users/Admin does not load Dough Quality CSS
- only Dough / Dough Quality views opt into the new stylesheet

No other global layout replacement is allowed.

## Login and Users risk analysis

## Current state

Login and users currently rely on:

- the same shared layout
- `site.css`
- existing nav and auth flow

Sensitive files:

- [Views/Session/Login.cshtml](/C:/Users/Hombr/source/repos/ParlorPredictions/src/ParlorPrediction.Mvc/Views/Session/Login.cshtml)
- [Views/Shared/_Layout.cshtml](/C:/Users/Hombr/source/repos/ParlorPredictions/src/ParlorPrediction.Mvc/Views/Shared/_Layout.cshtml)
- `AdminUsers` views and controller

## Main risks

1. global CSS leaks into login or users
2. replacing layout structure breaks header/nav/auth shell
3. manager-only visual actions become visible to PizzaMaker
4. Tailwind-style utility collisions inflate or override the app unexpectedly

## Risk mitigation

- isolate all new CSS under Dough Quality-specific class prefixes
- do not replace `_Layout.cshtml`
- do not touch `SessionController`
- do not touch `AdminUsersController`
- validate role visibility server-side in controllers and views
- use staging selectivo for each commit

## Proposed routes

For new Dough Quality screens:

- `GET  /prep/dough-quality/review`
- `POST /prep/dough-quality/mark-attention`
- `GET  /prep/dough-quality/reball/{id}`
- `POST /prep/dough-quality/reball/{id}`
- `GET  /prep/dough-quality/discard/{id}`
- `POST /prep/dough-quality/discard/{id}`
- `GET  /prep/dough-quality/kitchen-sheet`
- `GET  /prep/dough-quality/losses`

## Role strategy

### Manager / Admin

Can access:

- Review Older Dough
- Mark Attention
- Correct Status
- Reball management flow
- Discard
- Kitchen Sheet
- Loss Analytics

### PizzaMaker

Can access:

- permitted reball completion flow if allowed by backend

Cannot access:

- discard actions
- manager-only decision actions
- analytics actions meant for manager/owner

## Plan by phases

## Phase 0 - Documentation and preparation

- create branch from `codex/dough-quality-backend-and-ux-docs`
- add this integration plan document
- do not touch views yet
- run build

## Phase 1 - Isolated assets and Dough Prep Home

- create `dough-quality.css`
- add isolated visual tokens inspired by Stitch
- integrate Dough Prep Home styling into:
  - `Views/Prep/Dough.cshtml`
  - `_DoughPrepWorkspacePartial.cshtml`
  - `_DoughRecommendationPartial.cshtml`
  - `_DoughTasksPartial.cshtml`
- preserve:
  - real data
  - HTMX behavior
  - current controller flow
- do not fake `Good Dough` or `Attention Dough`
- if those values are not yet available, omit them or show only real-connected states

## Phase 2 - Review Older Dough

- add `DoughQualityController`
- add Dough Quality view models
- create `Review.cshtml`
- connect to `IDoughQualityReadService`
- support real filters where backend supports them
- if a needed read shape is not available yet, document TODO instead of faking data

## Phase 3 - Reball and Discard

- create `Reball.cshtml`
- create `Discard.cshtml`
- connect POST actions to `IDoughQualityManagementService`
- enforce role checks server-side
- use large numeric input for reball
- make discard reason required
- clearly state when discard is full-group only

## Phase 4 - Kitchen Sheet / Print View

- create `KitchenSheet.cshtml`
- add isolated print CSS
- compose real available data only
- ensure black-and-white readability

## Phase 5 - Loss Analytics Preview

- create `Losses.cshtml`
- connect to `IDoughQualityReadService`
- show real loss totals and reason breakdowns
- use future-facing language such as:
  - `These loss patterns can support future recommendations.`
- do not imply AI is already live

## Phase 6 - Cleanup and final verification

- confirm no Tailwind CDN imports
- confirm no remote Stitch images
- confirm no mock profile UI remains
- confirm login and users were not visually affected
- final build/test

## Commit strategy

Recommended small commits:

1. `docs: document stitch mvc integration plan`
2. `feat: integrate stitch dough prep home layout`
3. `feat: add dough quality review screen`
4. `feat: add dough reball and discard screens`
5. `feat: add dough kitchen sheet print view`
6. `feat: add dough loss analytics preview`
7. `test: verify stitch dough quality ux integration`

## Validation strategy

## Build and test after each phase

- `dotnet build ParlorPrediction.sln /nodeReuse:false`
- `dotnet test ParlorPrediction.sln /nodeReuse:false`

## Manual smoke checks after relevant phases

1. Login loads normally.
2. Login styling is unchanged.
3. Users/Admin pages load normally.
4. Roles still work.
5. Dough Prep loads.
6. Review Older Dough loads when added.
7. Reball validates quantity correctly.
8. Discard requires a reason.
9. PizzaMaker cannot discard dough.
10. Kitchen Sheet prints legibly.
11. Loss Analytics does not imply AI is active now.
12. No HTTP 500 errors appear in core flows.

## Current known constraints

These constraints should shape the implementation:

- current Dough Quality summary is not yet composed into Dough Prep Home
- current discard backend is full-record discard, not partial discard
- current MVC does not yet expose Dough Quality screens
- current task board is table-first, so the visual refactor should be incremental rather than a full rewrite

## Success criteria

The integration is successful when:

- Dough Prep feels visually closer to Stitch without losing real behavior
- Dough Quality screens use the real backend where available
- login and users are unaffected
- no mock data is presented as production truth
- commits stay small and reviewable
