# Global Tailwind / Stitch Visual Audit

## Goal

Unify the visual language of `ParlorPrediction` so the product no longer feels like two separate applications:

- Dough Prep / Dough Quality already uses the newer Tailwind/Stitch direction.
- Home, Dashboard, Session, and Admin Users still render through the older shared shell.

## Real Views Audited

- `Views/Home/Index.cshtml`
- `Views/Dashboard/Index.cshtml`
- `Views/Session/Login.cshtml`
- `Views/Session/Register.cshtml`
- `Views/AdminUsers/Index.cshtml`
- `Views/AdminUsers/Create.cshtml`
- `Views/AdminUsers/Edit.cshtml`
- `Views/AdminUsers/Details.cshtml`
- `Views/AdminUsers/Roles.cshtml`
- `Views/Shared/_Layout.cshtml`
- `Views/Shared/_DoughLayout.cshtml`
- `Views/Shared/_ValidationScriptsPartial.cshtml`

## CSS Audited

- `wwwroot/css/site.css`
- `wwwroot/css/dough-quality.css`

## Where Tailwind Already Loads

Tailwind is currently loaded only inside:

- `Views/Shared/_DoughLayout.cshtml`

The general app shell (`_Layout.cshtml`) does **not** load Tailwind. It uses:

- Bootstrap
- `site.css`
- Font Awesome

## Current Product Split

### Dough Prep side

- fixed top bar
- operational sidebar
- stronger visual hierarchy
- kitchen-first spacing
- Stitch-style color system
- Inter typography

### General app side

- older bootstrap shell
- softer but different gradients
- different navigation structure
- different spacing rhythm
- different brand framing

## Decision

Do **not** make Tailwind CDN fully global in this phase.

Reason:

- login and register are sensitive
- admin/forms use existing bootstrap validation behavior
- a second global utility layer would increase collision risk

Instead:

- keep Tailwind scoped to Dough-specific layout for now
- extract the Dough Prep visual system into a new shared stylesheet
- update `_Layout.cshtml` to use the same product identity, colors, spacing, nav hierarchy, and card language

## Chosen Approach

### Global shell

Create `wwwroot/css/app-shell.css` as the visual bridge:

- Dough Prep color tokens
- Inter typography
- top bar + sidebar pattern
- card-first surfaces
- larger form controls
- shared status/alert styling

### Layout strategy

Use:

- `_DoughLayout.cshtml` for Dough Prep / Dough Quality pages
- upgraded `_Layout.cshtml` for Home, Dashboard, Session, and Admin Users

This keeps risk low while making both shells feel like the same product.

### Reusable component direction

The first pass will standardize reusable CSS components rather than rewrite controllers:

- page hero
- action row
- metric card
- form card
- table wrapper
- alert banner

## Out of Scope

- auth logic changes
- route changes
- controller rewrites
- backend changes
- role changes
- replacing Dough-specific Tailwind layout

## Expected Outcome

After this pass:

- Home feels like the same product as Dough Prep
- Dashboard keeps its data but loses the “older app” feel
- Login/Register match the kitchen workflow brand without touching auth
- Admin Users matches the same shell and component language
