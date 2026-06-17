# Daily Dough Usage Rules

## Purpose

`Dough Used Today` explains where dough came from during service.
It does not replace `Daily Closing`, and it does not change weekly closing rules.

## Route

- Main route: `/prep/dough-usage`
- Companion screen: `/prep/dough-usage/reball-planning`

## Quantity Rules

- Entry unit is cases.
- `1.0 case = 12 balls`
- `0.5 case = 6 balls`
- `1.5 cases = 18 balls`
- Case quantity must convert to a whole number of balls before saving.
- A source cannot be used for more balls than it still has remaining.

## Source Rules

- A dough usage trace must point to a specific dough quality source.
- `Discarded` dough can never be selected as a usage source.
- The source must already exist on or before the usage date.
- Remaining dough by source is always calculated as:
  `original source balls - traced balls already used from that source`

## Destination Rules

- `Restaurant` may use `Good`, `Attention`, `Reballed`, or `MustUseNextDay` dough when still available.
- `Event` and `FarmersMarket` show a warning in June, July, and August when the selected source is older dough, reballed dough, attention dough, or must-use dough.

## Availability Rules

- `Ready Now` means dough that is usable right now.
- Open-day usage traces reduce `Ready Now` immediately.
- Closed-day consumption remains controlled by `Daily Closing`.
- `Produced This Week` comes from completed `Ball Dough` work only.
- `Future Dough` means mixed but not balled dough plus fermenting dough.
- `Future Dough` must never be counted as `Ready Now`.

## Reconciliation Rules

- `Daily Closing` is still the week-to-date actual usage authority for closed days.
- Usage traces explain the source story behind that usage.
- If closed-day `Daily Closing` totals do not match closed-day usage traces, the UI must show a reconciliation warning instead of silently blending the numbers.
