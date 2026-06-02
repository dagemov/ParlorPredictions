# Parlor Prediction Demo Pages

This folder contains a **static GitHub Pages demo** for Parlor Prediction.

It is intentionally isolated from the real ASP.NET MVC application.

## What this is

- A lightweight presentation layer for stakeholder review
- A fake login and mock navigation flow
- A static dashboard, dough prep page, and weekly calendar
- A reversible preview that can be removed without touching the real app

## What this is not

- It does **not** use the real backend
- It does **not** use the real database
- It does **not** call Azure
- It does **not** replace the real Azure deployment plan

## Files included

- `index.html`
- `login.html`
- `dashboard.html`
- `dough.html`
- `calendar.html`
- `assets/styles.css`
- `assets/demo-data.js`

## Demo credentials

- Email: `pizzamaker@parlor.local`
- Password: `demo123`

## Run locally

Open this file directly in a browser:

```text
docs/demo-pages/index.html
```

No build tools, package restore, or server startup are required.

## Enable with GitHub Pages

1. Push the branch that contains `docs/demo-pages/`.
2. In GitHub, open `Settings > Pages`.
3. Under **Build and deployment**, choose **Deploy from a branch**.
4. Select the target branch.
5. Select the `/docs` folder.
6. Save the setting.
7. After Pages publishes, open:

```text
https://<your-github-user>.github.io/<your-repo>/demo-pages/
```

## Remove later

If you no longer need the static demo, delete it cleanly with:

```bash
git rm -r docs/demo-pages
```

Then commit the removal.
