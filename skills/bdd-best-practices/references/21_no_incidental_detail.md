# Rule 21: No detail the rule does not need

> Leave out every value and every step that the rule does not need.

[All rules](../SKILL.md)

## Why

Every extra detail makes the reader look for a reason that is not there, and it is one more thing
that can break.

For example, a scenario about insufficient stock needs the available and requested quantities.
It does not need the customer's preferred language or the product price, so they go.

If elapsed time matters, define the scenario's reference date or clock and express dates relative
to it, such as `10 days before today`. A fixed date is also valid when the scenario controls the
clock. Make the time relationship explicit so the scenario remains valid on later runs.

## Bad

```gherkin
Feature: Shopping cart

  Rule: A cart quantity cannot exceed available stock
    Scenario: A customer requests more units than are in stock
      Given a customer has an empty cart
      And the customer has a preferred language of "English"
      And product "Travel mug" has 2 units in stock
      And "Travel mug" costs 12 euros
      When the customer adds 3 units of "Travel mug" to the cart
      Then the customer sees an insufficient stock error for "Travel mug"
```

The language and price change nothing about the stock limit.

## Good

```gherkin
Feature: Shopping cart

  Rule: A cart quantity cannot exceed available stock
    Scenario: A customer requests more units than are in stock
      Given a customer has an empty cart
      And product "Travel mug" has 2 units in stock
      When the customer adds 3 units of "Travel mug" to the cart
      Then the customer sees an insufficient stock error for "Travel mug"
```

## Sources

- [Seb Rose, Keep your scenarios BRIEF](https://cucumber.io/blog/bdd/keep-your-scenarios-brief/)
- [Cucumber, Gherkin reference](https://cucumber.io/docs/gherkin/reference/)
