# Money

- Every amount is a whole number of cents, held in an `int`: see `src/Orders/Money.cs`. Never use floating-point numbers for money.
- A percentage discount is computed from the amount it applies to and rounded half up to the cent: use `Money.PercentOf()`. Round the discount, never the total.
- Show amounts to customers with `Money.FormatCents()`.
