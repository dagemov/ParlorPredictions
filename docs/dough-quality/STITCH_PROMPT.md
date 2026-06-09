# Stitch Prompt For Dough Quality Prototype

Use the prompt below when generating the first visual prototype in Stitch.

## Prompt

```text
Design a responsive web app prototype for Parlor Pizza, focused on a new operational layer called Dough Quality.

Important: this is a visual UX prototype only. Do not invent backend implementation details, database structure, or fake technical architecture. Focus on screen clarity, user flow, and legibility.

Product context:
Parlor Pizza already has a dough planning workflow that calculates:
- dough needed today
- dough available
- dough missing
- prep recommendations
- dough prep tasks

This new Dough Quality layer does not replace planning. It adds operational tracking for the real condition of dough batches in the kitchen.

The app must help staff understand:
- Good Dough
- Attention Dough
- Reballed Dough
- Must Use Next Day Dough
- Discarded Dough

Primary users:
1. Manager or Admin
2. PizzaMaker

User profile:
Many kitchen users are older adults. They are not highly technical, may feel lost in dense systems, and need very clear layouts, large text, obvious actions, and short labels. The interface should feel calm, operational, and easy to scan in a busy kitchen environment.

Business rules to reflect visually:
- Attention Dough still counts as available
- Dough only stops counting as available when Manager or Admin discards it
- Reball is partial recovery, never full recovery
- After reball, dough should be marked Must Use Next Day
- Discard requires a reason
- Losses are stored for future reporting, but do not design AI features now

Main screen flow:
1. Dough Prep Home
2. Dough Quality Review
3. Reball Dough Task
4. Discard Dough
5. Kitchen Sheet / Print View
6. Loss Analytics Preview

Screen requirements:

1. Dough Prep Home
- Show 4 big summary cards:
  - Need Today
  - Good Dough
  - Attention Dough
  - Need To Make
- Show a strong alert if Must Use Next Day exists
- Show a short human recommendation
- Show one clear primary action

2. Dough Quality Review
- Show simple filters for created date, balled date, reballed date, and status
- Show older dough batches as large cards, not as a dense table
- Allow actions like Mark Attention and Correct Status

3. Reball Dough Task
- Show original quantity
- Show recovered quantity input
- Show automatic loss result
- Explain clearly that recovered dough becomes Must Use Next Day

4. Discard Dough
- Show batch summary
- Show discard reason selector
- Show optional note
- Make the confirm action feel serious but simple

5. Kitchen Sheet / Print View
- Design a large-print, highly legible sheet for kitchen use
- Prioritize readability over decoration
- Include the daily summary, recommendation, and a pay-attention section

6. Loss Analytics Preview
- Show simple cards or one clean chart
- Focus on losses by week and losses by reason
- Keep it preview-level, not enterprise BI

Visual style:
- Operational, kitchen-focused, calm, and practical
- Not generic SaaS
- Not overly playful
- High contrast
- Large typography
- Strong spacing
- Very clear status hierarchy
- Cards first, tables later

Color semantics:
- Good Dough: clear safe/positive tone
- Attention Dough: warning tone
- Must Use Next Day: urgent but controlled tone
- Discarded Dough: danger tone

Important: do not rely on color alone. Always pair color with plain text labels and strong headings.

Component guidance:
- Large metric cards
- Large action buttons
- Status banners
- Card-based batch list
- Very simple forms
- Print-friendly layout
- Minimal charting for analytics preview

Example visible text:
- Need Today
- Good Dough
- Attention Dough
- Need To Make
- Must Use Next Day
- Review Older Dough
- Mark as Attention
- Correct Status
- Reball Dough
- Recovered Dough
- Dough Lost During Reball
- Confirm Discard
- Reason Required
- Print Kitchen Sheet
- Pay Attention Today

Restrictions:
- Do not design code screens
- Do not show database tables or developer tools
- Do not invent AI assistant widgets
- Do not make the screen dense with tiny text
- Do not make data tables the first thing users see
- Do not make color the only way to understand status

Deliver a clean, realistic visual prototype that could later be implemented in ASP.NET MVC, but do not present it as real implementation.
```

## Recommended Follow-Up Prompt

After the first result, a second Stitch pass should refine:

- older-user readability
- print view legibility
- action hierarchy on Dough Prep Home
- clarity between `Attention Dough` and `Must Use Next Day`

## What To Review In The Prototype

Before accepting the prototype, verify:

- the first screen answers "what do I do now?"
- the cards are readable from a short distance
- reball is shown as partial recovery, not generic editing
- discard feels intentional and auditable
- the print sheet looks usable for a real kitchen
