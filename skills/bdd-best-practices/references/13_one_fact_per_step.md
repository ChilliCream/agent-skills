# Rule 13: One fact per step

> Put one fact in each step.

[All rules](../SKILL.md)

## Why

When a step checks two things and fails, the report cannot tell you which one was wrong. That is
why each step states one fact.

For example, after a customer adds a product to a cart, a scenario may check both the confirmation
and the cart contents. If one step checks both and fails, you do not know which outcome is wrong.
With two `Then` steps, the report names the failed expectation.

One fact can include a relationship. `a customer has an empty cart` establishes one starting
fact; it does not need separate steps for the customer and the cart.

## Bad

```gherkin
Feature: Shopping cart

  Rule: Adding an in-stock product confirms the addition and updates the cart
    Scenario: A customer adds an in-stock product to the cart
      Given a customer has an empty cart
      And product "Travel mug" is in stock
      When the customer adds "Travel mug" to the cart
      Then the customer sees a confirmation and sees "Travel mug" in the cart
```

## Good

```gherkin
Feature: Shopping cart

  Rule: Adding an in-stock product confirms the addition and updates the cart
    Scenario: A customer adds an in-stock product to the cart
      Given a customer has an empty cart
      And product "Travel mug" is in stock
      When the customer adds "Travel mug" to the cart
      Then the customer sees a confirmation that "Travel mug" was added to the cart
      And the customer sees "Travel mug" in the cart
```

## Sources

- [Cucumber, Anti-patterns](https://cucumber.io/docs/guides/anti-patterns/)
